using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vista.Core.Data;
using Vista.Core.Models;

namespace Vista.Core.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BerichtController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<BerichtController> _logger;

    public BerichtController(AppDbContext db, IWebHostEnvironment env, ILogger<BerichtController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.Berichte
            .Where(b => !b.IstGeloescht)
            .OrderByDescending(b => b.HochgeladenAm)
            .Select(b => new
            {
                b.Id,
                b.Titel,
                b.DateiTyp,
                b.Version,
                b.HochgeladenAm,
                b.BearbeitetAm
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Upload([FromForm] string titel, [FromForm] string version, IFormFile datei)
    {
        var mandantId = GetMandantId();
        if (mandantId is null) return BadRequest(new { Nachricht = "MandantId fehlt." });

        if (datei is null || datei.Length == 0)
            return BadRequest(new { Nachricht = "Keine Datei hochgeladen." });

        var bericht = new Bericht
        {
            MandantId = mandantId.Value,
            Titel = titel,
            DateiTyp = datei.ContentType,
            Version = version
        };

        var ordner = Path.Combine(_env.ContentRootPath, "Storage", "berichte", bericht.Id.ToString());
        Directory.CreateDirectory(ordner);
        var pfad = Path.Combine(ordner, datei.FileName);

        using (var stream = System.IO.File.Create(pfad))
            await datei.CopyToAsync(stream);

        bericht.DateiPfad = pfad;

        _db.Berichte.Add(bericht);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Bericht hochgeladen | Id: {Id} | Datei: {Name}", bericht.Id, datei.FileName);
        return CreatedAtAction(nameof(Download), new { id = bericht.Id }, new { bericht.Id });
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var bericht = await _db.Berichte.FirstOrDefaultAsync(b => b.Id == id && !b.IstGeloescht);
        if (bericht is null) return NotFound(new { Nachricht = "Bericht nicht gefunden." });

        if (!System.IO.File.Exists(bericht.DateiPfad))
            return NotFound(new { Nachricht = "Datei nicht gefunden." });

        var bytes = await System.IO.File.ReadAllBytesAsync(bericht.DateiPfad);
        var fileName = Path.GetFileName(bericht.DateiPfad);
        return File(bytes, bericht.DateiTyp, fileName);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var bericht = await _db.Berichte.FirstOrDefaultAsync(b => b.Id == id && !b.IstGeloescht);
        if (bericht is null) return NotFound(new { Nachricht = "Bericht nicht gefunden." });

        bericht.IstGeloescht = true;
        bericht.BearbeitetAm = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var ordner = Path.Combine(_env.ContentRootPath, "Storage", "berichte", id.ToString());
        if (Directory.Exists(ordner))
            Directory.Delete(ordner, true);

        _logger.LogInformation("Bericht gelöscht | Id: {Id}", id);
        return NoContent();
    }

    private Guid? GetMandantId()
    {
        var claim = User.FindFirst("MandantId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
