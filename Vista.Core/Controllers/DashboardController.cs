using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vista.Core.Data;

namespace Vista.Core.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetStatistics()
    {
        var kundenAnzahl = await _db.Kunden.CountAsync(k => !k.IstGeloescht);
        var projekteAnzahl = await _db.Projekte.CountAsync(p => !p.IstGeloescht);
        var ticketsOffen = await _db.Tickets.CountAsync(t => !t.IstGeloescht && t.Status == "Offen");
        var ticketsGesamt = await _db.Tickets.CountAsync(t => !t.IstGeloescht);

        var projekteNachStatus = await _db.Projekte
            .Where(p => !p.IstGeloescht)
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Anzahl = g.Count() })
            .ToListAsync();

        var ticketsNachPrioritaet = await _db.Tickets
            .Where(t => !t.IstGeloescht)
            .GroupBy(t => t.Prioritaet)
            .Select(g => new { Prioritaet = g.Key, Anzahl = g.Count() })
            .ToListAsync();

        var letzteTickets = await _db.Tickets
            .Where(t => !t.IstGeloescht)
            .OrderByDescending(t => t.ErstelltAm)
            .Take(5)
            .Select(t => new { t.Id, t.Titel, t.Status, t.Prioritaet, t.ErstelltAm })
            .ToListAsync();

        return Ok(new
        {
            KundenAnzahl = kundenAnzahl,
            ProjekteAnzahl = projekteAnzahl,
            TicketsOffen = ticketsOffen,
            TicketsGesamt = ticketsGesamt,
            ProjekteNachStatus = projekteNachStatus,
            TicketsNachPrioritaet = ticketsNachPrioritaet,
            LetzteTickets = letzteTickets
        });
    }
}
