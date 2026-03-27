using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Domain.Entities;

public sealed class StackItem
{
    public string InstanceId { get; }
    public CardDefinition Definition { get; }
    public string ControllerId { get; }
    public string? TargetId { get; }

    public StackItem(CardInstance card, string controllerId, string? targetId)
    {
        InstanceId = card.InstanceId;
        Definition = card.Definition;
        ControllerId = controllerId;
        TargetId = targetId;
    }
}
