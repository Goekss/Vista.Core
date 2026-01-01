using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vista.Core.Data;
using Vista.Core.DTOs.Common;
using Vista.Core.DTOs.Projekt;
using Vista.Core.Models;

namespace Vista.Core.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProjektController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProjektController> _logger;

    public ProjektController(AppDbContext db, ILogger<ProjektController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] string? search = null)
    {
        var query = _db.Projekte.Where(p => !p.IstGeloescht).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Beschreibung.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.ErstelltAm)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => new ProjektResponseDto
            {
                Id = p.Id,
                MandantId = p.MandantId,
                Name = p.Name,
                Beschreibung = p.Beschreibung,
                Startdatum = p.Startdatum,
                Enddatum = p.Enddatum,
                Status = p.Status,
                Prioritaet = p.Prioritaet,
                AbschlussInProzent = p.AbschlussInProzent,
                IstAbgeschlossen = p.IstAbgeschlossen,
                KundeId = p.KundeId
            })
            .ToListAsync();

        return Ok(new PagedResult<ProjektResponseDto>
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
        var p = await _db.Projekte
            .Include(x => x.Benutzer)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IstGeloescht);

        if (p is null) return NotFound(new { Nachricht = "Projekt nicht gefunden." });

        return Ok(new
        {
            p.Id,
            p.MandantId,
            p.Name,
            p.Beschreibung,
            p.Startdatum,
            p.Enddatum,
            p.Status,
            p.Prioritaet,
            p.AbschlussInProzent,
            p.IstAbgeschlossen,
            p.KundeId,
            Benutzer = p.Benutzer.Select(b => new { b.Id, b.Vorname, b.Nachname, b.Email })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProjektRequestDto dto)
    {
        var mandantId = GetMandantId();
        if (mandantId is null) return BadRequest(new { Nachricht = "MandantId fehlt." });

        var projekt = new Projekt
        {
            MandantId = mandantId.Value,
            Name = dto.Name,
            Beschreibung = dto.Beschreibung,
            Startdatum = dto.Startdatum,
            Enddatum = dto.Enddatum,
            Status = dto.Status,
            Prioritaet = dto.Prioritaet,
            AbschlussInProzent = dto.AbschlussInProzent,
            IstAbgeschlossen = dto.IstAbgeschlossen,
            KundeId = dto.KundeId
        };

        if (dto.BenutzerIds.Count > 0)
        {
            var benutzer = await _db.Users.Where(u => dto.BenutzerIds.Contains(u.Id)).ToListAsync();
            projekt.Benutzer = benutzer;
        }

        _db.Projekte.Add(projekt);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Projekt erstellt | Id: {Id}", projekt.Id);
        return CreatedAtAction(nameof(GetById), new { id = projekt.Id }, new { projekt.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ProjektRequestDto dto)
    {
        var projekt = await _db.Projekte
            .Include(p => p.Benutzer)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IstGeloescht);

        if (projekt is null) return NotFound(new { Nachricht = "Projekt nicht gefunden." });

        projekt.Name = dto.Name;
        projekt.Beschreibung = dto.Beschreibung;
        projekt.Startdatum = dto.Startdatum;
        projekt.Enddatum = dto.Enddatum;
        projekt.Status = dto.Status;
        projekt.Prioritaet = dto.Prioritaet;
        projekt.AbschlussInProzent = dto.AbschlussInProzent;
        projekt.IstAbgeschlossen = dto.IstAbgeschlossen;
        projekt.KundeId = dto.KundeId;
        projekt.AktualisiertAm = DateTime.UtcNow;

        if (dto.BenutzerIds.Count > 0)
        {
            var benutzer = await _db.Users.Where(u => dto.BenutzerIds.Contains(u.Id)).ToListAsync();
            projekt.Benutzer = benutzer;
        }
        else
        {
            projekt.Benutzer.Clear();
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Projekt aktualisiert | Id: {Id}", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var projekt = await _db.Projekte.FirstOrDefaultAsync(p => p.Id == id && !p.IstGeloescht);
        if (projekt is null) return NotFound(new { Nachricht = "Projekt nicht gefunden." });

        projekt.IstGeloescht = true;
        projekt.AktualisiertAm = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Projekt soft-deleted | Id: {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Projeye personel ata/güncelle
    /// </summary>
    [HttpPut("{id}/benutzer")]
    public async Task<IActionResult> AssignBenutzer(Guid id, [FromBody] List<string> benutzerIds)
    {
        var projekt = await _db.Projekte
            .Include(p => p.Benutzer)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IstGeloescht);

        if (projekt is null) return NotFound(new { Nachricht = "Projekt nicht gefunden." });

        var benutzer = await _db.Users.Where(u => benutzerIds.Contains(u.Id)).ToListAsync();
        projekt.Benutzer = benutzer;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Projekt Benutzer zugewiesen | ProjektId: {Id} | Anzahl: {Count}", id, benutzer.Count);
        return Ok(new { Nachricht = $"{benutzer.Count} Benutzer zugewiesen." });
    }

    private Guid? GetMandantId()
    {
        var claim = User.FindFirst("MandantId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
