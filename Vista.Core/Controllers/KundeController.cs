using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vista.Core.Data;
using Vista.Core.DTOs.Common;
using Vista.Core.DTOs.Kunde;
using Vista.Core.Models;
using Vista.Core.Services;

namespace Vista.Core.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class KundeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<KundeController> _logger;
    private readonly FileStorageService _fileStorage;

    public KundeController(AppDbContext db, ILogger<KundeController> logger, FileStorageService fileStorage)
    {
        _db = db;
        _logger = logger;
        _fileStorage = fileStorage;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] string? search = null)
    {
        var query = _db.Kunden.Where(k => !k.IstGeloescht).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(k => k.Unternehmen.Contains(search) || k.Vorname.Contains(search) || k.Nachname.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(k => k.ErstelltAm)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(k => new KundeResponseDto
            {
                Id = k.Id,
                MandantId = k.MandantId,
                Unternehmen = k.Unternehmen,
                Vorname = k.Vorname,
                Nachname = k.Nachname,
                Email = k.Email,
                TelefonMobil = k.TelefonMobil,
                TelefonHaus = k.TelefonHaus,
                Adresse = k.Adresse,
                Website = k.Website,
                Logo = k.Logo,
                Hinweise = k.Hinweise
            })
            .ToListAsync();

        return Ok(new PagedResult<KundeResponseDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var k = await _db.Kunden.FirstOrDefaultAsync(x => x.Id == id && !x.IstGeloescht);
        if (k is null) return NotFound(new { Nachricht = "Kunde nicht gefunden." });

        return Ok(new KundeResponseDto
        {
            Id = k.Id,
            MandantId = k.MandantId,
            Unternehmen = k.Unternehmen,
            Vorname = k.Vorname,
            Nachname = k.Nachname,
            Email = k.Email,
            TelefonMobil = k.TelefonMobil,
            TelefonHaus = k.TelefonHaus,
            Adresse = k.Adresse,
            Website = k.Website,
            Logo = k.Logo,
            Hinweise = k.Hinweise
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] KundeRequestDto dto)
    {
        var mandantId = GetMandantId();
        if (mandantId is null) return BadRequest(new { Nachricht = "MandantId fehlt." });

        var kunde = new Kunde
        {
            MandantId = mandantId.Value,
            Unternehmen = dto.Unternehmen,
            Vorname = dto.Vorname,
            Nachname = dto.Nachname,
            Email = dto.Email,
            TelefonMobil = dto.TelefonMobil,
            TelefonHaus = dto.TelefonHaus,
            Adresse = dto.Adresse,
            Website = dto.Website,
            Logo = dto.Logo,
            Hinweise = dto.Hinweise
        };

        _db.Kunden.Add(kunde);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Kunde erstellt | Id: {Id}", kunde.Id);
        return CreatedAtAction(nameof(GetById), new { id = kunde.Id }, new { kunde.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] KundeRequestDto dto)
    {
        var kunde = await _db.Kunden.FirstOrDefaultAsync(k => k.Id == id && !k.IstGeloescht);
        if (kunde is null) return NotFound(new { Nachricht = "Kunde nicht gefunden." });

        kunde.Unternehmen = dto.Unternehmen;
        kunde.Vorname = dto.Vorname;
        kunde.Nachname = dto.Nachname;
        kunde.Email = dto.Email;
        kunde.TelefonMobil = dto.TelefonMobil;
        kunde.TelefonHaus = dto.TelefonHaus;
        kunde.Adresse = dto.Adresse;
        kunde.Website = dto.Website;
        kunde.Logo = dto.Logo;
        kunde.Hinweise = dto.Hinweise;
        kunde.AktualisiertAm = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Kunde aktualisiert | Id: {Id}", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var kunde = await _db.Kunden.FirstOrDefaultAsync(k => k.Id == id && !k.IstGeloescht);
        if (kunde is null) return NotFound(new { Nachricht = "Kunde nicht gefunden." });

        // Soft delete
        kunde.IstGeloescht = true;
        kunde.AktualisiertAm = DateTime.UtcNow;

        // Logo dosyasını sil (varsa)
        if (!string.IsNullOrWhiteSpace(kunde.Logo))
        {
            await _fileStorage.DeleteFileAsync(kunde.Logo);
            _logger.LogInformation("Kunde Logo silindi | KundeId: {Id}, Logo: {Logo}", id, kunde.Logo);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Kunde soft-deleted | Id: {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Müşteri logosu yükle (PNG, JPEG, JPG - Max 5MB)
    /// </summary>
    [HttpPost("{id}/upload-logo")]
    public async Task<IActionResult> UploadLogo(Guid id, IFormFile logoFile)
    {
        var kunde = await _db.Kunden.FirstOrDefaultAsync(k => k.Id == id && !k.IstGeloescht);
        if (kunde is null) return NotFound(new { Nachricht = "Kunde nicht gefunden." });

        var (success, fileUrl, errorMessage) = await _fileStorage.UploadFileAsync(
            logoFile, 
            FileStorageService.LogosFolder, 
            id.ToString(),
            kunde.Logo // Eski logoyu sil
        );

        if (!success)
            return BadRequest(new { Nachricht = errorMessage });

        // Yeni logo URL'ini kaydet
        kunde.Logo = fileUrl!;
        kunde.AktualisiertAm = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Kunde Logo yüklendi | KundeId: {Id}, Logo: {Logo}", id, fileUrl);
        return Ok(new { Logo = fileUrl, Nachricht = "Logo başarıyla yüklendi." });
    }

    /// <summary>
    /// Müşteri logosunu sil
    /// </summary>
    [HttpDelete("{id}/delete-logo")]
    public async Task<IActionResult> DeleteLogo(Guid id)
    {
        var kunde = await _db.Kunden.FirstOrDefaultAsync(k => k.Id == id && !k.IstGeloescht);
        if (kunde is null) return NotFound(new { Nachricht = "Kunde nicht gefunden." });

        if (string.IsNullOrWhiteSpace(kunde.Logo))
            return BadRequest(new { Nachricht = "Logo zaten mevcut değil." });

        var success = await _fileStorage.DeleteFileAsync(kunde.Logo);
        if (!success)
            return StatusCode(500, new { Nachricht = "Logo silinirken bir hata oluştu." });

        // DB'den logo URL'ini temizle
        kunde.Logo = string.Empty;
        kunde.AktualisiertAm = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Kunde Logo silindi | KundeId: {Id}", id);
        return Ok(new { Nachricht = "Logo başarıyla silindi." });
    }

    private Guid? GetMandantId()
    {
        var claim = User.FindFirst("MandantId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
