using System.Net.WebSockets;
using MtgEngine.Application.CardEngine;
using MtgEngine.Application.Effects;
using MtgEngine.Application.GameEngine;
using MtgEngine.Application.Services;
using MtgEngine.Domain.Interfaces;
using MtgEngine.Infrastructure.Mods;
using MtgEngine.Infrastructure.Networking;
using MtgEngine.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Register services — Dependency Injection (SOLID: Dependency Inversion)
builder.Services.AddSingleton<ICardRepository, InMemoryCardRepository>();
builder.Services.AddSingleton<IEventBus, EventBus>();
builder.Services.AddSingleton<ConnectionManager>();
builder.Services.AddSingleton<IConnectionManager>(sp => sp.GetRequiredService<ConnectionManager>());

// Effect handlers (SOLID: Open/Closed — add new handlers without modifying resolver)
builder.Services.AddSingleton<IEffectHandler, DealDamageHandler>();
builder.Services.AddSingleton<IEffectHandler, GainLifeHandler>();
builder.Services.AddSingleton<IEffectHandler, DrawCardHandler>();
builder.Services.AddSingleton<IEffectHandler, DestroyHandler>();
builder.Services.AddSingleton<IEffectHandler, TapHandler>();
builder.Services.AddSingleton<IEffectHandler, UntapHandler>();
builder.Services.AddSingleton<IEffectHandler, AddManaHandler>();
builder.Services.AddSingleton<IEffectHandler, CreateTokenHandler>();
builder.Services.AddSingleton<IEffectHandler, ReturnToHandHandler>();
builder.Services.AddSingleton<IEffectHandler, ExileHandler>();

// Card and Game Engine
builder.Services.AddSingleton<EffectResolver>();
builder.Services.AddSingleton<CardLoader>();
builder.Services.AddSingleton<TargetValidator>();
builder.Services.AddSingleton<TurnManager>();
builder.Services.AddSingleton<StackManager>();
builder.Services.AddSingleton<CombatManager>();
builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<ModLoader>();
builder.Services.AddSingleton<MessageRouter>();

var app = builder.Build();

// Load core cards on startup
var cardLoader = app.Services.GetRequiredService<CardLoader>();
var cardRepo = app.Services.GetRequiredService<ICardRepository>();
var cardsPath = Path.Combine(app.Environment.ContentRootPath, "..", "..", "cards");
if (Directory.Exists(cardsPath))
{
    var cards = cardLoader.LoadFromDirectory(cardsPath);
    cardRepo.RegisterRange(cards);
    Console.WriteLine($"Loaded {cards.Count} core cards from {cardsPath}");
}

// Load mods
var modLoader = app.Services.GetRequiredService<ModLoader>();
var modsPath = Path.Combine(app.Environment.ContentRootPath, "..", "..", "mods");
modLoader.LoadModsFromDirectory(modsPath);

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Card catalog endpoint
app.MapGet("/api/cards", (ICardRepository repo) =>
    Results.Ok(repo.GetAll()));

// WebSocket endpoint
app.Map("/ws", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("WebSocket connections only");
        return;
    }

    var ws = await context.WebSockets.AcceptWebSocketAsync();
    var connection = new WebSocketClientConnection(ws);
    var connManager = context.RequestServices.GetRequiredService<ConnectionManager>();
    var router = context.RequestServices.GetRequiredService<MessageRouter>();

    connManager.Add(connection);
    Console.WriteLine($"Client connected: {connection.ConnectionId}");

    try
    {
        while (ws.State == WebSocketState.Open)
        {
            var message = await connection.ReceiveAsync(context.RequestAborted);
            if (message == null) break;

            await router.HandleMessageAsync(connection, message);
        }
    }
    catch (WebSocketException)
    {
        // Client disconnected
    }
    finally
    {
        connManager.Remove(connection.ConnectionId);
        connection.Dispose();
        Console.WriteLine($"Client disconnected: {connection.ConnectionId}");
    }
});

Console.WriteLine("MTG Engine Server starting on http://localhost:5000");
app.Run("http://0.0.0.0:5000");
