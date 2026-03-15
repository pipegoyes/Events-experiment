using System.Text;
using System.Text.Json;
using BoxTracking.Shared.Events;
using BoxTracking.Shared.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Sentry;
using Microsoft.AspNetCore.SignalR.Client;

var builder = Host.CreateApplicationBuilder(args);

// Add Sentry (for Worker Services, use logging extension)
builder.Logging.AddSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"];
    options.Environment = builder.Environment.EnvironmentName;
    options.TracesSampleRate = 1.0;
    options.SendDefaultPii = false;
    options.Release = $"boxtracking-processor@{builder.Configuration["Version"] ?? "1.0.0"}";
});

// Add RabbitMQ connection
builder.Services.AddSingleton<IConnection>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var factory = new ConnectionFactory
    {
        HostName = config["RabbitMQ:Host"] ?? "localhost",
        Port = int.Parse(config["RabbitMQ:Port"] ?? "5672"),
        UserName = config["RabbitMQ:Username"] ?? "guest",
        Password = config["RabbitMQ:Password"] ?? "guest"
    };
    
    // Retry connection
    var maxRetries = 10;
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return factory.CreateConnection();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to RabbitMQ (attempt {i + 1}/{maxRetries}): {ex.Message}");
            if (i < maxRetries - 1)
                Thread.Sleep(3000);
            else
                throw;
        }
    }
    
    throw new Exception("Could not connect to RabbitMQ");
});

// Add metrics service (in-memory for prototype)
builder.Services.AddSingleton<MetricsService>();

// Add event processor
builder.Services.AddHostedService<EventProcessorService>();

var host = builder.Build();
host.Run();

// Metrics Service (in-memory)
public class MetricsService
{
    private DailyMetrics _metrics = new();
    private readonly Dictionary<string, BoxStatus> _boxStates = new();
    private readonly List<RecentEvent> _recentEvents = new();
    private readonly object _lock = new();

    public void ProcessEvent(BoxEvent evt)
    {
        lock (_lock)
        {
            // Update box state
            var currentState = _boxStates.GetValueOrDefault(evt.BoxId);
            var newState = evt.EventType switch
            {
                nameof(BoxCleaningStarted) => "CLEANING",
                nameof(BoxCleaningCompleted) => "CLEANED",
                nameof(BoxRepairStarted) => "REPAIRING",
                nameof(BoxRepairCompleted) => "REPAIRED",
                nameof(BoxLoadingAttempted) => "LOADED",
                _ => currentState?.CurrentState ?? "UNKNOWN"
            };
            
            _boxStates[evt.BoxId] = new BoxStatus
            {
                BoxId = evt.BoxId,
                CurrentState = newState,
                LastWorker = evt.WorkerId,
                LastUpdated = evt.Timestamp
            };

            // Update metrics
            var updatedMetrics = _metrics with
            {
                BoxesCleaned = evt is BoxCleaningCompleted 
                    ? _metrics.BoxesCleaned + 1 
                    : _metrics.BoxesCleaned,
                BoxesRepaired = evt is BoxRepairCompleted 
                    ? _metrics.BoxesRepaired + 1 
                    : _metrics.BoxesRepaired,
                BoxesLoaded = evt is BoxLoadingAttempted { Success: true } 
                    ? _metrics.BoxesLoaded + 1 
                    : _metrics.BoxesLoaded,
                LoadingAttemptsFailed = evt is BoxLoadingAttempted { Success: false } 
                    ? _metrics.LoadingAttemptsFailed + 1 
                    : _metrics.LoadingAttemptsFailed,
                BoxesInProgress = _boxStates.Count(x => x.Value.CurrentState is "CLEANING" or "REPAIRING"),
                LastUpdated = DateTime.UtcNow
            };
            
            _metrics = updatedMetrics;

            // Add to recent events
            _recentEvents.Insert(0, new RecentEvent
            {
                EventId = evt.EventId,
                BoxId = evt.BoxId,
                EventType = evt.EventType,
                WorkerId = evt.WorkerId,
                Timestamp = evt.Timestamp
            });
            
            if (_recentEvents.Count > 50)
                _recentEvents.RemoveAt(_recentEvents.Count - 1);
        }
    }

    public DailyMetrics GetMetrics() => _metrics;
    public List<RecentEvent> GetRecentEvents() => _recentEvents.Take(10).ToList();
}

// Event Processor Background Service
public class EventProcessorService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly MetricsService _metrics;
    private readonly ILogger<EventProcessorService> _logger;
    private readonly IConfiguration _configuration;
    private IModel? _channel;
    private HubConnection? _hubConnection;

    public EventProcessorService(
        IConnection connection,
        MetricsService metrics,
        ILogger<EventProcessorService> logger,
        IConfiguration configuration)
    {
        _connection = connection;
        _metrics = metrics;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event Processor started");

        // Connect to SignalR Dashboard Hub
        var hubUrl = _configuration["SignalR:HubUrl"] ?? "http://dashboard:8080/hubs/dashboard";
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        try
        {
            await _hubConnection.StartAsync(stoppingToken);
            _logger.LogInformation("Connected to Dashboard Hub at {HubUrl}", hubUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Dashboard Hub. Events will be processed but not displayed.");
        }

        _channel = _connection.CreateModel();
        _channel.QueueDeclare(
            queue: "box-events",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                
                // Deserialize to base BoxEvent to determine type
                var baseEvent = JsonSerializer.Deserialize<BoxEvent>(json);
                if (baseEvent == null)
                {
                    _logger.LogWarning("Failed to deserialize event");
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                // Deserialize to specific type
                BoxEvent? evt = baseEvent.EventType switch
                {
                    nameof(BoxCleaningStarted) => JsonSerializer.Deserialize<BoxCleaningStarted>(json),
                    nameof(BoxCleaningCompleted) => JsonSerializer.Deserialize<BoxCleaningCompleted>(json),
                    nameof(BoxRepairStarted) => JsonSerializer.Deserialize<BoxRepairStarted>(json),
                    nameof(BoxRepairCompleted) => JsonSerializer.Deserialize<BoxRepairCompleted>(json),
                    nameof(BoxLoadingAttempted) => JsonSerializer.Deserialize<BoxLoadingAttempted>(json),
                    _ => baseEvent
                };

                if (evt != null)
                {
                    _metrics.ProcessEvent(evt);
                    _logger.LogInformation("Processed event: {EventType} for box {BoxId}", evt.EventType, evt.BoxId);
                    
                    // Broadcast to SignalR dashboard
                    if (_hubConnection?.State == HubConnectionState.Connected)
                    {
                        try
                        {
                            await _hubConnection.SendAsync("BroadcastEvent", evt, _metrics.GetMetrics());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to broadcast event to dashboard");
                        }
                    }
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(
            queue: "box-events",
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Listening for events on queue 'box-events'");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async ValueTask DisposeAsync()
    {
        _channel?.Close();
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
        await base.DisposeAsync();
    }

    public override void Dispose()
    {
        _channel?.Close();
        _hubConnection?.DisposeAsync().AsTask().Wait();
        base.Dispose();
    }
}
