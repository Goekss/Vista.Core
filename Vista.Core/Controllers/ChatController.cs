using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Vista.Core.Data;
using Vista.Core.Hubs;
using Vista.Core.Services;

namespace Vista.Core.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly FileStorageService _fileService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(AppDbContext db, FileStorageService fileService, IHubContext<ChatHub> hubContext)
    {
        _db = db;
        _fileService = fileService;
        _hubContext = hubContext;
    }

    private Guid RequireMandantId()
    {
        var mandantId = GetMandantId();
        if (mandantId == null)
            throw new Exception("MandantId Claim fehlt");
        return mandantId.Value;
    }

    [HttpGet("raeume")]
    public async Task<IActionResult> GetRaeume()
    {
        try
        {
            var mandantId = RequireMandantId();
            var raeume = await _db.ChatRaeume
                .AsNoTracking()
                .Where(r => !r.IstGeloescht && r.MandantId == mandantId)
                .Include(r => r.Projekt!).ThenInclude(p => p.Benutzer)
                .Include(r => r.Ticket!).ThenInclude(t => t.ZugewiesenAn)
                .Include(r => r.Benutzer1)
                .Include(r => r.Benutzer2)
                .OrderBy(r => r.Name)
                .ToListAsync();

            var result = raeume.Select(r =>
            {
                var teilnehmer = new List<object>();
                if (r.Projekt != null)
                {
                    teilnehmer.AddRange(r.Projekt.Benutzer.Select(b => (object)new
                    {
                        id = b.Id,
                        vorname = b.Vorname,
                        nachname = b.Nachname,
                        bild = b.Bild
                    }));
                }
                else if (r.Ticket?.ZugewiesenAn != null)
                {
                    teilnehmer.Add(new
                    {
                        id = r.Ticket.ZugewiesenAn.Id,
                        vorname = r.Ticket.ZugewiesenAn.Vorname,
                        nachname = r.Ticket.ZugewiesenAn.Nachname,
                        bild = r.Ticket.ZugewiesenAn.Bild
                    });
                }
                else if (r.IstDirektChat)
                {
                    if (r.Benutzer1 != null)
                        teilnehmer.Add(new { id = r.Benutzer1.Id, vorname = r.Benutzer1.Vorname, nachname = r.Benutzer1.Nachname, bild = r.Benutzer1.Bild });
                    if (r.Benutzer2 != null)
                        teilnehmer.Add(new { id = r.Benutzer2.Id, vorname = r.Benutzer2.Vorname, nachname = r.Benutzer2.Nachname, bild = r.Benutzer2.Bild });
                }

                return new
                {
                    id = r.Id,
                    name = r.Name,
                    projektId = r.ProjektId,
                    ticketId = r.TicketId,
                    istDirektChat = r.IstDirektChat,
                    teilnehmer
                };
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ChatController.GetRaeume Error: {ex}");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("raum/{raumId}/nachrichten")]
    public async Task<IActionResult> GetNachrichten(Guid raumId, [FromQuery] int page = 1, [FromQuery] int size = 50)
    {
        try
        {
            var mandantId = RequireMandantId();
            var raum = await _db.ChatRaeume
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == raumId && r.MandantId == mandantId && !r.IstGeloescht);
            if (raum == null) return NotFound();

            if (size < 1) size = 50;
            if (size > 200) size = 200;
            if (page < 1) page = 1;

            var query = _db.ChatNachrichten
                .AsNoTracking()
                .Where(n => n.RaumId == raumId);

            var total = await query.CountAsync();

            var slice = await query
                .OrderByDescending(n => n.GeschicktAm)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(n => new
                {
                    id = n.Id,
                    inhalt = n.Inhalt,
                    geschicktAm = n.GeschicktAm,
                    istDatei = n.IstDatei,
                    dateiPfad = n.DateiPfad,
                    dateiName = n.DateiName,
                    dateiTyp = n.DateiTyp,
                    dateiGroesse = n.DateiGroesse,
                    absenderId = n.AbsenderId,
                    absender = n.Absender == null ? null : new
                    {
                        vorname = n.Absender.Vorname,
                        nachname = n.Absender.Nachname,
                        bild = n.Absender.Bild
                    }
                })
                .ToListAsync();

            var items = slice.OrderBy(n => n.geschicktAm).ToList();

            return Ok(new { total, page, size, items });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ChatController Error: {ex}");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("direktchat/{zielBenutzerId}")]
    public async Task<IActionResult> GetOrCreateDirektChat(string zielBenutzerId)
    {
        try
        {
            var mandantId = RequireMandantId();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (userId == zielBenutzerId) return BadRequest("Kann keinen Chat mit sich selbst starten");

            // Sortiere IDs, damit Chat-Räume konsistent sind
            var user1Id = string.Compare(userId, zielBenutzerId) < 0 ? userId : zielBenutzerId;
            var user2Id = string.Compare(userId, zielBenutzerId) < 0 ? zielBenutzerId : userId;

            var existingRaum = await _db.ChatRaeume
                .FirstOrDefaultAsync(r => r.IstDirektChat && r.MandantId == mandantId && 
                                          r.Benutzer1Id == user1Id && r.Benutzer2Id == user2Id && !r.IstGeloescht);

            if (existingRaum != null)
            {
                return Ok(new { id = existingRaum.Id });
            }

            // Neuen Raum erstellen
            var zielBenutzer = await _db.Users.FindAsync(zielBenutzerId);
            if (zielBenutzer == null) return NotFound("Ziel-Benutzer nicht gefunden");

            var aktuellerBenutzer = await _db.Users.FindAsync(userId);

            var neuerRaum = new Vista.Core.Models.ChatRaum
            {
                MandantId = mandantId,
                IstDirektChat = true,
                Name = $"Chat: {aktuellerBenutzer?.Vorname} & {zielBenutzer.Vorname}",
                Benutzer1Id = user1Id,
                Benutzer2Id = user2Id
            };

            _db.ChatRaeume.Add(neuerRaum);
            await _db.SaveChangesAsync();

            return Ok(new { id = neuerRaum.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("raum/{raumId}/datei")]
    public async Task<IActionResult> UploadFile(Guid raumId, IFormFile datei)
    {
        try
        {
            var mandantId = RequireMandantId();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var raum = await _db.ChatRaeume
                .FirstOrDefaultAsync(r => r.Id == raumId && r.MandantId == mandantId && !r.IstGeloescht);
            
            if (raum == null) return NotFound("Raum nicht gefunden");

            var uniqueId = Guid.NewGuid().ToString();
            var (success, fileUrl, error) = await _fileService.UploadFileAsync(datei, FileStorageService.ChatFolder, uniqueId);

            if (!success) return BadRequest(new { error });

            var nachricht = new Vista.Core.Models.ChatNachricht
            {
                MandantId = mandantId,
                RaumId = raumId,
                AbsenderId = userId,
                Inhalt = "Datei gesendet",
                IstDatei = true,
                DateiPfad = fileUrl,
                DateiName = datei.FileName,
                DateiTyp = datei.ContentType,
                DateiGroesse = datei.Length
            };

            _db.ChatNachrichten.Add(nachricht);
            await _db.SaveChangesAsync();

            var user = await _db.Users.FindAsync(userId);
            var payload = new
            {
                id = nachricht.Id,
                raumId = raumId,
                inhalt = nachricht.Inhalt,
                geschicktAm = nachricht.GeschicktAm,
                istDatei = nachricht.IstDatei,
                dateiPfad = nachricht.DateiPfad,
                dateiName = nachricht.DateiName,
                dateiTyp = nachricht.DateiTyp,
                dateiGroesse = nachricht.DateiGroesse,
                absenderId = userId,
                absender = user == null ? null : new { vorname = user.Vorname, nachname = user.Nachname, bild = user.Bild }
            };

            await _hubContext.Clients.Group(raumId.ToString()).SendAsync("ReceiveMessage", payload);

            return Ok(payload);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid? GetMandantId()
    {
        var claim = User.FindFirst("MandantId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
