using System.Text;
using System.Text.Json;
using BoxTracking.Shared.Events;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// RabbitMQ connection
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

builder.Services.AddSingleton<IModel>(sp =>
{
    var connection = sp.GetRequiredService<IConnection>();
    var channel = connection.CreateModel();
    
    // Declare queue
    channel.QueueDeclare(
        queue: "box-events",
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);
    
    return channel;
});

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Event endpoints
app.MapPost("/api/events", (BoxEvent evt, IModel channel) =>
{
    try
    {
        // Validate
        if (string.IsNullOrEmpty(evt.BoxId) || string.IsNullOrEmpty(evt.WorkerId))
        {
            return Results.BadRequest(new { error = "BoxId and WorkerId are required" });
        }
        
        // Publish to RabbitMQ
        var json = JsonSerializer.Serialize(evt, evt.GetType());
        var body = Encoding.UTF8.GetBytes(json);
        
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.Type = evt.EventType;
        
        channel.BasicPublish(
            exchange: "",
            routingKey: "box-events",
            basicProperties: properties,
            body: body);
        
        Console.WriteLine($"Published event: {evt.EventType} for box {evt.BoxId} by worker {evt.WorkerId}");
        
        return Results.Ok(new { eventId = evt.EventId, status = "published" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error publishing event: {ex.Message}");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

// Get recent events (for testing)
app.MapGet("/api/events", () =>
{
    return Results.Ok(new { message = "Event history not implemented in prototype" });
});

Console.WriteLine("Box Tracking API started");
Console.WriteLine($"RabbitMQ Host: {builder.Configuration["RabbitMQ:Host"]}");

app.Run();
