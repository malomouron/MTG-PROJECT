using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MtgEngine.Shared.Protocol;

namespace MtgClient.Services;

public sealed class ServerConnection : IDisposable
{
    private static readonly JsonSerializerOptions SendOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly JsonSerializerOptions ReceiveOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;

    public bool IsConnected => _ws?.State == WebSocketState.Open;

    // Events for received messages
    public event Action<GameStateMessage>? GameStateReceived;
    public event Action<LobbyUpdateMessage>? LobbyUpdateReceived;
    public event Action<ErrorMessage>? ErrorReceived;
    public event Action<GameEventMessage>? GameEventReceived;
    public event Action<PrivatePlayerStateMessage>? PrivateStateReceived;
    public event Action<ChatMessageServer>? ChatReceived;
    public event Action? Connected;
    public event Action<string>? Disconnected;

    public async Task ConnectAsync(string serverUrl, string playerName)
    {
        _ws = new ClientWebSocket();
        _cts = new CancellationTokenSource();

        await _ws.ConnectAsync(new Uri(serverUrl), _cts.Token);
        Connected?.Invoke();

        // Send connect message
        await SendAsync(new ConnectMessage
        {
            Type = "connect",
            PlayerName = playerName
        });

        // Start receiving loop
        _ = Task.Run(() => ReceiveLoop(_cts.Token));
    }

    public async Task DisconnectAsync()
    {
        if (_ws?.State == WebSocketState.Open)
        {
            _cts?.Cancel();
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
        }
    }

    public async Task SendAsync<T>(T message) where T : ClientMessage
    {
        if (_ws?.State != WebSocketState.Open)
            return;

        var json = JsonSerializer.Serialize(message, message.GetType(), SendOptions);
        var buffer = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task SendRawAsync(string json)
    {
        if (_ws?.State != WebSocketState.Open)
            return;

        var buffer = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        var buffer = new byte[16384];
        try
        {
            while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Disconnected?.Invoke("Server closed connection");
                        return;
                    }
                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                var json = Encoding.UTF8.GetString(ms.ToArray());
                DispatchMessage(json);
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException ex)
        {
            Disconnected?.Invoke(ex.Message);
        }
    }

    private void DispatchMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var type = doc.RootElement.GetProperty("type").GetString();

            switch (type)
            {
                case "game_state":
                    var gs = JsonSerializer.Deserialize<GameStateMessage>(json, ReceiveOptions);
                    if (gs != null) GameStateReceived?.Invoke(gs);
                    break;

                case "lobby_update":
                    var lu = JsonSerializer.Deserialize<LobbyUpdateMessage>(json, ReceiveOptions);
                    if (lu != null) LobbyUpdateReceived?.Invoke(lu);
                    break;

                case "error":
                    var err = JsonSerializer.Deserialize<ErrorMessage>(json, ReceiveOptions);
                    if (err != null) ErrorReceived?.Invoke(err);
                    break;

                case "game_created":
                case "game_event":
                    var ge = JsonSerializer.Deserialize<GameEventMessage>(json, ReceiveOptions);
                    if (ge != null) GameEventReceived?.Invoke(ge);
                    break;

                case "private_state":
                    var ps = JsonSerializer.Deserialize<PrivatePlayerStateMessage>(json, ReceiveOptions);
                    if (ps != null) PrivateStateReceived?.Invoke(ps);
                    break;

                case "chat":
                    var chat = JsonSerializer.Deserialize<ChatMessageServer>(json, ReceiveOptions);
                    if (chat != null) ChatReceived?.Invoke(chat);
                    break;
            }
        }
        catch (JsonException)
        {
            // Silently ignore malformed messages
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _ws?.Dispose();
    }
}

/// <summary>
/// Server-side chat message DTO. Extends ServerMessage for protocol compatibility.
/// </summary>
public sealed class ChatMessageServer : ServerMessage
{
    public required string Sender { get; init; }
    public required string Text { get; init; }
}
