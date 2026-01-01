using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Vista.Core.Data;

namespace Vista.Core.Plugins;

public class TicketPlugin
{
    private readonly AppDbContext _db;

    public TicketPlugin(AppDbContext db) => _db = db;

    [KernelFunction("offene_tickets")]
    [Description("Listet alle offenen Tickets auf. Optional nach Kunde oder Priorität filtern.")]
    public async Task<string> OffeneTickets(
        [Description("Optional: Kundenname zum Filtern")] string? kundeName = null,
        [Description("Optional: Priorität (Niedrig, Mittel, Hoch, Kritisch)")] string? prioritaet = null)
    {
        var query = _db.Tickets
            .Include(t => t.Kunde)
            .Where(t => !t.IstGeloescht && t.Status != "Geschlossen");

        if (!string.IsNullOrWhiteSpace(kundeName))
            query = query.Where(t => t.Kunde != null && t.Kunde.Unternehmen.Contains(kundeName));

        if (!string.IsNullOrWhiteSpace(prioritaet))
            query = query.Where(t => t.Prioritaet == prioritaet);

        var tickets = await query
            .OrderByDescending(t => t.ErstelltAm)
            .Take(10)
            .Select(t => new
            {
                t.Id,
                t.Titel,
                t.Status,
                t.Prioritaet,
                Kunde = t.Kunde != null ? t.Kunde.Unternehmen : "—",
                t.Faelligkeitsdatum
            })
            .ToListAsync();

        if (tickets.Count == 0)
            return "Keine offenen Tickets gefunden.";

        return string.Join("\n", tickets.Select(t =>
            $"• [{t.Prioritaet}] {t.Titel} | Status: {t.Status} | Kunde: {t.Kunde} | Fällig: {t.Faelligkeitsdatum?.ToString("dd.MM.yyyy") ?? "—"}"));
    }

    [KernelFunction("ticket_statistik")]
    [Description("Gibt eine Statistik der Tickets zurück: Anzahl pro Status und Priorität.")]
    public async Task<string> TicketStatistik()
    {
        var tickets = await _db.Tickets
            .Where(t => !t.IstGeloescht)
            .ToListAsync();

        var nachStatus = tickets.GroupBy(t => t.Status)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();

        var nachPrioritaet = tickets.GroupBy(t => t.Prioritaet)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();

        return $"Tickets gesamt: {tickets.Count}\n" +
               $"Nach Status: {string.Join(", ", nachStatus)}\n" +
               $"Nach Priorität: {string.Join(", ", nachPrioritaet)}";
    }

    [KernelFunction("ticket_suchen")]
    [Description("Sucht Tickets nach Titel oder Beschreibung.")]
    public async Task<string> TicketSuchen(
        [Description("Suchbegriff im Ticket-Titel oder Beschreibung")] string suchbegriff)
    {
        var tickets = await _db.Tickets
            .Include(t => t.Kunde)
            .Where(t => !t.IstGeloescht &&
                (t.Titel.Contains(suchbegriff) || t.Beschreibung.Contains(suchbegriff)))
            .Take(5)
            .Select(t => new
            {
                t.Titel,
                t.Status,
                t.Prioritaet,
                Kunde = t.Kunde != null ? t.Kunde.Unternehmen : "—"
            })
            .ToListAsync();

        if (tickets.Count == 0)
            return $"Keine Tickets mit '{suchbegriff}' gefunden.";

        return string.Join("\n", tickets.Select(t =>
            $"• [{t.Prioritaet}] {t.Titel} | {t.Status} | Kunde: {t.Kunde}"));
    }
}
