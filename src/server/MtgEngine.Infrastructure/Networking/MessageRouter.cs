using System.Text.Json;
using System.Text.Json.Serialization;
using MtgEngine.Application.Services;
using MtgEngine.Domain.Entities;
using MtgEngine.Domain.Interfaces;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Protocol;

namespace MtgEngine.Infrastructure.Networking;

public sealed class MessageRouter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private static readonly JsonSerializerOptions SendOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly GameService _gameService;
    private readonly ConnectionManager _connectionManager;

    public MessageRouter(GameService gameService, ConnectionManager connectionManager)
    {
        _gameService = gameService;
        _connectionManager = connectionManager;
    }

    public async Task HandleMessageAsync(WebSocketClientConnection connection, string rawJson)
    {
        using var doc = JsonDocument.Parse(rawJson);
        var type = doc.RootElement.GetProperty("type").GetString();

        try
        {
            switch (type)
            {
                case "connect":
                    await HandleConnect(connection, rawJson);
                    break;
                case "create_game":
                    await HandleCreateGame(connection, rawJson);
                    break;
                case "join_game":
                    await HandleJoinGame(connection, rawJson);
                    break;
                case "start_game":
                    await HandleStartGame(connection, rawJson);
                    break;
                case "play_card":
                    await HandlePlayCard(connection, rawJson);
                    break;
                case "declare_attackers":
                    await HandleDeclareAttackers(connection, rawJson);
                    break;
                case "declare_blockers":
                    await HandleDeclareBlockers(connection, rawJson);
                    break;
                case "pass_priority":
                    await HandlePassPriority(connection, rawJson);
                    break;
                case "activate_ability":
                    await HandleActivateAbility(connection, rawJson);
                    break;
                default:
                    await SendError(connection, $"Unknown message type: {type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            await SendError(connection, $"Error processing message: {ex.Message}");
        }
    }

    private async Task HandleConnect(WebSocketClientConnection connection, string json)
    {
        var msg = Deserialize<ConnectMessage>(json);
        connection.PlayerId = msg.PlayerName;
        _connectionManager.Add(connection);

        // Send lobby state
        var games = _gameService.GetAllGames();
        var lobbyMsg = new LobbyUpdateMessage
        {
            Type = "lobby_update",
            Games = games.Select(g => new GameInfoDto
            {
                GameId = g.GameId,
                GameName = g.GameName,
                PlayerCount = g.Players.Count,
                MaxPlayers = g.MaxPlayers,
                Status = g.Status
            }).ToList()
        };
        await connection.SendAsync(lobbyMsg);
    }

    private async Task HandleCreateGame(WebSocketClientConnection connection, string json)
    {
        var msg = Deserialize<CreateGameMessage>(json);
        var game = _gameService.CreateGame(msg.GameName, msg.MaxPlayers);

        await BroadcastLobbyUpdate();
        await connection.SendAsync(new GameEventMessage
        {
            Type = "game_created",
            EventType = "game_created",
            Data = new Dictionary<string, object>
            {
                { "gameId", game.GameId },
                { "gameName", game.GameName }
            }
        });
    }

    private async Task HandleJoinGame(WebSocketClientConnection connection, string json)
    {
        var msg = Deserialize<JoinGameMessage>(json);
        var result = _gameService.JoinGame(
            msg.GameId, connection.PlayerId!, connection.PlayerId!, msg.DeckCardIds, msg.CommanderId);

        if (!result.Success)
        {
            await SendError(connection, result.Error!);
            return;
        }

        _connectionManager.MapPlayerToGame(msg.GameId, connection.PlayerId!);
        await BroadcastGameState(msg.GameId);
        await BroadcastLobbyUpdate();
    }

    private async Task HandleStartGame(WebSocketClientConnection connection, string json)
    {
        var msg = Deserialize<StartGameMessage>(json);
        var result = _gameService.StartGame(msg.GameId, connection.PlayerId!);

        if (!result.Success)
        {
            await SendError(connection, result.Error!);
            return;
        }

        await BroadcastGameState(msg.GameId);
        await SendPrivateHandsForGame(msg.GameId);
    }

    private async Task HandlePlayCard(WebSocketClientConnection connection, string json)
    {
        var msg = Deserialize<PlayCardMessage>(json);
        var result = _gameService.PlayCard(msg.GameId, connection.PlayerId!, msg.CardInstanceId, msg.TargetId);

        if (!result.Success)
        {
            await SendError(connection, result.Error!);
            return;
        }

        await BroadcastGameState(msg.GameId);
        await SendPrivateHand(msg.GameId, connection.PlayerId!);
    }

    private async Task HandleDeclareAttackers(WebSocketClientConnection connection, string json)
    {
        var msg = Deserialize<DeclareAttackersMessage>(json);
        var attackers = msg.Attackers.Select(a => new CombatAssignment
        {
            AttackerId = a.CreatureId,
            AttackerControllerId = connection.PlayerId!,
            DefendingPlayerId = a.DefendingPlayerId
        }).ToList();

        var result = _gameService.DeclareAttackers(msg.GameId, connection.PlayerId!, attackers);
        if (!result.Success)
        {
            await SendError(connection, result.Error!);
            return;
        }

        await BroadcastGameState(msg.GameId);
    }

    private async Task HandleDeclareBlockers(WebSocketClientConnection connection, string json)
    {
        var msg = Deserialize<DeclareBlockersMessage>(json);
        var blockers = msg.Blockers.Select(b => new CombatBlock
        {
            BlockerId = b.BlockerId,
            AttackerId = b.AttackerId
        }).ToList();

        var result = _gameService.DeclareBlockers(msg.GameId, connection.PlayerId!, blockers);
        if (!result.Success)
        {
            await SendError(connection, result.Error!);
            return;
        }

        await BroadcastGameState(msg.GameId);
    }

    private async Task HandlePassPriority(WebSocketClientConnection connection, string json)
    {
        var msg = Deserialize<PassPriorityMessage>(json);
        var result = _gameService.PassPriority(msg.GameId, connection.PlayerId!);

        if (!result.Success)
        {
            await SendError(connection, result.Error!);
            return;
        }

        await BroadcastGameState(msg.GameId);
    }

    private async Task HandleActivateAbility(WebSocketClientConnection connection, string json)
    {
        var msg = Deserialize<ActivateAbilityMessage>(json);
        var result = _gameService.ActivateAbility(
            msg.GameId, connection.PlayerId!, msg.PermanentId, msg.AbilityIndex, msg.TargetId);

        if (!result.Success)
        {
            await SendError(connection, result.Error!);
            return;
        }

        await BroadcastGameState(msg.GameId);
    }

    private async Task BroadcastGameState(string gameId)
    {
        var game = _gameService.GetGame(gameId);
        if (game == null) return;

        var stateMsg = new GameStateMessage
        {
            Type = "game_state",
            State = MapGameState(game)
        };

        await _connectionManager.BroadcastToGameAsync(gameId, stateMsg);
    }

    private async Task BroadcastLobbyUpdate()
    {
        var games = _gameService.GetAllGames();
        var lobbyMsg = new LobbyUpdateMessage
        {
            Type = "lobby_update",
            Games = games.Select(g => new GameInfoDto
            {
                GameId = g.GameId,
                GameName = g.GameName,
                PlayerCount = g.Players.Count,
                MaxPlayers = g.MaxPlayers,
                Status = g.Status
            }).ToList()
        };

        foreach (var conn in _connectionManager.GetAll())
            await conn.SendAsync(lobbyMsg);
    }

    private async Task SendPrivateHandsForGame(string gameId)
    {
        var game = _gameService.GetGame(gameId);
        if (game == null) return;

        foreach (var player in game.Players)
            await SendPrivateHand(gameId, player.PlayerId);
    }

    private async Task SendPrivateHand(string gameId, string playerId)
    {
        var game = _gameService.GetGame(gameId);
        var player = game?.GetPlayer(playerId);
        if (player == null) return;

        var handMsg = new PrivatePlayerStateMessage
        {
            Type = "private_state",
            Hand = player.Hand.Select(c => new HandCardDto
            {
                InstanceId = c.InstanceId,
                DefinitionId = c.Definition.Id,
                Name = c.Definition.Name,
                CardType = c.Definition.Type,
                Cost = c.Definition.Cost,
                Power = c.Definition.Power,
                Toughness = c.Definition.Toughness
            }).ToList()
        };

        await _connectionManager.SendToPlayerAsync(playerId, handMsg);
    }

    private static GameStateDto MapGameState(GameState game)
    {
        return new GameStateDto
        {
            GameId = game.GameId,
            Status = game.Status,
            CurrentPhase = game.CurrentPhase,
            ActivePlayerId = game.ActivePlayer?.PlayerId ?? string.Empty,
            TurnNumber = game.TurnNumber,
            Players = game.Players.Select(p => new PlayerStateDto
            {
                PlayerId = p.PlayerId,
                PlayerName = p.PlayerName,
                Life = p.Life,
                HandCount = p.Hand.Count,
                LibraryCount = p.Library.Count,
                Battlefield = p.Battlefield.Select(perm => new PermanentDto
                {
                    InstanceId = perm.InstanceId,
                    DefinitionId = perm.Definition.Id,
                    Name = perm.Definition.Name,
                    CardType = perm.Definition.Type,
                    IsTapped = perm.IsTapped,
                    Power = perm.Definition.Power,
                    Toughness = perm.Definition.Toughness,
                    DamageMarked = perm.DamageMarked,
                    HasSummoningSickness = perm.HasSummoningSickness
                }).ToList(),
                Graveyard = p.Graveyard.Select(c => new CardDto
                {
                    InstanceId = c.InstanceId,
                    DefinitionId = c.Definition.Id,
                    Name = c.Definition.Name,
                    CardType = c.Definition.Type
                }).ToList(),
                Exile = p.Exile.Select(c => new CardDto
                {
                    InstanceId = c.InstanceId,
                    DefinitionId = c.Definition.Id,
                    Name = c.Definition.Name,
                    CardType = c.Definition.Type
                }).ToList(),
                CommandZone = p.CommandZone.Select(c => new CardDto
                {
                    InstanceId = c.InstanceId,
                    DefinitionId = c.Definition.Id,
                    Name = c.Definition.Name,
                    CardType = c.Definition.Type
                }).ToList(),
                ManaPool = new ManaPoolDto
                {
                    White = p.ManaPool.White,
                    Blue = p.ManaPool.Blue,
                    Black = p.ManaPool.Black,
                    Red = p.ManaPool.Red,
                    Green = p.ManaPool.Green,
                    Colorless = p.ManaPool.Colorless
                },
                IsEliminated = p.IsEliminated
            }).ToList(),
            Stack = game.Stack.Select(s => new StackItemDto
            {
                InstanceId = s.InstanceId,
                Name = s.Definition.Name,
                ControllerId = s.ControllerId,
                TargetId = s.TargetId
            }).ToList()
        };
    }

    private static async Task SendError(WebSocketClientConnection connection, string message)
    {
        await connection.SendAsync(new ErrorMessage
        {
            Type = "error",
            Message = message
        });
    }

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");
}
