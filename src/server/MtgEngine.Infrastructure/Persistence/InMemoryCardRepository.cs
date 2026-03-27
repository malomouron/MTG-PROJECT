using MtgEngine.Domain.Interfaces;
using MtgEngine.Shared.Models;

namespace MtgEngine.Infrastructure.Persistence;

public sealed class InMemoryCardRepository : ICardRepository
{
    private readonly Dictionary<string, CardDefinition> _cards = new();

    public CardDefinition? GetById(string cardId) =>
        _cards.GetValueOrDefault(cardId);

    public IReadOnlyList<CardDefinition> GetAll() =>
        _cards.Values.ToList();

    public void Register(CardDefinition card) =>
        _cards[card.Id] = card;

    public void RegisterRange(IEnumerable<CardDefinition> cards)
    {
        foreach (CardDefinition card in cards)
            Register(card);
    }

    public bool Exists(string cardId) =>
        _cards.ContainsKey(cardId);
}
