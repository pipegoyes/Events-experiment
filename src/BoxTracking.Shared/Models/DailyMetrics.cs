namespace BoxTracking.Shared.Models;

public record DailyMetrics
{
    public DateTime Date { get; init; } = DateTime.UtcNow.Date;
    public int BoxesCleaned { get; init; }
    public int BoxesRepaired { get; init; }
    public int BoxesLoaded { get; init; }
    public int BoxesInProgress { get; init; }
    public int LoadingAttemptsFailed { get; init; }
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}

public record BoxStatus
{
    public string BoxId { get; init; } = string.Empty;
    public string CurrentState { get; init; } = string.Empty;
    public string? LastWorker { get; init; }
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}

public record RecentEvent
{
    public string EventId { get; init; } = string.Empty;
    public string BoxId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string WorkerId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}
