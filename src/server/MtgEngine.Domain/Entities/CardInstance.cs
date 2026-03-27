using MtgEngine.Shared.Models;

namespace MtgEngine.Domain.Entities;

public sealed class CardInstance
{
    public string InstanceId { get; } = Guid.NewGuid().ToString();
    public CardDefinition Definition { get; }
    public string OwnerId { get; }

    public CardInstance(CardDefinition definition, string ownerId)
    {
        Definition = definition;
        OwnerId = ownerId;
    }
}
