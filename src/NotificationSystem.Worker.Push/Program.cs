using NotificationSystem.Shared.Services;
using NotificationSystem.Worker.Push;

var builder = WebApplication.CreateBuilder(args);

// API URL configuration
var apiUrl = builder.Configuration["ApiUrl"] ?? "http://api:8080";

// Register HTTP client for API communication
builder.Services.AddHttpClient<INotificationApiClient, NotificationApiClient>(client =>
{
    client.BaseAddress = new Uri(apiUrl);
});

// Register sender simulator
builder.Services.AddSingleton<INotificationSender, NotificationSenderSimulator>();

// Register metrics service
builder.Services.AddSingleton<IMetricsService, MetricsService>();

builder.Services.AddHostedService<Worker>();

var app = builder.Build();

// Metrics endpoint for Prometheus
app.MapGet("/metrics", (IMetricsService metrics) => 
    Results.Text(metrics.GetPrometheusMetrics("push_worker"), "text/plain"));

app.Run();
