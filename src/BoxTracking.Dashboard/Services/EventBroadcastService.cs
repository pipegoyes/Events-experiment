using BoxTracking.Shared.Events;
using BoxTracking.Shared.Models;

namespace BoxTracking.Dashboard.Services;

public class EventBroadcastService
{
    private DailyMetrics _metrics = new();
    private readonly List<RecentEvent> _recentEvents = new();
    private readonly object _lock = new();
    
    public event Action<BoxEvent, DailyMetrics>? OnEventReceived;

    public void BroadcastEvent(BoxEvent evt, DailyMetrics metrics)
    {
        lock (_lock)
        {
            _metrics = metrics;
            
            _recentEvents.Insert(0, new RecentEvent
            {
                EventId = evt.EventId,
                BoxId = evt.BoxId,
                EventType = evt.EventType,
                WorkerId = evt.WorkerId,
                Timestamp = evt.Timestamp
            });

            if (_recentEvents.Count > 20)
                _recentEvents.RemoveAt(_recentEvents.Count - 1);
        }

        OnEventReceived?.Invoke(evt, metrics);
    }

    public DailyMetrics GetMetrics()
    {
        lock (_lock)
        {
            return _metrics;
        }
    }

    public List<RecentEvent> GetRecentEvents()
    {
        lock (_lock)
        {
            return _recentEvents.ToList();
        }
    }
}
