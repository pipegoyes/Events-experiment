using System.Net.Http.Json;
using BoxTracking.Shared.Events;

namespace BoxTracking.EventSimulator.Services;

public class EventService
{
    private readonly HttpClient _httpClient;
    private readonly Random _random = new();

    public EventService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(bool Success, string? Error)> SendEventAsync(BoxEvent boxEvent)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/events", boxEvent);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return (false, $"API returned {response.StatusCode}: {errorContent}");
        }
        catch (Exception ex)
        {
            return (false, $"HTTP error: {ex.Message}");
        }
    }

    public BoxEvent GenerateRandomEvent(string? boxId = null, string? workerId = null)
    {
        var eventTypes = new[]
        {
            "BoxCleaningStarted",
            "BoxCleaningCompleted",
            "BoxRepairStarted",
            "BoxRepairCompleted",
            "BoxLoadingAttempted"
        };

        return new BoxEvent
        {
            EventId = Guid.NewGuid().ToString(),
            BoxId = boxId ?? $"BOX-{_random.Next(1, 100):D3}",
            WorkerId = workerId ?? $"WORKER-{_random.Next(1, 20):D2}",
            EventType = eventTypes[_random.Next(eventTypes.Length)],
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<(List<BoxEvent> Events, List<string> Errors)> GenerateAndSendBatchAsync(int count)
    {
        var events = new List<BoxEvent>();
        var errors = new List<string>();
        
        for (int i = 0; i < count; i++)
        {
            var evt = GenerateRandomEvent();
            var (success, error) = await SendEventAsync(evt);
            
            if (success)
            {
                events.Add(evt);
            }
            else if (error != null)
            {
                errors.Add($"{evt.BoxId}: {error}");
            }
            
            // Small delay to avoid overwhelming the API
            await Task.Delay(100);
        }

        return (events, errors);
    }

    public async Task<(List<BoxEvent> Events, List<string> Errors)> SimulateBoxLifecycleAsync(string boxId, string workerId)
    {
        var events = new List<BoxEvent>();
        var errors = new List<string>();
        
        // 1. Cleaning Started
        var cleaningStarted = new BoxEvent
        {
            EventId = Guid.NewGuid().ToString(),
            BoxId = boxId,
            WorkerId = workerId,
            EventType = "BoxCleaningStarted",
            Timestamp = DateTime.UtcNow
        };
        var (success1, error1) = await SendEventAsync(cleaningStarted);
        if (success1) events.Add(cleaningStarted);
        else if (error1 != null) errors.Add(error1);
        await Task.Delay(2000);

        // 2. Cleaning Completed
        var cleaningCompleted = new BoxEvent
        {
            EventId = Guid.NewGuid().ToString(),
            BoxId = boxId,
            WorkerId = workerId,
            EventType = "BoxCleaningCompleted",
            Timestamp = DateTime.UtcNow
        };
        var (success2, error2) = await SendEventAsync(cleaningCompleted);
        if (success2) events.Add(cleaningCompleted);
        else if (error2 != null) errors.Add(error2);
        await Task.Delay(1000);

        // 3. Loading Attempted
        var loadingAttempted = new BoxEvent
        {
            EventId = Guid.NewGuid().ToString(),
            BoxId = boxId,
            WorkerId = $"WORKER-{_random.Next(1, 20):D2}",
            EventType = "BoxLoadingAttempted",
            Timestamp = DateTime.UtcNow
        };
        var (success3, error3) = await SendEventAsync(loadingAttempted);
        if (success3) events.Add(loadingAttempted);
        else if (error3 != null) errors.Add(error3);

        return (events, errors);
    }
}
