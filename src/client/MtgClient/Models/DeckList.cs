namespace MtgClient.Models;

/// <summary>
/// Parsed deck from import text.
/// </summary>
public sealed class DeckList
{
    public string? CommanderId { get; set; }
    public List<DeckEntry> Entries { get; set; } = [];
    public List<string> Errors { get; set; } = [];

    public int TotalCards => Entries.Sum(e => e.Quantity);
    public bool IsValid => CommanderId != null && TotalCards == 100 && Errors.Count == 0;
}

public sealed class DeckEntry
{
    public required string CardId { get; init; }
    public required string CardName { get; init; }
    public int Quantity { get; init; } = 1;
}
