using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BoxTracking.Shared.Events;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Testcontainers.RabbitMq;
using Xunit;

namespace BoxTracking.IntegrationTests;

public class ApiIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private IConnection? _rabbitConnection;
    private IModel? _rabbitChannel;
    
    public ApiIntegrationTests()
    {
        // Disable resource reaper to avoid timeout issues in CI/CD environments
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
        
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithPortBinding(5672, true)
            .WithPortBinding(15672, true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start RabbitMQ container
        await _rabbitMqContainer.StartAsync();
        
        // Wait a bit for RabbitMQ to be ready
        await Task.Delay(2000);
        
        // Setup RabbitMQ connection for verification
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqContainer.Hostname,
            Port = _rabbitMqContainer.GetMappedPublicPort(5672),
            UserName = "guest",
            Password = "guest"
        };
        
        _rabbitConnection = factory.CreateConnection();
        _rabbitChannel = _rabbitConnection.CreateModel();
        
        // Declare the queue (same as API does)
        _rabbitChannel.QueueDeclare(
            queue: "box-events",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        
        // Setup API with test RabbitMQ connection
        var rabbitMqConnectionString = $"amqp://guest:guest@{_rabbitMqContainer.Hostname}:{_rabbitMqContainer.GetMappedPublicPort(5672)}";
        
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("RabbitMQ:ConnectionString", rabbitMqConnectionString);
            });
        
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _rabbitChannel?.Close();
        _rabbitChannel?.Dispose();
        _rabbitConnection?.Close();
        _rabbitConnection?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
        await _rabbitMqContainer.DisposeAsync();
    }

    [Fact]
    public async Task HealthCheck_Should_ReturnHealthy()
    {
        // Act
        var response = await _client!.GetAsync("/health");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task PostEvent_BoxCleaningCompleted_Should_PublishToRabbitMQ()
    {
        // Arrange
        var boxEvent = new BoxEvent
        {
            EventId = Guid.NewGuid().ToString(),
            BoxId = "BOX-001",
            WorkerId = "WORKER-01",
            EventType = "BoxCleaningCompleted",
            Timestamp = DateTime.UtcNow
        };

        var messageReceived = false;
        BoxEvent? receivedEvent = null;
        
        // Setup consumer to verify message
        var consumer = new EventingBasicConsumer(_rabbitChannel!);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            receivedEvent = JsonSerializer.Deserialize<BoxEvent>(message);
            messageReceived = true;
        };
        
        _rabbitChannel!.BasicConsume(
            queue: "box-events",
            autoAck: true,
            consumer: consumer);

        // Act
        var response = await _client!.PostAsJsonAsync("/api/events", boxEvent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Wait for message to be consumed
        await Task.Delay(1000);
        
        messageReceived.Should().BeTrue("message should be published to RabbitMQ");
        receivedEvent.Should().NotBeNull();
        receivedEvent!.BoxId.Should().Be("BOX-001");
        receivedEvent.WorkerId.Should().Be("WORKER-01");
        receivedEvent.EventType.Should().Be("BoxCleaningCompleted");
    }

    [Fact]
    public async Task PostEvent_InvalidEvent_Should_ReturnBadRequest()
    {
        // Arrange
        var invalidEvent = new { }; // Missing required fields

        // Act
        var response = await _client!.PostAsJsonAsync("/api/events", invalidEvent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("BoxCleaningStarted")]
    [InlineData("BoxCleaningCompleted")]
    [InlineData("BoxRepairStarted")]
    [InlineData("BoxRepairCompleted")]
    [InlineData("BoxLoadingAttempted")]
    public async Task PostEvent_AllEventTypes_Should_BeAccepted(string eventType)
    {
        // Arrange
        var boxEvent = new BoxEvent
        {
            EventId = Guid.NewGuid().ToString(),
            BoxId = $"BOX-{eventType}",
            WorkerId = "WORKER-TEST",
            EventType = eventType,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var response = await _client!.PostAsJsonAsync("/api/events", boxEvent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostMultipleEvents_Should_AllBePublished()
    {
        // Arrange
        var events = new[]
        {
            new BoxEvent
            {
                EventId = Guid.NewGuid().ToString(),
                BoxId = "BOX-001",
                WorkerId = "WORKER-01",
                EventType = "BoxCleaningStarted",
                Timestamp = DateTime.UtcNow
            },
            new BoxEvent
            {
                EventId = Guid.NewGuid().ToString(),
                BoxId = "BOX-001",
                WorkerId = "WORKER-01",
                EventType = "BoxCleaningCompleted",
                Timestamp = DateTime.UtcNow.AddMinutes(5)
            },
            new BoxEvent
            {
                EventId = Guid.NewGuid().ToString(),
                BoxId = "BOX-001",
                WorkerId = "WORKER-02",
                EventType = "BoxLoadingAttempted",
                Timestamp = DateTime.UtcNow.AddMinutes(10)
            }
        };

        var receivedCount = 0;
        var consumer = new EventingBasicConsumer(_rabbitChannel!);
        consumer.Received += (model, ea) => { receivedCount++; };
        
        _rabbitChannel!.BasicConsume(
            queue: "box-events",
            autoAck: true,
            consumer: consumer);

        // Act
        foreach (var evt in events)
        {
            var response = await _client!.PostAsJsonAsync("/api/events", evt);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Assert
        await Task.Delay(1000);
        receivedCount.Should().Be(3, "all three events should be published");
    }

    [Fact]
    public void RabbitMQ_QueueProperties_Should_BeCorrect()
    {
        // Act
        var queueInfo = _rabbitChannel!.QueueDeclarePassive("box-events");

        // Assert
        queueInfo.QueueName.Should().Be("box-events");
        // Queue should be durable (survives broker restart)
    }
}
