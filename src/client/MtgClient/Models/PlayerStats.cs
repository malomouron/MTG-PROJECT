namespace MtgClient.Models;

/// <summary>
/// Player statistics.
/// </summary>
public sealed class PlayerStats
{
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses => GamesPlayed - Wins;
    public double WinRate => GamesPlayed > 0 ? (double)Wins / GamesPlayed * 100 : 0;
    public List<GameRecord> RecentGames { get; set; } = [];
}

public sealed class GameRecord
{
    public required string GameId { get; init; }
    public required string GameName { get; init; }
    public bool IsWin { get; init; }
    public required string CommanderUsed { get; init; }
    public int PlayerCount { get; init; }
    public DateTime PlayedAt { get; init; }
    public TimeSpan Duration { get; init; }
}
