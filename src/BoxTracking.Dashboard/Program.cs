using BoxTracking.Dashboard.Components;
using BoxTracking.Dashboard.Hubs;
using BoxTracking.Dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Sentry
builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"];
    options.Environment = builder.Environment.EnvironmentName;
    options.TracesSampleRate = 1.0;
    options.SendDefaultPii = false;
    options.Release = $"boxtracking-dashboard@{builder.Configuration["Version"] ?? "1.0.0"}";
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();

// Add event broadcast service (singleton to share state across all pages)
builder.Services.AddSingleton<EventBroadcastService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<DashboardHub>("/hubs/dashboard");

Console.WriteLine("Box Tracking Dashboard started");
Console.WriteLine("SignalR Hub available at: /hubs/dashboard");
app.Run();
