using MtgEngine.Shared.Enums;

namespace MtgEngine.Domain.Entities;

public sealed class GameState
{
    public string GameId { get; } = Guid.NewGuid().ToString();
    public string GameName { get; set; } = string.Empty;
    public GameStatus Status { get; set; } = GameStatus.WaitingForPlayers;
    public int MaxPlayers { get; set; } = 4;
    public int StartingLife { get; set; } = 40;
    public int TurnNumber { get; set; }
    public int ActivePlayerIndex { get; set; }
    public Phase CurrentPhase { get; set; } = Phase.Untap;
    public List<PlayerState> Players { get; } = [];
    public Stack<StackItem> Stack { get; } = new();
    public List<string> ModsEnabled { get; } = [];
    public string? PriorityPlayerId { get; set; }
    public HashSet<string> PlayersPassedPriority { get; } = [];

    // Combat state
    public List<CombatAssignment> CombatAttackers { get; } = [];
    public List<CombatBlock> CombatBlockers { get; } = [];

    public PlayerState? ActivePlayer =>
        ActivePlayerIndex >= 0 && ActivePlayerIndex < Players.Count
            ? Players[ActivePlayerIndex]
            : null;

    public PlayerState? GetPlayer(string playerId) =>
        Players.FirstOrDefault(p => p.PlayerId == playerId);

    public List<PlayerState> GetAlivePlayers() =>
        Players.Where(p => !p.IsEliminated).ToList();

    public void NextActivePlayer()
    {
        List<PlayerState> alivePlayers = GetAlivePlayers();
        if (alivePlayers.Count == 0) return;

        do
        {
            ActivePlayerIndex = (ActivePlayerIndex + 1) % Players.Count;
        }
        while (Players[ActivePlayerIndex].IsEliminated);

        TurnNumber++;
    }
}

public sealed class CombatAssignment
{
    public required string AttackerId { get; init; }
    public required string AttackerControllerId { get; init; }
    public required string DefendingPlayerId { get; init; }
}

public sealed class CombatBlock
{
    public required string BlockerId { get; init; }
    public required string AttackerId { get; init; }
}
