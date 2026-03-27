using MtgEngine.Shared.Protocol;

namespace MtgEngine.Domain.Interfaces;

public interface IConnectionManager
{
    void Add(IClientConnection connection);
    void Remove(string connectionId);
    IClientConnection? GetByPlayerId(string playerId);
    IEnumerable<IClientConnection> GetAll();
    Task BroadcastToGameAsync(string gameId, ServerMessage message);
    Task SendToPlayerAsync(string playerId, ServerMessage message);
}
