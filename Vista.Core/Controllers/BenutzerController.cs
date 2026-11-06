using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Vista.Core.Models;
using Vista.Core.Services;

namespace Vista.Core.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin,Manager")]
[Route("api/[controller]")]
public class BenutzerController : ControllerBase
{
    private readonly UserManager<Benutzer> _userManager;
    private readonly ILogger<BenutzerController> _logger;
    private readonly FileStorageService _fileStorage;

    public BenutzerController(UserManager<Benutzer> userManager, ILogger<BenutzerController> logger, FileStorageService fileStorage)
    {
        _userManager = userManager;
        _logger = logger;
        _fileStorage = fileStorage;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var mandantId = GetMandantId();
        if (mandantId is null) return BadRequest(new { Nachricht = "MandantId fehlt." });

        var list = _userManager.Users
            .Where(b => b.MandantId == mandantId)
            .Select(b => new
            {
                b.Id,
                b.Vorname,
                b.Nachname,
                b.Email,
                b.RufNummer,
                b.Abteilung,
                b.Rolle,
                b.Bild,
                b.MandantId
            })
            .ToList();

        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var mandantId = GetMandantId();
        if (mandantId is null) return BadRequest(new { Nachricht = "MandantId fehlt." });

        var b = await _userManager.FindByIdAsync(id);
        if (b is null || b.MandantId != mandantId) return NotFound(new { Nachricht = "Benutzer nicht gefunden." });

        var rollen = await _userManager.GetRolesAsync(b);

        return Ok(new
        {
            b.Id,
            b.Vorname,
            b.Nachname,
            b.Email,
            b.RufNummer,
            b.Abteilung,
            b.Rolle,
            b.Bild,
            b.Hinweise,
            b.MandantId,
            Rollen = rollen
        });
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BenutzerCreateDto dto)
    {
        var mandantId = GetMandantId();
        if (mandantId is null) return BadRequest(new { Nachricht = "MandantId fehlt." });

        if (!DarfRolleVergeben(dto.Rolle, out var fehler))
            return StatusCode(403, new { Nachricht = fehler });

        var benutzer = new Benutzer
        {
            UserName = dto.Email,
            Email = dto.Email,
            Vorname = dto.Vorname,
            Nachname = dto.Nachname,
            RufNummer = dto.RufNummer,
            Abteilung = dto.Abteilung,
            Rolle = dto.Rolle,
            MandantId = mandantId,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(benutzer, dto.Passwort);
        if (!result.Succeeded)
            return BadRequest(new { Nachricht = "Erstellung fehlgeschlagen.", Fehler = result.Errors.Select(e => e.Description) });

        if (!string.IsNullOrWhiteSpace(dto.Rolle))
            await _userManager.AddToRoleAsync(benutzer, dto.Rolle);

        _logger.LogInformation("Benutzer erstellt | Email: {Email}", dto.Email);
        return CreatedAtAction(nameof(GetById), new { id = benutzer.Id }, new { benutzer.Id });
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] BenutzerUpdateDto dto)
    {
        var mandantId = GetMandantId();
        if (mandantId is null) return BadRequest(new { Nachricht = "MandantId fehlt." });

        var benutzer = await _userManager.FindByIdAsync(id);
        if (benutzer is null || benutzer.MandantId != mandantId) return NotFound(new { Nachricht = "Benutzer nicht gefunden." });

        benutzer.Vorname = dto.Vorname;
        benutzer.Nachname = dto.Nachname;
        benutzer.RufNummer = dto.RufNummer;
        benutzer.Abteilung = dto.Abteilung;
        benutzer.Hinweise = dto.Hinweise;

        if (!string.IsNullOrWhiteSpace(dto.Rolle) && dto.Rolle != benutzer.Rolle)
        {
            if (!DarfRolleVergeben(dto.Rolle, out var fehler))
                return StatusCode(403, new { Nachricht = fehler });

            var currentRoles = await _userManager.GetRolesAsync(benutzer);
            await _userManager.RemoveFromRolesAsync(benutzer, currentRoles);
            await _userManager.AddToRoleAsync(benutzer, dto.Rolle);
            benutzer.Rolle = dto.Rolle;
        }

        await _userManager.UpdateAsync(benutzer);

        _logger.LogInformation("Benutzer aktualisiert | Id: {Id}", id);
        return NoContent();
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var mandantId = GetMandantId();
        if (mandantId is null) return BadRequest(new { Nachricht = "MandantId fehlt." });

        var benutzer = await _userManager.FindByIdAsync(id);
        if (benutzer is null || benutzer.MandantId != mandantId) return NotFound(new { Nachricht = "Benutzer nicht gefunden." });

        // Avatar dosyasını sil (varsa)
        if (!string.IsNullOrWhiteSpace(benutzer.Bild))
        {
            await _fileStorage.DeleteFileAsync(benutzer.Bild);
            _logger.LogInformation("Benutzer Avatar silindi | BenutzerId: {Id}, Avatar: {Avatar}", id, benutzer.Bild);
        }

        await _userManager.DeleteAsync(benutzer);

        _logger.LogInformation("Benutzer gelöscht | Id: {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Kullanıcı avatarı yükle (PNG, JPEG, JPG - Max 5MB)
    /// </summary>
    [HttpPost("{id}/upload-avatar")]
    public async Task<IActionResult> UploadAvatar(string id, IFormFile avatarFile)
    {
        var mandantId = GetMandantId();
        if (mandantId is null) return BadRequest(new { Nachricht = "MandantId fehlt." });

        var benutzer = await _userManager.FindByIdAsync(id);
        if (benutzer is null || benutzer.MandantId != mandantId) return NotFound(new { Nachricht = "Benutzer nicht gefunden." });

        var (success, fileUrl, errorMessage) = await _fileStorage.UploadFileAsync(
            avatarFile,
            FileStorageService.AvatarsFolder,
            id,
            benutzer.Bild // Eski avatarı sil
        );

        if (!success)
            return BadRequest(new { Nachricht = errorMessage });

        // Yeni avatar URL'ini kaydet
        benutzer.Bild = fileUrl!;
        await _userManager.UpdateAsync(benutzer);

        _logger.LogInformation("Benutzer Avatar yüklendi | BenutzerId: {Id}, Avatar: {Avatar}", id, fileUrl);
        return Ok(new { Avatar = fileUrl, Nachricht = "Avatar başarıyla yüklendi." });
    }

    /// <summary>
    /// Kullanıcı avatarını sil
    /// </summary>
    [HttpDelete("{id}/delete-avatar")]
    public async Task<IActionResult> DeleteAvatar(string id)
    {
        var mandantId = GetMandantId();
        if (mandantId is null) return BadRequest(new { Nachricht = "MandantId fehlt." });

        var benutzer = await _userManager.FindByIdAsync(id);
        if (benutzer is null || benutzer.MandantId != mandantId) return NotFound(new { Nachricht = "Benutzer nicht gefunden." });

        if (string.IsNullOrWhiteSpace(benutzer.Bild))
            return BadRequest(new { Nachricht = "Avatar zaten mevcut değil." });

        var success = await _fileStorage.DeleteFileAsync(benutzer.Bild);
        if (!success)
            return StatusCode(500, new { Nachricht = "Avatar silinirken bir hata oluştu." });

        // DB'den avatar URL'ini temizle
        benutzer.Bild = string.Empty;
        await _userManager.UpdateAsync(benutzer);

        _logger.LogInformation("Benutzer Avatar silindi | BenutzerId: {Id}", id);
        return Ok(new { Nachricht = "Avatar başarıyla silindi." });
    }

    private string? GetMandantId()
    {
        return User.FindFirst("MandantId")?.Value;
    }

    // TR: SuperAdmin dışındaki rolleri yalnızca Admin/SuperAdmin atayabilir; Manager rol atayamaz.
    // DE: Nur Admin/SuperAdmin dürfen Rollen vergeben (außer SuperAdmin); Manager darf keine Rollen vergeben.
    private static readonly string[] ZuweisbareRollen = { "Admin", "Manager", "Standard", "NurLesen" };

    private bool DarfRolleVergeben(string? rolle, out string fehler)
    {
        fehler = string.Empty;

        if (string.IsNullOrWhiteSpace(rolle))
            return true;

        if (User.IsInRole("SuperAdmin"))
            return true;

        if (!ZuweisbareRollen.Contains(rolle))
        {
            fehler = "Diese Rolle darf nicht vergeben werden.";
            return false;
        }

        if (!User.IsInRole("Admin"))
        {
            fehler = "Nur Administratoren dürfen Rollen vergeben.";
            return false;
        }

        return true;
    }
}

public class BenutzerCreateDto
{
    public string Email { get; set; } = string.Empty;
    public string Passwort { get; set; } = string.Empty;
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public string RufNummer { get; set; } = string.Empty;
    public string Abteilung { get; set; } = string.Empty;
    public string Rolle { get; set; } = string.Empty;
}

public class BenutzerUpdateDto
{
    public string Vorname { get; set; } = string.Empty;
    public string Nachname { get; set; } = string.Empty;
    public string RufNummer { get; set; } = string.Empty;
    public string Abteilung { get; set; } = string.Empty;
    public string Rolle { get; set; } = string.Empty;
    public string Hinweise { get; set; } = string.Empty;
}
