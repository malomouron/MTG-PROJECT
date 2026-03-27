using MtgEngine.Shared.Enums;

namespace MtgEngine.Shared.Protocol;

public abstract class ClientMessage
{
    public required string Type { get; init; }
}

public sealed class ConnectMessage : ClientMessage
{
    public required string PlayerName { get; init; }
}

public sealed class CreateGameMessage : ClientMessage
{
    public required string GameName { get; init; }
    public int MaxPlayers { get; init; } = 4;
}

public sealed class JoinGameMessage : ClientMessage
{
    public required string GameId { get; init; }
    public required List<string> DeckCardIds { get; init; }
    public required string CommanderId { get; init; }
}

public sealed class StartGameMessage : ClientMessage
{
    public required string GameId { get; init; }
}

public sealed class PlayCardMessage : ClientMessage
{
    public required string GameId { get; init; }
    public required string CardInstanceId { get; init; }
    public string? TargetId { get; init; }
}

public sealed class DeclareAttackersMessage : ClientMessage
{
    public required string GameId { get; init; }
    public required List<AttackerDeclaration> Attackers { get; init; }
}

public sealed class AttackerDeclaration
{
    public required string CreatureId { get; init; }
    public required string DefendingPlayerId { get; init; }
}

public sealed class DeclareBlockersMessage : ClientMessage
{
    public required string GameId { get; init; }
    public required List<BlockerDeclaration> Blockers { get; init; }
}

public sealed class BlockerDeclaration
{
    public required string BlockerId { get; init; }
    public required string AttackerId { get; init; }
}

public sealed class PassPriorityMessage : ClientMessage
{
    public required string GameId { get; init; }
}

public sealed class ActivateAbilityMessage : ClientMessage
{
    public required string GameId { get; init; }
    public required string PermanentId { get; init; }
    public int AbilityIndex { get; init; }
    public string? TargetId { get; init; }
}
