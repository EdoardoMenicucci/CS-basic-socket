using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5000);
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", builder =>
    {
        builder.WithOrigins("http://localhost:4200")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});

builder.Services.AddHttpClient(); // Add HttpClient service
builder.Services.AddControllers(); // Add controllers

// Register GeminiApiClient as a service
builder.Services.AddSingleton(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var apiKey = "API-KEY"; // Api Key
    return new GeminiApiClient(httpClient, apiKey);
});


var app = builder.Build();
var logger = app.Logger;

app.UseCors("AllowAngular");
app.UseWebSockets();
app.MapControllers();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid().ToString();
        logger.LogInformation($"New WebSocket connection established. ID: {connectionId}");

        var geminiApiClient = app.Services.GetRequiredService<GeminiApiClient>();
        var handler = new WebSocketHandler(webSocket, logger, geminiApiClient);
        await handler.HandleConnectionAsync();
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});


app.MapGet("/", () => "WebSocket Server is running!");

logger.LogInformation("Application configured, starting to listen...");
await app.RunAsync();