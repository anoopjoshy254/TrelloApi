using Microsoft.AspNetCore.SignalR;

namespace TrelloApi.Hubs;

public class BoardHub : Hub
{
    public async Task JoinBoard(string projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, projectId);
    }

    public async Task LeaveBoard(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, projectId);
    }
}
