namespace MtgEngine.Domain.Entities;

public sealed class PlayerState
{
    public string PlayerId { get; }
    public string PlayerName { get; }
    public int Life { get; set; }
    public bool IsEliminated { get; set; }
    public bool LandPlayedThisTurn { get; set; }
    public int CommanderCastCount { get; set; }
    public Dictionary<string, int> CommanderDamageReceived { get; } = new();

    public List<CardInstance> Hand { get; } = [];
    public List<CardInstance> Library { get; } = [];
    public List<Permanent> Battlefield { get; } = [];
    public List<CardInstance> Graveyard { get; } = [];
    public List<CardInstance> Exile { get; } = [];
    public List<CardInstance> CommandZone { get; } = [];
    public ValueObjects.ManaPool ManaPool { get; } = new();

    public PlayerState(string playerId, string playerName, int startingLife)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        Life = startingLife;
    }

    public CardInstance? DrawCard()
    {
        if (Library.Count == 0)
            return null;

        CardInstance card = Library[0];
        Library.RemoveAt(0);
        Hand.Add(card);
        return card;
    }

    public void ShuffleLibrary()
    {
        Random rng = Random.Shared;
        int n = Library.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (Library[k], Library[n]) = (Library[n], Library[k]);
        }
    }
}
