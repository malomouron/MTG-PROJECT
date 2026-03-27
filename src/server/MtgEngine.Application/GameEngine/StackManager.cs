using MtgEngine.Application.CardEngine;
using MtgEngine.Application.GameEngine.Events;
using MtgEngine.Domain.Entities;
using MtgEngine.Domain.Interfaces;
using MtgEngine.Shared.Enums;

namespace MtgEngine.Application.GameEngine;

public sealed class StackManager
{
    private readonly EffectResolver _effectResolver;
    private readonly IEventBus _eventBus;

    public StackManager(EffectResolver effectResolver, IEventBus eventBus)
    {
        _effectResolver = effectResolver;
        _eventBus = eventBus;
    }

    public void PushToStack(GameState game, CardInstance card, string controllerId, string? targetId)
    {
        var stackItem = new StackItem(card, controllerId, targetId);
        game.Stack.Push(stackItem);

        _eventBus.Publish(new CardPlayedEvent
        {
            GameId = game.GameId,
            PlayerId = controllerId,
            CardInstanceId = card.InstanceId,
            TargetId = targetId
        });
    }

    public void ResolveTop(GameState game)
    {
        if (game.Stack.Count == 0) return;

        var item = game.Stack.Pop();
        var caster = game.GetPlayer(item.ControllerId);
        if (caster == null) return;

        // Resolve on-cast effects
        _effectResolver.ResolveEffects(
            game, caster, item.Definition.Effects, EffectTrigger.OnCast, item.TargetId);

        // If it's a permanent type, put it on the battlefield
        if (IsPermanentType(item.Definition.Type))
        {
            var cardInstance = new CardInstance(item.Definition, item.ControllerId);
            var permanent = new Permanent(cardInstance, game.TurnNumber);
            caster.Battlefield.Add(permanent);

            // Resolve ETB triggers
            _effectResolver.ResolveEffects(
                game, caster, item.Definition.Effects, EffectTrigger.OnEnterBattlefield, item.TargetId);

            if (item.Definition.Type == CardType.Creature)
            {
                _eventBus.Publish(new CreatureEnteredBattlefieldEvent
                {
                    GameId = game.GameId,
                    PermanentId = permanent.InstanceId,
                    ControllerId = caster.PlayerId
                });
            }
        }
        else
        {
            // Instants/Sorceries go to graveyard after resolution
            var cardForGraveyard = new CardInstance(item.Definition, item.ControllerId);
            caster.Graveyard.Add(cardForGraveyard);
        }

        _eventBus.Publish(new SpellResolvedEvent
        {
            GameId = game.GameId,
            CardInstanceId = item.InstanceId
        });

        // Check for state-based actions (creatures with lethal damage, players at 0 life)
        CheckStateBasedActions(game);
    }

    public void ResolveAll(GameState game)
    {
        while (game.Stack.Count > 0)
        {
            ResolveTop(game);
        }
    }

    private void CheckStateBasedActions(GameState game)
    {
        // Check for dead creatures
        foreach (var player in game.Players.Where(p => !p.IsEliminated))
        {
            var deadCreatures = player.Battlefield
                .Where(p => p.IsCreature && p.DamageMarked >= p.Toughness)
                .ToList();

            foreach (var dead in deadCreatures)
            {
                player.Battlefield.Remove(dead);
                var card = new CardInstance(dead.Definition, player.PlayerId);
                player.Graveyard.Add(card);

                _eventBus.Publish(new CreatureDiedEvent
                {
                    GameId = game.GameId,
                    PermanentId = dead.InstanceId,
                    ControllerId = player.PlayerId
                });
            }
        }

        // Check for eliminated players
        foreach (var player in game.Players.Where(p => !p.IsEliminated))
        {
            if (player.Life <= 0)
            {
                player.IsEliminated = true;
                _eventBus.Publish(new PlayerEliminatedEvent
                {
                    GameId = game.GameId,
                    PlayerId = player.PlayerId,
                    Reason = "Life reached 0"
                });
            }
        }

        // Check win condition
        var alive = game.GetAlivePlayers();
        if (alive.Count <= 1 && game.Status == GameStatus.InProgress)
        {
            game.Status = GameStatus.Finished;
            _eventBus.Publish(new GameEndedEvent
            {
                GameId = game.GameId,
                WinnerId = alive.FirstOrDefault()?.PlayerId ?? string.Empty
            });
        }
    }

    private static bool IsPermanentType(CardType type) =>
        type is CardType.Creature or CardType.Enchantment or CardType.Artifact;
}
