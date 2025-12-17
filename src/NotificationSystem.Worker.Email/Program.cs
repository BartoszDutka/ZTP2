using MongoDB.Driver;
using NotificationSystem.Domain.Interfaces;
using NotificationSystem.Infrastructure.Repositories;
using NotificationSystem.Shared.Services;
using NotificationSystem.Worker.Email;

var builder = Host.CreateApplicationBuilder(args);

// MongoDB configuration
var connectionString = builder.Configuration.GetConnectionString("MongoDB") 
    ?? "mongodb+srv://user:RGB3manas@cluster0.ccrfrrt.mongodb.net/?appName=Cluster0";
var databaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "ZtpLab1DevDb";

builder.Services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
builder.Services.AddScoped(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));

// Register repositories and services
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddSingleton<INotificationSender, NotificationSenderSimulator>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
