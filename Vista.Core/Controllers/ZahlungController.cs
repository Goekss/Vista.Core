using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vista.Core.Data;
using Vista.Core.DTOs.Zahlung;
using Vista.Core.Enums;
using Vista.Core.Models;

namespace Vista.Core.Controllers;

/// <summary>
/// Taslak — Ödeme loglama. Gerçek ödeme gateway entegrasyonu (Stripe/Iyzico) ileride eklenecek.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ZahlungController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ZahlungController> _logger;

    public ZahlungController(AppDbContext db, ILogger<ZahlungController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.Zahlungen
            .Where(z => !z.IstGeloescht)
            .OrderByDescending(z => z.ZahlungsDatum)
            .Select(z => new ZahlungResponseDto
            {
                Id = z.Id,
                MandantId = z.MandantId,
                RechnungId = z.RechnungId,
                Betrag = z.Betrag,
                ZahlungsDatum = z.ZahlungsDatum,
                Status = z.Status,
                TransaktionId = z.TransaktionId,
                IBAN = z.IBAN,
                Hinweise = z.Hinweise
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var z = await _db.Zahlungen.FirstOrDefaultAsync(x => x.Id == id && !x.IstGeloescht);
        if (z is null) return NotFound();

        return Ok(new ZahlungResponseDto
        {
            Id = z.Id,
            MandantId = z.MandantId,
            RechnungId = z.RechnungId,
            Betrag = z.Betrag,
            ZahlungsDatum = z.ZahlungsDatum,
            Status = z.Status,
            TransaktionId = z.TransaktionId,
            IBAN = z.IBAN,
            Hinweise = z.Hinweise
        });
    }

    /// <summary>
    /// Taslak — Ödeme kaydı oluştur (prototip, gerçek ödeme işlemi yapmaz).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] ZahlungRequestDto dto)
    {
        var mandantClaim = User.FindFirst("MandantId")?.Value;
        if (!Guid.TryParse(mandantClaim, out var mandantId))
            return BadRequest("MandantId claim fehlt.");

        var entity = new Zahlung
        {
            MandantId = mandantId,
            RechnungId = dto.RechnungId,
            Betrag = dto.Betrag,
            ZahlungsDatum = DateTime.UtcNow,
            Status = ZahlungStatus.Ausstehend,
            IBAN = dto.IBAN,
            Hinweise = dto.Hinweise
        };

        _db.Zahlungen.Add(entity);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Zahlung oluşturuldu (taslak) | Id: {Id} | Betrag: {Betrag}", entity.Id, entity.Betrag);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new ZahlungResponseDto
        {
            Id = entity.Id,
            MandantId = entity.MandantId,
            RechnungId = entity.RechnungId,
            Betrag = entity.Betrag,
            ZahlungsDatum = entity.ZahlungsDatum,
            Status = entity.Status,
            IBAN = entity.IBAN,
            Hinweise = entity.Hinweise
        });
    }

    /// <summary>
    /// Taslak — Ödeme durumunu güncelle (manuel onay/red).
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] ZahlungStatus status)
    {
        var entity = await _db.Zahlungen.FirstOrDefaultAsync(z => z.Id == id && !z.IstGeloescht);
        if (entity is null) return NotFound();

        entity.Status = status;
        entity.AktualisiertAm = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Zahlung status güncellendi | Id: {Id} | Status: {Status}", id, status);
        return NoContent();
    }
}
