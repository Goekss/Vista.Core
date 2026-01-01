namespace Vista.Core.Services;

/// <summary>
/// Dosya yükleme, silme ve yönetim işlemlerini güvenli bir şekilde gerçekleştirir.
/// PNG, JPEG, JPG formatlarını destekler. Max 5MB dosya boyutu.
/// </summary>
public class FileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _storageBasePath;

    // İzin verilen dosya uzantıları
    private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".pdf", ".xls", ".xlsx", ".doc", ".docx", ".zip", ".rar", ".txt" };

    // Max dosya boyutu: 5MB
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    // Klasör yapısı
    public const string LogosFolder = "Logos";
    public const string AvatarsFolder = "Avatars";
    public const string ChatFolder = "Chat";

    public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> logger)
    {
        _env = env;
        _logger = logger;
        _storageBasePath = Path.Combine(_env.ContentRootPath, "Storage");

        // Klasörleri oluştur (yoksa)
        EnsureDirectoriesExist();
    }

    /// <summary>
    /// Gerekli klasörlerin varlığını garanti eder
    /// </summary>
    private void EnsureDirectoriesExist()
    {
        try
        {
            var logosPath = Path.Combine(_storageBasePath, LogosFolder);
            var avatarsPath = Path.Combine(_storageBasePath, AvatarsFolder);

            if (!Directory.Exists(logosPath))
            {
                Directory.CreateDirectory(logosPath);
                _logger.LogInformation("Logos klasörü oluşturuldu: {Path}", logosPath);
            }

            if (!Directory.Exists(avatarsPath))
            {
                Directory.CreateDirectory(avatarsPath);
                _logger.LogInformation("Avatars klasörü oluşturuldu: {Path}", avatarsPath);
            }

            var chatPath = Path.Combine(_storageBasePath, ChatFolder);
            if (!Directory.Exists(chatPath))
            {
                Directory.CreateDirectory(chatPath);
                _logger.LogInformation("Chat klasörü oluşturuldu: {Path}", chatPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Klasörler oluşturulurken hata oluştu");
            throw;
        }
    }

    /// <summary>
    /// Dosya yükler ve URL döner
    /// </summary>
    /// <param name="file">Yüklenecek dosya</param>
    /// <param name="folderName">Hedef klasör (Logos veya Avatars)</param>
    /// <param name="uniqueId">Dosya adı için unique identifier (örn: Guid)</param>
    /// <param name="oldFilePath">Eski dosya varsa silinmesi için (opsiyonel)</param>
    /// <returns>Başarı durumu ve URL</returns>
    public async Task<(bool Success, string? FileUrl, string? ErrorMessage)> UploadFileAsync(
        IFormFile file, 
        string folderName, 
        string uniqueId,
        string? oldFilePath = null)
    {
        try
        {
            // Validasyonlar
            if (file == null || file.Length == 0)
                return (false, null, "Dosya boş veya geçersiz");

            if (file.Length > MaxFileSizeBytes)
                return (false, null, $"Dosya boyutu çok büyük. Max: {MaxFileSizeBytes / (1024 * 1024)}MB");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return (false, null, $"Desteklenmeyen dosya formatı. İzin verilenler: {string.Join(", ", AllowedExtensions)}");

            // Dosya adı oluştur (collision önleme)
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var safeFileName = $"{uniqueId}-{timestamp}{extension}";

            // Tam dosya yolu
            var targetDirectory = Path.Combine(_storageBasePath, folderName);
            var targetFilePath = Path.Combine(targetDirectory, safeFileName);

            // Path traversal saldırılarına karşı kontrol
            if (!IsPathSafe(targetFilePath, _storageBasePath))
                return (false, null, "Güvenlik hatası: Geçersiz dosya yolu");

            // Eski dosyayı sil (varsa)
            if (!string.IsNullOrWhiteSpace(oldFilePath))
            {
                await DeleteFileAsync(oldFilePath);
            }

            // Dosyayı kaydet
            await using (var stream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            // URL döndür
            var fileUrl = $"/storage/{folderName}/{safeFileName}";

            _logger.LogInformation("Dosya yüklendi: {FilePath}", targetFilePath);
            return (true, fileUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya yüklenirken hata oluştu: {FileName}", file?.FileName);
            return (false, null, "Dosya yüklenirken beklenmeyen bir hata oluştu");
        }
    }

    /// <summary>
    /// Dosyayı siler (null-safe)
    /// </summary>
    /// <param name="fileUrl">Silinecek dosyanın URL'i (örn: /storage/Logos/file.png)</param>
    /// <returns>Başarı durumu</returns>
    public Task<bool> DeleteFileAsync(string? fileUrl)
    {
        try
        {
            // Boş veya null kontrolü
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                _logger.LogDebug("Silinecek dosya yolu boş, işlem atlandı");
                return Task.FromResult(true); // Hata değil, sadece işlem yok
            }

            // URL'den fiziksel yola dönüştür
            // Örnek: /storage/Logos/file.png -> C:\...\Storage\Logos\file.png
            var relativePath = fileUrl.TrimStart('/').Replace("storage/", "", StringComparison.OrdinalIgnoreCase);
            var filePath = Path.Combine(_storageBasePath, relativePath);

            // Path traversal güvenlik kontrolü
            if (!IsPathSafe(filePath, _storageBasePath))
            {
                _logger.LogWarning("Güvenlik uyarısı: Geçersiz dosya yolu silinmeye çalışıldı: {FilePath}", fileUrl);
                return Task.FromResult(false);
            }

            // Dosya var mı kontrol et
            if (!File.Exists(filePath))
            {
                _logger.LogDebug("Dosya zaten mevcut değil, silme atlandı: {FilePath}", filePath);
                return Task.FromResult(true); // Zaten yok, sorun değil
            }

            // Dosyayı sil
            File.Delete(filePath);
            _logger.LogInformation("Dosya silindi: {FilePath}", filePath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya silinirken hata oluştu: {FileUrl}", fileUrl);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Path traversal saldırılarına karşı güvenlik kontrolü
    /// </summary>
    private bool IsPathSafe(string path, string baseDirectory)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var fullBasePath = Path.GetFullPath(baseDirectory);
            return fullPath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Dosyanın fiziksel olarak var olup olmadığını kontrol eder
    /// </summary>
    public bool FileExists(string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return false;

        try
        {
            var relativePath = fileUrl.TrimStart('/').Replace("storage/", "", StringComparison.OrdinalIgnoreCase);
            var filePath = Path.Combine(_storageBasePath, relativePath);
            return File.Exists(filePath);
        }
        catch
        {
            return false;
        }
    }
}
