using MtgEngine.Domain.Entities;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Domain.Interfaces;

public interface IEffectHandler
{
    EffectAction Action { get; }
    void Execute(GameState game, PlayerState caster, EffectDefinition effect, string? targetId);
}
