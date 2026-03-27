using MtgEngine.Shared.Models;

namespace MtgEngine.Domain.Interfaces;

public interface ICardRepository
{
    CardDefinition? GetById(string cardId);
    IReadOnlyList<CardDefinition> GetAll();
    void Register(CardDefinition card);
    void RegisterRange(IEnumerable<CardDefinition> cards);
    bool Exists(string cardId);
}
