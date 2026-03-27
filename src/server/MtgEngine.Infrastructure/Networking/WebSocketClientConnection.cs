using MtgEngine.Domain.Interfaces;
using MtgEngine.Shared.Protocol;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MtgEngine.Infrastructure.Networking;

public sealed class WebSocketClientConnection : IClientConnection, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly WebSocket _webSocket;

    public string ConnectionId { get; }
    public string? PlayerId { get; set; }

    public WebSocketClientConnection(WebSocket webSocket)
    {
        _webSocket = webSocket;
        ConnectionId = Guid.NewGuid().ToString();
    }

    public async Task SendAsync(ServerMessage message)
    {
        if (_webSocket.State != WebSocketState.Open)
            return;

        string json = JsonSerializer.Serialize(message, message.GetType(), JsonOptions);
        byte[] buffer = Encoding.UTF8.GetBytes(json);
        await _webSocket.SendAsync(
            new ArraySegment<byte>(buffer),
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None);
    }

    public async Task<string?> ReceiveAsync(CancellationToken ct)
    {
        byte[] buffer = new byte[8192];
        using MemoryStream ms = new MemoryStream();

        WebSocketReceiveResult result;
        do
        {
            result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close)
                return null;

            ms.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public async Task CloseAsync()
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Closing",
                CancellationToken.None);
        }
    }

    public void Dispose()
    {
        _webSocket.Dispose();
    }
}
