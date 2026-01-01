using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vista.Core.DTOs.ChatBot;
using Vista.Core.Services.ChatBot;

namespace Vista.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class VikaAdminController : ControllerBase
{
    private readonly DataIngestionService _dataIngestionService;
    private readonly VikaChatBotService _vikaService;

    public VikaAdminController(DataIngestionService dataIngestionService, VikaChatBotService vikaService)
    {
        _dataIngestionService = dataIngestionService;
        _vikaService = vikaService;
    }

    [HttpPost("daten-indizieren")]
    public async Task<IActionResult> DatenIndizieren()
    {
        await _dataIngestionService.AlleDatenIndizieren();
        return Ok(new { Message = "Data Ingestion erfolgreich gestartet und abgeschlossen." });
    }

    [HttpPost("test-frage")]
    public async Task<ActionResult<ChatBotResponseDto>> TestFrage([FromBody] ChatBotRequestDto request)
    {
        var mandantIdClaim = User.Claims.FirstOrDefault(c => c.Type == "MandantId");
        if (mandantIdClaim == null || !Guid.TryParse(mandantIdClaim.Value, out var mandantId))
        {
            return BadRequest("MandantId fehlt.");
        }

        var result = await _vikaService.FrageStellen(request.Nachricht, mandantId);
        return Ok(result);
    }
}
