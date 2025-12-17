using MongoDB.Driver;
using NotificationSystem.Domain.Interfaces;
using NotificationSystem.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// MongoDB configuration
var connectionString = builder.Configuration.GetConnectionString("MongoDB") 
    ?? "mongodb+srv://user:RGB3manas@cluster0.ccrfrrt.mongodb.net/?appName=Cluster0";
var databaseName = builder.Configuration["MongoDB:DatabaseName"] ?? "ZtpLab1DevDb";

builder.Services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
builder.Services.AddScoped(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));

// Register repositories
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
