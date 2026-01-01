using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Vista.Core.Data;

namespace Vista.Core.Plugins;

public class ProjektPlugin
{
    private readonly AppDbContext _db;

    public ProjektPlugin(AppDbContext db) => _db = db;

    [KernelFunction("aktive_projekte")]
    [Description("Listet alle aktiven (nicht abgeschlossenen) Projekte auf. Optional nach Kunde filtern.")]
    public async Task<string> AktiveProjekte(
        [Description("Optional: Kundenname zum Filtern")] string? kundeName = null)
    {
        var query = _db.Projekte
            .Include(p => p.Kunde)
            .Where(p => !p.IstGeloescht && !p.IstAbgeschlossen);

        if (!string.IsNullOrWhiteSpace(kundeName))
            query = query.Where(p => p.Kunde != null && p.Kunde.Unternehmen.Contains(kundeName));

        var projekte = await query
            .OrderByDescending(p => p.ErstelltAm)
            .Take(10)
            .Select(p => new
            {
                p.Name,
                p.Status,
                p.Prioritaet,
                p.AbschlussInProzent,
                Kunde = p.Kunde != null ? p.Kunde.Unternehmen : "—",
                p.Startdatum,
                p.Enddatum
            })
            .ToListAsync();

        if (projekte.Count == 0)
            return "Keine aktiven Projekte gefunden.";

        return string.Join("\n", projekte.Select(p =>
            $"• {p.Name} | {p.Status} | {p.AbschlussInProzent}% | Kunde: {p.Kunde} | {p.Startdatum:dd.MM.yyyy} - {p.Enddatum?.ToString("dd.MM.yyyy") ?? "—"}"));
    }

    [KernelFunction("projekt_statistik")]
    [Description("Gibt eine Übersicht der Projekte zurück: Anzahl pro Status und durchschnittlicher Fortschritt.")]
    public async Task<string> ProjektStatistik()
    {
        var projekte = await _db.Projekte
            .Where(p => !p.IstGeloescht)
            .ToListAsync();

        var nachStatus = projekte.GroupBy(p => p.Status)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();

        var durchschnitt = projekte.Any()
            ? projekte.Average(p => p.AbschlussInProzent)
            : 0;

        return $"Projekte gesamt: {projekte.Count}\n" +
               $"Nach Status: {string.Join(", ", nachStatus)}\n" +
               $"Durchschnittlicher Fortschritt: {durchschnitt:F0}%";
    }

    [KernelFunction("projekt_suchen")]
    [Description("Sucht Projekte nach Name oder Beschreibung.")]
    public async Task<string> ProjektSuchen(
        [Description("Suchbegriff im Projektnamen oder Beschreibung")] string suchbegriff)
    {
        var projekte = await _db.Projekte
            .Include(p => p.Kunde)
            .Where(p => !p.IstGeloescht &&
                (p.Name.Contains(suchbegriff) || p.Beschreibung.Contains(suchbegriff)))
            .Take(5)
            .Select(p => new
            {
                p.Name,
                p.Status,
                p.AbschlussInProzent,
                Kunde = p.Kunde != null ? p.Kunde.Unternehmen : "—"
            })
            .ToListAsync();

        if (projekte.Count == 0)
            return $"Keine Projekte mit '{suchbegriff}' gefunden.";

        return string.Join("\n", projekte.Select(p =>
            $"• {p.Name} | {p.Status} | {p.AbschlussInProzent}% | Kunde: {p.Kunde}"));
    }
}
