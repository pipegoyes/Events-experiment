namespace BoxTracking.Shared.Events;

public record BoxEvent
{
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public string BoxId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string WorkerId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, string> Metadata { get; init; } = new();
}

// Specific event types
public record BoxCleaningStarted : BoxEvent
{
    public BoxCleaningStarted()
    {
        EventType = nameof(BoxCleaningStarted);
    }
}

public record BoxCleaningCompleted : BoxEvent
{
    public BoxCleaningCompleted()
    {
        EventType = nameof(BoxCleaningCompleted);
    }
}

public record BoxRepairStarted : BoxEvent
{
    public BoxRepairStarted()
    {
        EventType = nameof(BoxRepairStarted);
    }
}

public record BoxRepairCompleted : BoxEvent
{
    public BoxRepairCompleted()
    {
        EventType = nameof(BoxRepairCompleted);
    }
}

public record BoxLoadingAttempted : BoxEvent
{
    public bool Success { get; init; }
    public string? FailureReason { get; init; }
    
    public BoxLoadingAttempted()
    {
        EventType = nameof(BoxLoadingAttempted);
    }
}
