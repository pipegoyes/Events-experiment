using BoxTracking.EventSimulator.Components;
using BoxTracking.EventSimulator.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Sentry
builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"];
    options.Environment = builder.Environment.EnvironmentName;
    options.TracesSampleRate = 1.0;
    options.SendDefaultPii = false;
    options.Release = $"boxtracking-simulator@{builder.Configuration["Version"] ?? "1.0.0"}";
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<EventService>(client =>
{
    var apiUrl = builder.Configuration["BoxTrackingApi:BaseUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(apiUrl);
    Console.WriteLine($"Event Simulator configured to connect to API at: {apiUrl}");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
