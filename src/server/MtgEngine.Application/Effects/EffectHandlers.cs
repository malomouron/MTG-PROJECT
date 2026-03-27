using MtgEngine.Domain.Entities;
using MtgEngine.Domain.Interfaces;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Application.Effects;

public sealed class DealDamageHandler : IEffectHandler
{
    public EffectAction Action => EffectAction.DealDamage;

    public void Execute(GameState game, PlayerState caster, EffectDefinition effect, string? targetId)
    {
        if (targetId == null) return;

        // Target is a player
        PlayerState? targetPlayer = game.GetPlayer(targetId);
        if (targetPlayer != null)
        {
            targetPlayer.Life -= effect.Value;
            return;
        }

        // Target is a creature on the battlefield
        foreach (PlayerState player in game.Players)
        {
            Permanent? permanent = player.Battlefield.FirstOrDefault(p => p.InstanceId == targetId);
            if (permanent != null)
            {
                permanent.DamageMarked += effect.Value;
                return;
            }
        }
    }
}

public sealed class GainLifeHandler : IEffectHandler
{
    public EffectAction Action => EffectAction.GainLife;

    public void Execute(GameState game, PlayerState caster, EffectDefinition effect, string? targetId)
    {
        caster.Life += effect.Value;
    }
}

public sealed class DrawCardHandler : IEffectHandler
{
    public EffectAction Action => EffectAction.DrawCard;

    public void Execute(GameState game, PlayerState caster, EffectDefinition effect, string? targetId)
    {
        for (int i = 0; i < effect.Value; i++)
        {
            if (caster.Library.Count == 0)
            {
                caster.IsEliminated = true;
                return;
            }
            _ = caster.DrawCard();
        }
    }
}

public sealed class DestroyHandler : IEffectHandler
{
    public EffectAction Action => EffectAction.Destroy;

    public void Execute(GameState game, PlayerState caster, EffectDefinition effect, string? targetId)
    {
        if (targetId == null) return;

        foreach (PlayerState player in game.Players)
        {
            Permanent? permanent = player.Battlefield.FirstOrDefault(p => p.InstanceId == targetId);
            if (permanent != null)
            {
                _ = player.Battlefield.Remove(permanent);
                CardInstance card = new CardInstance(permanent.Definition, player.PlayerId);
                player.Graveyard.Add(card);
                return;
            }
        }
    }
}

public sealed class TapHandler : IEffectHandler
{
    public EffectAction Action => EffectAction.Tap;

    public void Execute(GameState game, PlayerState caster, EffectDefinition effect, string? targetId)
    {
        if (targetId == null) return;

        foreach (PlayerState player in game.Players)
        {
            Permanent? permanent = player.Battlefield.FirstOrDefault(p => p.InstanceId == targetId);
            if (permanent != null)
            {
                permanent.IsTapped = true;
                return;
            }
        }
    }
}

public sealed class UntapHandler : IEffectHandler
{
    public EffectAction Action => EffectAction.Untap;

    public void Execute(GameState game, PlayerState caster, EffectDefinition effect, string? targetId)
    {
        if (targetId == null) return;

        foreach (PlayerState player in game.Players)
        {
            Permanent? permanent = player.Battlefield.FirstOrDefault(p => p.InstanceId == targetId);
            if (permanent != null)
            {
                permanent.IsTapped = false;
                return;
            }
        }
    }
}

public sealed class AddManaHandler : IEffectHandler
{
    public EffectAction Action => EffectAction.AddMana;

    public void Execute(GameState game, PlayerState caster, EffectDefinition effect, string? targetId)
    {
        string colorStr = effect.ValueString ?? "C";
        ManaColor color = colorStr switch
        {
            "W" => ManaColor.White,
            "U" => ManaColor.Blue,
            "B" => ManaColor.Black,
            "R" => ManaColor.Red,
            "G" => ManaColor.Green,
            _ => ManaColor.Colorless
        };
        caster.ManaPool.Add(color, Math.Max(1, effect.Value));
    }
}

public sealed class ReturnToHandHandler : IEffectHandler
{
    public EffectAction Action => EffectAction.ReturnToHand;

    public void Execute(GameState game, PlayerState caster, EffectDefinition effect, string? targetId)
    {
        if (targetId == null) return;

        foreach (PlayerState player in game.Players)
        {
            Permanent? permanent = player.Battlefield.FirstOrDefault(p => p.InstanceId == targetId);
            if (permanent != null)
            {
                _ = player.Battlefield.Remove(permanent);
                CardInstance card = new CardInstance(permanent.Definition, player.PlayerId);
                player.Hand.Add(card);
                return;
            }
        }
    }
}

public sealed class ExileHandler : IEffectHandler
{
    public EffectAction Action => EffectAction.Exile;

    public void Execute(GameState game, PlayerState caster, EffectDefinition effect, string? targetId)
    {
        if (targetId == null) return;

        foreach (PlayerState player in game.Players)
        {
            Permanent? permanent = player.Battlefield.FirstOrDefault(p => p.InstanceId == targetId);
            if (permanent != null)
            {
                _ = player.Battlefield.Remove(permanent);
                CardInstance card = new CardInstance(permanent.Definition, player.PlayerId);
                player.Exile.Add(card);
                return;
            }
        }
    }
}

public sealed class CreateTokenHandler : IEffectHandler
{
    public EffectAction Action => EffectAction.CreateToken;

    public void Execute(GameState game, PlayerState caster, EffectDefinition effect, string? targetId)
    {
        CardDefinition tokenDef = new CardDefinition
        {
            Id = $"token_{Guid.NewGuid():N}",
            Name = effect.ValueString ?? "Token",
            Type = CardType.Creature,
            Power = effect.Value,
            Toughness = effect.Value
        };

        CardInstance tokenCard = new CardInstance(tokenDef, caster.PlayerId);
        Permanent permanent = new Permanent(tokenCard, game.TurnNumber);
        caster.Battlefield.Add(permanent);
    }
}
