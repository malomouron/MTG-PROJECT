using MtgEngine.Domain.Entities;
using MtgEngine.Domain.Interfaces;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Application.CardEngine;

public sealed class EffectResolver
{
    private readonly Dictionary<EffectAction, IEffectHandler> _handlers;

    public EffectResolver(IEnumerable<IEffectHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.Action);
    }

    public void ResolveEffects(
        GameState game,
        PlayerState caster,
        IReadOnlyList<EffectDefinition> effects,
        EffectTrigger trigger,
        string? targetId)
    {
        List<EffectDefinition> matching = effects.Where(e => e.Trigger == trigger).ToList();

        foreach (EffectDefinition effect in matching)
        {
            if (_handlers.TryGetValue(effect.Action, out IEffectHandler? handler))
            {
                string? resolvedTarget = ResolveTarget(effect.Target, targetId, caster.PlayerId);
                handler.Execute(game, caster, effect, resolvedTarget);
            }
        }
    }

    private static string? ResolveTarget(TargetType targetType, string? explicitTarget, string casterId)
    {
        return targetType switch
        {
            TargetType.Self => casterId,
            TargetType.None => null,
            _ => explicitTarget
        };
    }
}
