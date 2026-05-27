using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Hubs;

public class SlotHub : Hub
{
    public async Task JoinTurfGroup(string turfId)
        => await Groups.AddToGroupAsync(
            Context.ConnectionId, $"turf_{turfId}");

    public async Task LeaveTurfGroup(string turfId)
        => await Groups.RemoveFromGroupAsync(
            Context.ConnectionId, $"turf_{turfId}");
}
