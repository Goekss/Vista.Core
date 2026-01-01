using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Vista.Core.Data;
using Vista.Core.DTOs.Common;
using Vista.Core.DTOs.Ticket;
using Vista.Core.Hubs;
using Vista.Core.Models;
using Vista.Core.Services;

namespace Vista.Core.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TicketController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly EmailService _emailService;
    private readonly IHubContext<BenachrichtigungHub> _hub;
    private readonly ILogger<TicketController> _logger;

    public TicketController(AppDbContext db, EmailService emailService, IHubContext<BenachrichtigungHub> hub, ILogger<TicketController> logger)
    {
        _db = db;
        _emailService = emailService;
        _hub = hub;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? prioritaet = null,
        [FromQuery] string? search = null)
    {
        var query = _db.Tickets.Where(t => !t.IstGeloescht).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrWhiteSpace(prioritaet))
            query = query.Where(t => t.Prioritaet == prioritaet);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Titel.Contains(search) || t.Beschreibung.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.ErstelltAm)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(t => new TicketResponseDto
            {
                Id = t.Id,
                MandantId = t.MandantId,
                Titel = t.Titel,
                Beschreibung = t.Beschreibung,
                Status = t.Status,
                Prioritaet = t.Prioritaet,
                Kategorie = t.Kategorie,
                Faelligkeitsdatum = t.Faelligkeitsdatum,
                ZugewiesenAnId = t.ZugewiesenAnId,
                KundeId = t.KundeId,
                ProjektId = t.ProjektId
            })
            .ToListAsync();

        return Ok(new PagedResult<TicketResponseDto>
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
        var t = await _db.Tickets
            .Include(x => x.ZugewiesenAn)
            .Include(x => x.Kunde)
            .Include(x => x.Nachrichten.OrderByDescending(n => n.GeschicktAm))
                .ThenInclude(n => n.Absender)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IstGeloescht);

        if (t is null) return NotFound(new { Nachricht = "Ticket nicht gefunden." });

        return Ok(new
        {
            t.Id,
            t.Titel,
            t.Beschreibung,
            t.Status,
            t.Prioritaet,
            t.Kategorie,
            t.Faelligkeitsdatum,
            t.KundeId,
            t.ProjektId,
            t.ErstelltAm,
            ZugewiesenAn = t.ZugewiesenAn is null ? null : new { t.ZugewiesenAn.Id, t.ZugewiesenAn.Vorname, t.ZugewiesenAn.Nachname },
            Kunde = new { t.Kunde!.Unternehmen, t.Kunde.Email },
            Nachrichten = t.Nachrichten.Select(n => new
            {
                n.Id,
                n.Inhalt,
                n.IstInternNotiz,
                n.GeschicktAm,
                Absender = n.Absender is null ? null : new { n.Absender.Vorname, n.Absender.Nachname }
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TicketRequestDto dto)
    {
        var mandantId = GetMandantId();
        if (mandantId is null) return BadRequest(new { Nachricht = "MandantId fehlt." });
        if (dto.KundeId == Guid.Empty) return BadRequest(new { Nachricht = "KundeId fehlt." });

        var ticket = new Ticket
        {
            MandantId = mandantId.Value,
            Titel = dto.Titel,
            Beschreibung = dto.Beschreibung,
            Status = "Offen",
            Prioritaet = dto.Prioritaet,
            Kategorie = dto.Kategorie,
            Faelligkeitsdatum = dto.Faelligkeitsdatum,
            ZugewiesenAnId = dto.ZugewiesenAnId,
            KundeId = dto.KundeId,
            ProjektId = dto.ProjektId
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Ticket erstellt | Id: {Id} | Titel: {Titel}", ticket.Id, ticket.Titel);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, new { ticket.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TicketRequestDto dto)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id && !t.IstGeloescht);
        if (ticket is null) return NotFound(new { Nachricht = "Ticket nicht gefunden." });

        ticket.Titel = dto.Titel;
        ticket.Beschreibung = dto.Beschreibung;
        ticket.Prioritaet = dto.Prioritaet;
        ticket.Kategorie = dto.Kategorie;
        ticket.Faelligkeitsdatum = dto.Faelligkeitsdatum;
        ticket.ZugewiesenAnId = dto.ZugewiesenAnId;
        if (dto.KundeId != Guid.Empty)
            ticket.KundeId = dto.KundeId;
        ticket.ProjektId = dto.ProjektId;
        ticket.AktualisiertAm = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Ticket aktualisiert | Id: {Id}", id);
        return NoContent();
    }

    /// <summary>
    /// Durum değiştirme + email bildirimi (ADIM 5.3)
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] TicketStatusChangeDto? dto, [FromQuery] string? status = null)
    {
        var neuerStatus = !string.IsNullOrWhiteSpace(dto?.NeuerStatus)
            ? dto!.NeuerStatus
            : status;

        if (string.IsNullOrWhiteSpace(neuerStatus))
            return BadRequest(new { Nachricht = "Neuer Status fehlt." });

        var ticket = await _db.Tickets
            .Include(t => t.ZugewiesenAn)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IstGeloescht);

        if (ticket is null) return NotFound(new { Nachricht = "Ticket nicht gefunden." });

        var alterStatus = ticket.Status;
        ticket.Status = neuerStatus;
        ticket.AktualisiertAm = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Ticket Status geändert | Id: {Id} | {Alt} → {Neu}", id, alterStatus, neuerStatus);

        // Email bildirimi
        if (ticket.ZugewiesenAn?.Email is not null)
        {
            try
            {
                await _emailService.SendStatusChangeAsync(ticket.ZugewiesenAn.Email, ticket.Titel, alterStatus, neuerStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Status-E-Mail fehlgeschlagen | TicketId: {Id}", id);
            }
        }

        // SignalR push bildirimi
        await _hub.Clients.Group($"ticket-{id}").SendAsync("TicketStatusChanged", new { TicketId = id, AlterStatus = alterStatus, NeuerStatus = neuerStatus });

        return Ok(new { Nachricht = $"Status: {alterStatus} → {neuerStatus}" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id && !t.IstGeloescht);
        if (ticket is null) return NotFound(new { Nachricht = "Ticket nicht gefunden." });

        ticket.IstGeloescht = true;
        ticket.AktualisiertAm = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Ticket soft-deleted | Id: {Id}", id);
        return NoContent();
    }

    private Guid? GetMandantId()
    {
        var claim = User.FindFirst("MandantId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public class TicketStatusChangeDto
{
    public string NeuerStatus { get; set; } = string.Empty;
}
