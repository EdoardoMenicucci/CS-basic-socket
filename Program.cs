using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

//configurazione del web server engine Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5000);
});

//configurazione log di output 
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

//impostazion di sicurezza per richieste da altri domini
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
    var apiKey = "api-key"; // Api Key
    return new GeminiApiClient(httpClient, apiKey);
});

//auth

var key = Encoding.ASCII.GetBytes("your_very_long_secret_key_here_32_bytes_or_more");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "your_issuer",
        ValidAudience = "your_audience",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();



var app = builder.Build();
var logger = app.Logger;

app.UseCors("AllowAngular");
app.UseWebSockets();
app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var token = context.Request.Query["token"].ToString();
        logger.LogInformation($"Recived token: {token}");
        if (string.IsNullOrEmpty(token) || !ValidateToken(token, key, out var claimsPrincipal))
        {
            context.Response.StatusCode = 401;
            logger.LogInformation($"Invalid Token");
            return;
        }

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

//risponde alla richiesta get porta 5000 path / con il messaggio:
app.MapGet("/", () => "WebSocket Server is running!");

logger.LogInformation("Application configured, starting to listen...");
await app.RunAsync();


//settaggi per la validazione del token JWT
bool ValidateToken(string token, byte[] key, out ClaimsPrincipal claimsPrincipal)
{
    claimsPrincipal = null;
    var tokenHandler = new JwtSecurityTokenHandler();
    try
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "your_issuer",
            ValidAudience = "your_audience",
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };

        claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
        return true;
    }
    catch (Exception ex)
    {
        logger.LogError($"Token validation failed: {ex.Message}");
        return false;
    }
}