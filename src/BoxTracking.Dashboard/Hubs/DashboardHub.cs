using Microsoft.AspNetCore.SignalR;
using BoxTracking.Shared.Events;
using BoxTracking.Shared.Models;
using BoxTracking.Dashboard.Services;

namespace BoxTracking.Dashboard.Hubs;

public class DashboardHub : Hub
{
    private readonly ILogger<DashboardHub> _logger;
    private readonly EventBroadcastService _broadcastService;

    public DashboardHub(ILogger<DashboardHub> logger, EventBroadcastService broadcastService)
    {
        _logger = logger;
        _broadcastService = broadcastService;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("EventProcessor connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("EventProcessor disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // Called by EventProcessor to broadcast events
    public Task BroadcastEvent(BoxEvent evt, DailyMetrics metrics)
    {
        _logger.LogInformation("Broadcasting event: {EventType} for {BoxId}", evt.EventType, evt.BoxId);
        _broadcastService.BroadcastEvent(evt, metrics);
        return Task.CompletedTask;
    }

    // Called by EventProcessor to update metrics only
    public Task BroadcastMetrics(DailyMetrics metrics)
    {
        // Not needed for now, events include metrics
        return Task.CompletedTask;
    }
}
