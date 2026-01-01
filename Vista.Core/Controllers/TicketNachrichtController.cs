using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vista.Core.Data;
using Vista.Core.Models;

namespace Vista.Core.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TicketNachrichtController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<TicketNachrichtController> _logger;

    public TicketNachrichtController(AppDbContext db, ILogger<TicketNachrichtController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("ticket/{ticketId}")]
    public async Task<IActionResult> GetByTicket(Guid ticketId)
    {
        var list = await _db.TicketNachrichten
            .Where(n => n.TicketId == ticketId && !n.IstGeloescht)
            .OrderBy(n => n.GeschicktAm)
            .Select(n => new
            {
                n.Id,
                n.Inhalt,
                n.IstInternNotiz,
                n.GeschicktAm,
                Absender = n.Absender == null ? null : new { n.Absender.Vorname, n.Absender.Nachname }
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TicketNachrichtRequestDto dto)
    {
        var mandantId = GetMandantId();
        if (mandantId is null) return BadRequest(new { Nachricht = "MandantId fehlt." });

        var benutzerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (benutzerId is null) return Unauthorized();

        var nachricht = new TicketNachricht
        {
            MandantId = mandantId.Value,
            TicketId = dto.TicketId,
            AbsenderId = benutzerId,
            Inhalt = dto.Inhalt,
            IstInternNotiz = dto.IstInternNotiz
        };

        _db.TicketNachrichten.Add(nachricht);
        await _db.SaveChangesAsync();

        _logger.LogInformation("TicketNachricht erstellt | TicketId: {TicketId} | Id: {Id}", dto.TicketId, nachricht.Id);
        return Created("", new { nachricht.Id });
    }

    private Guid? GetMandantId()
    {
        var claim = User.FindFirst("MandantId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public class TicketNachrichtRequestDto
{
    public Guid TicketId { get; set; }
    public string Inhalt { get; set; } = string.Empty;
    public bool IstInternNotiz { get; set; }
}
