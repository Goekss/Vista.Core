using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Vista.Core.Data;

namespace Vista.Core.Plugins;

public class KundePlugin
{
    private readonly AppDbContext _db;

    public KundePlugin(AppDbContext db) => _db = db;

    [KernelFunction("kunde_suchen")]
    [Description("Sucht Kunden nach Name oder Unternehmen. Gibt eine Liste von Kunden mit ihren Kontaktdaten zurück.")]
    public async Task<string> KundeSuchen(
        [Description("Suchbegriff: Kundenname oder Unternehmensname")] string suchbegriff)
    {
        var kunden = await _db.Kunden
            .Where(k => !k.IstGeloescht &&
                (k.Unternehmen.Contains(suchbegriff) ||
                 k.Vorname.Contains(suchbegriff) ||
                 k.Nachname.Contains(suchbegriff)))
            .Take(5)
            .Select(k => new
            {
                k.Id,
                k.Unternehmen,
                Name = $"{k.Vorname} {k.Nachname}",
                k.Email,
                k.TelefonMobil,
                k.Adresse
            })
            .ToListAsync();

        if (kunden.Count == 0)
            return $"Keine Kunden mit '{suchbegriff}' gefunden.";

        return string.Join("\n", kunden.Select(k =>
            $"• {k.Unternehmen} | {k.Name} | {k.Email} | {k.TelefonMobil} | {k.Adresse}"));
    }

    [KernelFunction("kunde_anzahl")]
    [Description("Gibt die Gesamtanzahl der aktiven Kunden zurück.")]
    public async Task<string> KundeAnzahl()
    {
        var anzahl = await _db.Kunden.CountAsync(k => !k.IstGeloescht);
        return $"Aktive Kunden: {anzahl}";
    }

    [KernelFunction("kunde_details")]
    [Description("Gibt detaillierte Informationen zu einem Kunden anhand seiner ID zurück, inklusive Filialen und Ansprechpartner.")]
    public async Task<string> KundeDetails(
        [Description("Die Kunden-ID (GUID)")] string kundeId)
    {
        if (!Guid.TryParse(kundeId, out var id))
            return "Ungültige Kunden-ID.";

        var kunde = await _db.Kunden
            .Include(k => k.Filialen.Where(f => !f.IstGeloescht))
            .Include(k => k.Ansprechpartner.Where(a => !a.IstGeloescht))
            .FirstOrDefaultAsync(k => k.Id == id && !k.IstGeloescht);

        if (kunde is null)
            return "Kunde nicht gefunden.";

        var result = $"Kunde: {kunde.Unternehmen} | {kunde.Vorname} {kunde.Nachname}\n" +
                     $"Email: {kunde.Email} | Tel: {kunde.TelefonMobil}\n" +
                     $"Adresse: {kunde.Adresse} | Website: {kunde.Website}\n";

        if (kunde.Filialen.Any())
            result += $"Filialen ({kunde.Filialen.Count}): {string.Join(", ", kunde.Filialen.Select(f => f.Name))}\n";

        if (kunde.Ansprechpartner.Any())
            result += $"Ansprechpartner ({kunde.Ansprechpartner.Count}): {string.Join(", ", kunde.Ansprechpartner.Select(a => $"{a.Name} ({a.Email})"))}";

        return result;
    }
}
