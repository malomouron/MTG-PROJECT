using System.Text.Json.Serialization;
using MtgEngine.Shared.Enums;

namespace MtgEngine.Shared.Models;

public sealed class CardDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required CardType Type { get; init; }
    public string Cost { get; init; } = string.Empty;
    public int? Power { get; init; }
    public int? Toughness { get; init; }
    public bool IsLegendary { get; init; }
    public List<Keyword> Keywords { get; init; } = [];
    public List<EffectDefinition> Effects { get; init; } = [];
    public List<AbilityDefinition> Abilities { get; init; } = [];
    public List<ManaColor> ColorIdentity { get; init; } = [];
}

public sealed class EffectDefinition
{
    public required EffectTrigger Trigger { get; init; }
    public required EffectAction Action { get; init; }
    public TargetType Target { get; init; } = TargetType.None;
    public int Value { get; init; }
    public string? ValueString { get; init; }
}

public sealed class AbilityDefinition
{
    public required string Type { get; init; }
    public string Cost { get; init; } = string.Empty;
    public required EffectAction Action { get; init; }
    public string? Value { get; init; }
    public TargetType Target { get; init; } = TargetType.None;
}
