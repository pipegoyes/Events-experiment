namespace BoxTracking.Shared.Models;

public record RecentEvent
{
    public required string EventId { get; init; }
    public required string BoxId { get; init; }
    public required string EventType { get; init; }
    public required string WorkerId { get; init; }
    public DateTime Timestamp { get; init; }
}
