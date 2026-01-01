using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Vista.Core.Hubs;

[Authorize]
public class BenachrichtigungHub : Hub
{
    public async Task JoinTicketGroup(string ticketId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
    }

    public async Task LeaveTicketGroup(string ticketId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
    }

    public async Task JoinProjektGroup(string projektId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"projekt-{projektId}");
    }
}
