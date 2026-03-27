using MtgEngine.Shared.Enums;

namespace MtgEngine.Shared.Protocol;

public abstract class ServerMessage
{
    public required string Type { get; init; }
}

public sealed class GameStateMessage : ServerMessage
{
    public required GameStateDto State { get; init; }
}

public sealed class ErrorMessage : ServerMessage
{
    public required string Message { get; init; }
}

public sealed class LobbyUpdateMessage : ServerMessage
{
    public required List<GameInfoDto> Games { get; init; }
}

public sealed class GameEventMessage : ServerMessage
{
    public required string EventType { get; init; }
    public required Dictionary<string, object> Data { get; init; }
}

public sealed class GameStateDto
{
    public required string GameId { get; init; }
    public required GameStatus Status { get; init; }
    public required Phase CurrentPhase { get; init; }
    public required string ActivePlayerId { get; init; }
    public int TurnNumber { get; init; }
    public required List<PlayerStateDto> Players { get; init; }
    public required List<StackItemDto> Stack { get; init; }
}

public sealed class PlayerStateDto
{
    public required string PlayerId { get; init; }
    public required string PlayerName { get; init; }
    public int Life { get; init; }
    public int HandCount { get; init; }
    public int LibraryCount { get; init; }
    public required List<PermanentDto> Battlefield { get; init; }
    public required List<CardDto> Graveyard { get; init; }
    public required List<CardDto> Exile { get; init; }
    public required List<CardDto> CommandZone { get; init; }
    public required ManaPoolDto ManaPool { get; init; }
    public bool IsEliminated { get; init; }
}

public sealed class CardDto
{
    public required string InstanceId { get; init; }
    public required string DefinitionId { get; init; }
    public required string Name { get; init; }
    public required CardType CardType { get; init; }
}

public sealed class PermanentDto
{
    public required string InstanceId { get; init; }
    public required string DefinitionId { get; init; }
    public required string Name { get; init; }
    public required CardType CardType { get; init; }
    public bool IsTapped { get; init; }
    public int? Power { get; init; }
    public int? Toughness { get; init; }
    public int DamageMarked { get; init; }
    public bool HasSummoningSickness { get; init; }
}

public sealed class StackItemDto
{
    public required string InstanceId { get; init; }
    public required string Name { get; init; }
    public required string ControllerId { get; init; }
    public string? TargetId { get; init; }
}

public sealed class ManaPoolDto
{
    public int White { get; init; }
    public int Blue { get; init; }
    public int Black { get; init; }
    public int Red { get; init; }
    public int Green { get; init; }
    public int Colorless { get; init; }
}

public sealed class GameInfoDto
{
    public required string GameId { get; init; }
    public required string GameName { get; init; }
    public int PlayerCount { get; init; }
    public int MaxPlayers { get; init; }
    public required GameStatus Status { get; init; }
}

public sealed class HandCardDto
{
    public required string InstanceId { get; init; }
    public required string DefinitionId { get; init; }
    public required string Name { get; init; }
    public required CardType CardType { get; init; }
    public string Cost { get; init; } = string.Empty;
    public int? Power { get; init; }
    public int? Toughness { get; init; }
}

public sealed class PrivatePlayerStateMessage : ServerMessage
{
    public required List<HandCardDto> Hand { get; init; }
}
