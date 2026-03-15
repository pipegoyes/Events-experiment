using Microsoft.AspNetCore.SignalR;
using BoxTracking.Shared.Events;
using BoxTracking.Shared.Models;

namespace BoxTracking.Dashboard.Hubs;

public class DashboardHub : Hub
{
    private readonly ILogger<DashboardHub> _logger;

    public DashboardHub(ILogger<DashboardHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // Called by EventProcessor to broadcast events
    public async Task BroadcastEvent(BoxEvent evt, DailyMetrics metrics)
    {
        await Clients.All.SendAsync("ReceiveEvent", evt, metrics);
    }

    // Called by EventProcessor to update metrics only
    public async Task BroadcastMetrics(DailyMetrics metrics)
    {
        await Clients.All.SendAsync("ReceiveMetrics", metrics);
    }
}
