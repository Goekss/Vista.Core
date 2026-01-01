using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Vista.Core.Data;
using Vista.Core.DTOs.Abonnement;
using Vista.Core.Enums;
using Vista.Core.Models;

namespace Vista.Core.Controllers;

/// <summary>
/// Taslak — Abonelik plan yönetimi. Gerçek ödeme entegrasyonu ileride eklenecek.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AbonnementController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<AbonnementController> _logger;

    public AbonnementController(AppDbContext db, ILogger<AbonnementController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.Abonnements
            .Where(a => !a.IstGeloescht)
            .OrderByDescending(a => a.ErstelltAm)
            .Select(a => new AbonnementResponseDto
            {
                Id = a.Id,
                MandantId = a.MandantId,
                Plan = a.Plan,
                PlanName = a.PlanName,
                Preis = a.Preis,
                StartDatum = a.StartDatum,
                EndDatum = a.EndDatum,
                IstAktiv = a.IstAktiv
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var a = await _db.Abonnements.FirstOrDefaultAsync(x => x.Id == id && !x.IstGeloescht);
        if (a is null) return NotFound();

        return Ok(new AbonnementResponseDto
        {
            Id = a.Id,
            MandantId = a.MandantId,
            Plan = a.Plan,
            PlanName = a.PlanName,
            Preis = a.Preis,
            StartDatum = a.StartDatum,
            EndDatum = a.EndDatum,
            IstAktiv = a.IstAktiv
        });
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Create([FromBody] AbonnementRequestDto dto)
    {
        var mandantClaim = User.FindFirst("MandantId")?.Value;
        if (!Guid.TryParse(mandantClaim, out var mandantId))
            return BadRequest("MandantId claim fehlt.");

        var entity = new Abonnement
        {
            MandantId = mandantId,
            Plan = dto.Plan,
            PlanName = dto.PlanName,
            Preis = dto.Preis,
            StartDatum = dto.StartDatum,
            EndDatum = dto.EndDatum,
            IstAktiv = true
        };

        _db.Abonnements.Add(entity);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Abonnement oluşturuldu | Id: {Id} | Plan: {Plan}", entity.Id, entity.PlanName);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new AbonnementResponseDto
        {
            Id = entity.Id,
            MandantId = entity.MandantId,
            Plan = entity.Plan,
            PlanName = entity.PlanName,
            Preis = entity.Preis,
            StartDatum = entity.StartDatum,
            EndDatum = entity.EndDatum,
            IstAktiv = entity.IstAktiv
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AbonnementRequestDto dto)
    {
        var entity = await _db.Abonnements.FirstOrDefaultAsync(a => a.Id == id && !a.IstGeloescht);
        if (entity is null) return NotFound();

        entity.Plan = dto.Plan;
        entity.PlanName = dto.PlanName;
        entity.Preis = dto.Preis;
        entity.StartDatum = dto.StartDatum;
        entity.EndDatum = dto.EndDatum;
        entity.AktualisiertAm = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Abonnement güncellendi | Id: {Id}", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _db.Abonnements.FirstOrDefaultAsync(a => a.Id == id && !a.IstGeloescht);
        if (entity is null) return NotFound();

        entity.IstGeloescht = true;
        entity.IstAktiv = false;
        entity.AktualisiertAm = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Abonnement silindi (soft) | Id: {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Taslak — Mevcut plan seçeneklerini listele.
    /// </summary>
    [HttpGet("plaene")]
    [AllowAnonymous]
    public IActionResult GetPlaene()
    {
        var plaene = new[]
        {
            new { Plan = AbonnementPlan.Kostenlos, Name = "Kostenlos", Preis = 0m, Beschreibung = "Ücretsiz plan — temel özellikler" },
            new { Plan = AbonnementPlan.Basis, Name = "Basis", Preis = 29.99m, Beschreibung = "Küçük ekipler için" },
            new { Plan = AbonnementPlan.Professional, Name = "Professional", Preis = 79.99m, Beschreibung = "Orta ölçekli firmalar" },
            new { Plan = AbonnementPlan.Enterprise, Name = "Enterprise", Preis = 199.99m, Beschreibung = "Büyük firmalar — tüm özellikler" }
        };

        return Ok(plaene);
    }
}
