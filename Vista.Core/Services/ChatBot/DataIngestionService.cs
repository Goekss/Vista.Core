using Microsoft.EntityFrameworkCore;
using Microsoft.KernelMemory;
using Vista.Core.Data;

namespace Vista.Core.Services.ChatBot;

public class DataIngestionService
{
    private readonly AppDbContext _db;
    private readonly IKernelMemory _memory;
    private readonly ILogger<DataIngestionService> _logger;

    public DataIngestionService(AppDbContext db, IKernelMemory memory, ILogger<DataIngestionService> logger)
    {
        _db = db;
        _memory = memory;
        _logger = logger;
    }

    public async Task AlleDatenIndizieren()
    {
        _logger.LogInformation("VIKA | Data Ingestion gestartet...");

        await KundenIndizieren();
        await TicketsIndizieren();
        await ProjekteIndizieren();

        _logger.LogInformation("VIKA | Data Ingestion erfolgreich abgeschlossen.");
    }

    private async Task KundenIndizieren()
    {
        var kunden = await _db.Kunden.Where(k => !k.IstGeloescht).ToListAsync();
        foreach (var k in kunden)
        {
            var text = $"Kunde: {k.Unternehmen}\nName: {k.Vorname} {k.Nachname}\nEmail: {k.Email}\nTelefon: {k.TelefonMobil}\nAdresse: {k.Adresse}";
            await _memory.ImportTextAsync(text, documentId: $"kunde_{k.Id}", tags: new TagCollection { { "typ", "kunde" }, { "mandantId", k.MandantId.ToString() } });
        }
        _logger.LogInformation("VIKA | {Count} Kunden indiziert.", kunden.Count);
    }

    private async Task TicketsIndizieren()
    {
        var tickets = await _db.Tickets.Include(t => t.Kunde).Where(t => !t.IstGeloescht).ToListAsync();
        foreach (var t in tickets)
        {
            var kunde = t.Kunde != null ? t.Kunde.Unternehmen : "Kein Kunde";
            var text = $"Ticket: {t.Titel}\nStatus: {t.Status}\nPriorität: {t.Prioritaet}\nKunde: {kunde}\nBeschreibung: {t.Beschreibung}";
            await _memory.ImportTextAsync(text, documentId: $"ticket_{t.Id}", tags: new TagCollection { { "typ", "ticket" }, { "mandantId", t.MandantId.ToString() } });
        }
        _logger.LogInformation("VIKA | {Count} Tickets indiziert.", tickets.Count);
    }

    private async Task ProjekteIndizieren()
    {
        var projekte = await _db.Projekte.Include(p => p.Kunde).Where(p => !p.IstGeloescht).ToListAsync();
        foreach (var p in projekte)
        {
            var kunde = p.Kunde != null ? p.Kunde.Unternehmen : "Kein Kunde";
            var text = $"Projekt: {p.Name}\nStatus: {p.Status}\nFortschritt: {p.AbschlussInProzent}%\nKunde: {kunde}\nBeschreibung: {p.Beschreibung}";
            await _memory.ImportTextAsync(text, documentId: $"projekt_{p.Id}", tags: new TagCollection { { "typ", "projekt" }, { "mandantId", p.MandantId.ToString() } });
        }
        _logger.LogInformation("VIKA | {Count} Projekte indiziert.", projekte.Count);
    }
}
