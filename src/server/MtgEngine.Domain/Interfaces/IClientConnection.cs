using MtgEngine.Shared.Protocol;

namespace MtgEngine.Domain.Interfaces;

public interface IClientConnection
{
    string ConnectionId { get; }
    string? PlayerId { get; set; }
    Task SendAsync(ServerMessage message);
}
