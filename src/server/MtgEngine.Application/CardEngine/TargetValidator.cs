using MtgEngine.Domain.Entities;
using MtgEngine.Domain.Interfaces;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Application.CardEngine;

public sealed class TargetValidator
{
    public bool IsValidTarget(GameState game, PlayerState caster, TargetType targetType, string? targetId)
    {
        if (targetType == TargetType.None || targetType == TargetType.Self)
            return true;

        if (targetId == null)
            return false;

        return targetType switch
        {
            TargetType.AnyPlayer => game.GetPlayer(targetId) is { IsEliminated: false },
            TargetType.AnyCreature => FindCreature(game, targetId) != null,
            TargetType.Any => IsPlayerOrCreature(game, targetId),
            TargetType.Opponent => IsOpponent(game, caster.PlayerId, targetId),
            TargetType.SelfCreature => IsOwnedCreature(game, caster.PlayerId, targetId),
            TargetType.OpponentCreature => IsOpponentCreature(game, caster.PlayerId, targetId),
            _ => false
        };
    }

    private static Permanent? FindCreature(GameState game, string permanentId)
    {
        return game.Players
            .SelectMany(p => p.Battlefield)
            .FirstOrDefault(p => p.InstanceId == permanentId && p.IsCreature);
    }

    private static bool IsPlayerOrCreature(GameState game, string targetId)
    {
        return game.GetPlayer(targetId) is { IsEliminated: false }
            || FindCreature(game, targetId) != null;
    }

    private static bool IsOpponent(GameState game, string casterId, string targetId)
    {
        return targetId != casterId && game.GetPlayer(targetId) is { IsEliminated: false };
    }

    private static bool IsOwnedCreature(GameState game, string casterId, string targetId)
    {
        var player = game.GetPlayer(casterId);
        return player?.Battlefield.Any(p => p.InstanceId == targetId && p.IsCreature) ?? false;
    }

    private static bool IsOpponentCreature(GameState game, string casterId, string targetId)
    {
        return game.Players
            .Where(p => p.PlayerId != casterId)
            .SelectMany(p => p.Battlefield)
            .Any(p => p.InstanceId == targetId && p.IsCreature);
    }
}
