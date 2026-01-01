using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Vista.Core.Services.ChatBot;

namespace Vista.Core.Hubs;

[Authorize]
public class VikaChatBotHub : Hub
{
    private readonly VikaChatBotService _vikaService;
    private readonly ILogger<VikaChatBotHub> _logger;

    public VikaChatBotHub(VikaChatBotService vikaService, ILogger<VikaChatBotHub> logger)
    {
        _vikaService = vikaService;
        _logger = logger;
    }

    public async Task FrageStellen(string nachricht)
    {
        var mandantId = GetMandantId();
        if (mandantId is null)
        {
            await Clients.Caller.SendAsync("VikaFehler", "MandantId fehlt.");
            return;
        }

        await Clients.Caller.SendAsync("VikaSchreibt", true);

        try
        {
            await foreach (var chunk in _vikaService.FrageStreamen(nachricht, mandantId.Value))
            {
                await Clients.Caller.SendAsync("VikaAntwortChunk", chunk);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VIKA Hub | Fehler");
            await Clients.Caller.SendAsync("VikaAntwortChunk", $"[VIKA Fehler: {ex.Message}]");
        }

        await Clients.Caller.SendAsync("VikaSchreibt", false);
        await Clients.Caller.SendAsync("VikaAntwortFertig");

        _logger.LogInformation("VIKA Hub | Frage beantwortet | MandantId: {MandantId}", mandantId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("VIKA Hub | Verbunden | ConnectionId: {Id}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("VIKA Hub | Getrennt | ConnectionId: {Id}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private Guid? GetMandantId()
    {
        var claim = Context.User?.FindFirst("MandantId")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
