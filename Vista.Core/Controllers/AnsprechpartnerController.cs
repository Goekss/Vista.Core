using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vista.Core.Data;
using Vista.Core.Models;

namespace Vista.Core.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AnsprechpartnerController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<AnsprechpartnerController> _logger;

    public AnsprechpartnerController(AppDbContext db, ILogger<AnsprechpartnerController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("kunde/{kundeId}")]
    public async Task<IActionResult> GetByKunde(Guid kundeId)
    {
        var list = await _db.Ansprechpartner
            .Where(a => a.KundeId == kundeId && !a.IstGeloescht)
            .OrderBy(a => a.Name)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Telefon,
                a.Email,
                a.Abteilung,
                a.KundeId,
                a.FilialeId
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var a = await _db.Ansprechpartner.FirstOrDefaultAsync(x => x.Id == id && !x.IstGeloescht);
        if (a is null) return NotFound(new { Nachricht = "Ansprechpartner nicht gefunden." });

        return Ok(new { a.Id, a.Name, a.Telefon, a.Email, a.Abteilung, a.KundeId, a.FilialeId });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AnsprechpartnerRequestDto dto)
    {
        var mandantId = GetMandantId();
        if (mandantId is null) return BadRequest(new { Nachricht = "MandantId fehlt." });

        var ap = new Ansprechpartner
        {
            MandantId = mandantId.Value,
            KundeId = dto.KundeId,
            FilialeId = dto.FilialeId,
            Name = dto.Name,
            Telefon = dto.Telefon,
            Email = dto.Email,
            Abteilung = dto.Abteilung
        };

        _db.Ansprechpartner.Add(ap);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Ansprechpartner erstellt | Id: {Id}", ap.Id);
        return CreatedAtAction(nameof(GetById), new { id = ap.Id }, new { ap.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AnsprechpartnerRequestDto dto)
    {
        var ap = await _db.Ansprechpartner.FirstOrDefaultAsync(x => x.Id == id && !x.IstGeloescht);
        if (ap is null) return NotFound(new { Nachricht = "Ansprechpartner nicht gefunden." });

        ap.Name = dto.Name;
        ap.Telefon = dto.Telefon;
        ap.Email = dto.Email;
        ap.Abteilung = dto.Abteilung;
        ap.FilialeId = dto.FilialeId;
        ap.AktualisiertAm = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Ansprechpartner aktualisiert | Id: {Id}", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ap = await _db.Ansprechpartner.FirstOrDefaultAsync(x => x.Id == id && !x.IstGeloescht);
        if (ap is null) return NotFound(new { Nachricht = "Ansprechpartner nicht gefunden." });

        ap.IstGeloescht = true;
        ap.AktualisiertAm = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Ansprechpartner soft-deleted | Id: {Id}", id);
        return NoContent();
    }

    private Guid? GetMandantId()
    {
        var claim = User.FindFirst("MandantId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public class AnsprechpartnerRequestDto
{
    public Guid KundeId { get; set; }
    public Guid? FilialeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Abteilung { get; set; } = string.Empty;
}
