using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Domain.Entities;

public sealed class Permanent
{
    public string InstanceId { get; }
    public CardDefinition Definition { get; }
    public string ControllerId { get; set; }
    public bool IsTapped { get; set; }
    public int DamageMarked { get; set; }
    public bool HasSummoningSickness { get; set; }
    public int TurnEnteredBattlefield { get; set; }

    public int Power => Definition.Power ?? 0;
    public int Toughness => Definition.Toughness ?? 0;
    public bool IsCreature => Definition.Type == CardType.Creature;
    public bool HasKeyword(Keyword keyword) => Definition.Keywords.Contains(keyword);

    public bool CanAttack =>
        IsCreature &&
        !IsTapped &&
        (!HasSummoningSickness || HasKeyword(Keyword.Haste));

    public Permanent(CardInstance card, int turnNumber)
    {
        InstanceId = card.InstanceId;
        Definition = card.Definition;
        ControllerId = card.OwnerId;
        HasSummoningSickness = true;
        TurnEnteredBattlefield = turnNumber;
    }
}
