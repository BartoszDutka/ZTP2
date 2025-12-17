using NotificationSystem.Shared.Services;
using NotificationSystem.Worker.Email;

var builder = WebApplication.CreateBuilder(args);

// API URL configuration
var apiUrl = builder.Configuration["ApiUrl"] ?? "http://api:8080";

// Register HTTP client for API communication
builder.Services.AddHttpClient<INotificationApiClient, NotificationApiClient>(client =>
{
    client.BaseAddress = new Uri(apiUrl);
});

// Register services
builder.Services.AddSingleton<INotificationSender, NotificationSenderSimulator>();
builder.Services.AddSingleton<IMetricsService, MetricsService>();

builder.Services.AddHostedService<Worker>();

var app = builder.Build();

// Endpoint /metrics w formacie Prometheus
app.MapGet("/metrics", (IMetricsService metrics) =>
{
    return Results.Text(metrics.GetPrometheusMetrics("email-worker"), "text/plain");
});

app.Run();
