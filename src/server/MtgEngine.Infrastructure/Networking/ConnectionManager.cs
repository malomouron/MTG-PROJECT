using System.Collections.Concurrent;
using MtgEngine.Domain.Interfaces;
using MtgEngine.Shared.Protocol;

namespace MtgEngine.Infrastructure.Networking;

public sealed class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, IClientConnection> _connections = new();
    private readonly Dictionary<string, HashSet<string>> _gamePlayerMap = new();

    public void Add(IClientConnection connection)
    {
        _connections[connection.ConnectionId] = connection;
    }

    public void Remove(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    public IClientConnection? GetByPlayerId(string playerId)
    {
        return _connections.Values.FirstOrDefault(c => c.PlayerId == playerId);
    }

    public IEnumerable<IClientConnection> GetAll() => _connections.Values;

    public void MapPlayerToGame(string gameId, string playerId)
    {
        if (!_gamePlayerMap.ContainsKey(gameId))
            _gamePlayerMap[gameId] = [];
        _gamePlayerMap[gameId].Add(playerId);
    }

    public async Task BroadcastToGameAsync(string gameId, ServerMessage message)
    {
        if (!_gamePlayerMap.TryGetValue(gameId, out var playerIds))
            return;

        var tasks = playerIds
            .Select(pid => GetByPlayerId(pid))
            .Where(c => c != null)
            .Select(c => c!.SendAsync(message));

        await Task.WhenAll(tasks);
    }

    public async Task SendToPlayerAsync(string playerId, ServerMessage message)
    {
        var connection = GetByPlayerId(playerId);
        if (connection != null)
            await connection.SendAsync(message);
    }
}
