using MtgEngine.Domain.Interfaces;

namespace MtgEngine.Application.GameEngine.Events;

public sealed class GameStartedEvent : IGameEvent
{
    public string EventType => "GameStarted";
    public required string GameId { get; init; }
    public required List<string> PlayerIds { get; init; }
}

public sealed class TurnStartedEvent : IGameEvent
{
    public string EventType => "TurnStarted";
    public required string GameId { get; init; }
    public required string PlayerId { get; init; }
    public int TurnNumber { get; init; }
}

public sealed class PhaseChangedEvent : IGameEvent
{
    public string EventType => "PhaseChanged";
    public required string GameId { get; init; }
    public required Shared.Enums.Phase Phase { get; init; }
}

public sealed class CardDrawnEvent : IGameEvent
{
    public string EventType => "CardDrawn";
    public required string GameId { get; init; }
    public required string PlayerId { get; init; }
    public required string CardInstanceId { get; init; }
}

public sealed class CardPlayedEvent : IGameEvent
{
    public string EventType => "CardPlayed";
    public required string GameId { get; init; }
    public required string PlayerId { get; init; }
    public required string CardInstanceId { get; init; }
    public string? TargetId { get; init; }
}

public sealed class SpellResolvedEvent : IGameEvent
{
    public string EventType => "SpellResolved";
    public required string GameId { get; init; }
    public required string CardInstanceId { get; init; }
}

public sealed class CreatureEnteredBattlefieldEvent : IGameEvent
{
    public string EventType => "CreatureEnteredBattlefield";
    public required string GameId { get; init; }
    public required string PermanentId { get; init; }
    public required string ControllerId { get; init; }
}

public sealed class CreatureDiedEvent : IGameEvent
{
    public string EventType => "CreatureDied";
    public required string GameId { get; init; }
    public required string PermanentId { get; init; }
    public required string ControllerId { get; init; }
}

public sealed class DamageDealtEvent : IGameEvent
{
    public string EventType => "DamageDealt";
    public required string GameId { get; init; }
    public required string SourceId { get; init; }
    public required string TargetId { get; init; }
    public int Amount { get; init; }
}

public sealed class LifeChangedEvent : IGameEvent
{
    public string EventType => "LifeChanged";
    public required string GameId { get; init; }
    public required string PlayerId { get; init; }
    public int OldLife { get; init; }
    public int NewLife { get; init; }
}

public sealed class PlayerEliminatedEvent : IGameEvent
{
    public string EventType => "PlayerEliminated";
    public required string GameId { get; init; }
    public required string PlayerId { get; init; }
    public required string Reason { get; init; }
}

public sealed class GameEndedEvent : IGameEvent
{
    public string EventType => "GameEnded";
    public required string GameId { get; init; }
    public required string WinnerId { get; init; }
}

public sealed class PermanentEnteredBattlefieldEvent : IGameEvent
{
    public string EventType => "PermanentEnteredBattlefield";
    public required string GameId { get; init; }
    public required string PermanentId { get; init; }
    public required string ControllerId { get; init; }
}
