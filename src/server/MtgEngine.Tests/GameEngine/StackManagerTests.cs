using MtgEngine.Application.CardEngine;
using MtgEngine.Application.Effects;
using MtgEngine.Application.GameEngine;
using MtgEngine.Domain.Entities;
using MtgEngine.Domain.Interfaces;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Tests.GameEngine;

public class StackManagerTests
{
    private readonly EventBus _eventBus = new();
    private readonly StackManager _stackManager;

    public StackManagerTests()
    {
        IEffectHandler[] handlers =
        [
            new DealDamageHandler(),
            new GainLifeHandler(),
            new DrawCardHandler(),
            new DestroyHandler(),
            new TapHandler(),
            new UntapHandler(),
            new AddManaHandler(),
            new CreateTokenHandler(),
            new ReturnToHandHandler(),
            new ExileHandler()
        ];
        EffectResolver resolver = new EffectResolver(handlers);
        _stackManager = new StackManager(resolver, _eventBus);
    }

    private static GameState CreateGame()
    {
        GameState game = new GameState
        {
            GameName = "Test",
            Status = GameStatus.InProgress,
            TurnNumber = 1,
            ActivePlayerIndex = 0
        };
        game.Players.Add(new PlayerState("p1", "Player 1", 40));
        game.Players.Add(new PlayerState("p2", "Player 2", 40));
        return game;
    }

    [Fact]
    public void PushToStack_AddsToStack()
    {
        GameState game = CreateGame();
        CardDefinition cardDef = new CardDefinition
        {
            Id = "bolt",
            Name = "Lightning Bolt",
            Type = CardType.Instant,
            Cost = "R"
        };
        CardInstance card = new CardInstance(cardDef, "p1");

        _stackManager.PushToStack(game, card, "p1", "p2");

        _ = Assert.Single(game.Stack);
        Assert.Equal("p1", game.Stack.Peek().ControllerId);
    }

    [Fact]
    public void ResolveTop_InstantGoesToGraveyard()
    {
        GameState game = CreateGame();
        CardDefinition cardDef = new CardDefinition
        {
            Id = "bolt",
            Name = "Lightning Bolt",
            Type = CardType.Instant,
            Cost = "R",
            Effects =
            [
                new EffectDefinition
                {
                    Trigger = EffectTrigger.OnCast,
                    Action = EffectAction.DealDamage,
                    Target = TargetType.Any,
                    Value = 3
                }
            ]
        };
        CardInstance card = new CardInstance(cardDef, "p1");
        _stackManager.PushToStack(game, card, "p1", "p2");

        _stackManager.ResolveTop(game);

        Assert.Empty(game.Stack);
        Assert.Equal(37, game.Players[1].Life); // 40 - 3 damage
        _ = Assert.Single(game.Players[0].Graveyard);
    }

    [Fact]
    public void ResolveTop_CreatureEntersBattlefield()
    {
        GameState game = CreateGame();
        CardDefinition cardDef = new CardDefinition
        {
            Id = "bear",
            Name = "Bear",
            Type = CardType.Creature,
            Cost = "1G",
            Power = 2,
            Toughness = 2
        };
        CardInstance card = new CardInstance(cardDef, "p1");
        _stackManager.PushToStack(game, card, "p1", null);

        _stackManager.ResolveTop(game);

        Assert.Empty(game.Stack);
        _ = Assert.Single(game.Players[0].Battlefield);
        Assert.Equal("Bear", game.Players[0].Battlefield[0].Definition.Name);
    }

    [Fact]
    public void ResolveTop_ETBTriggersResolve()
    {
        GameState game = CreateGame();
        CardDefinition cardDef = new CardDefinition
        {
            Id = "etb_creature",
            Name = "ETB Creature",
            Type = CardType.Creature,
            Cost = "1R",
            Power = 2,
            Toughness = 2,
            Effects =
            [
                new EffectDefinition
                {
                    Trigger = EffectTrigger.OnEnterBattlefield,
                    Action = EffectAction.DealDamage,
                    Target = TargetType.Any,
                    Value = 2
                }
            ]
        };
        CardInstance card = new CardInstance(cardDef, "p1");
        _stackManager.PushToStack(game, card, "p1", "p2");

        _stackManager.ResolveTop(game);

        Assert.Equal(38, game.Players[1].Life); // 40 - 2 from ETB
        _ = Assert.Single(game.Players[0].Battlefield);
    }

    [Fact]
    public void ResolveAll_ResolvesEntireStack_LIFO()
    {
        GameState game = CreateGame();
        CardDefinition bolt = new CardDefinition
        {
            Id = "bolt",
            Name = "Lightning Bolt",
            Type = CardType.Instant,
            Effects =
            [
                new EffectDefinition
                {
                    Trigger = EffectTrigger.OnCast,
                    Action = EffectAction.DealDamage,
                    Target = TargetType.Any,
                    Value = 3
                }
            ]
        };
        CardDefinition heal = new CardDefinition
        {
            Id = "heal",
            Name = "Healing Salve",
            Type = CardType.Instant,
            Effects =
            [
                new EffectDefinition
                {
                    Trigger = EffectTrigger.OnCast,
                    Action = EffectAction.GainLife,
                    Target = TargetType.Self,
                    Value = 3
                }
            ]
        };

        // Push bolt first, then heal — heal resolves first (LIFO)
        _stackManager.PushToStack(game, new CardInstance(bolt, "p1"), "p1", "p2");
        _stackManager.PushToStack(game, new CardInstance(heal, "p2"), "p2", null);

        _stackManager.ResolveAll(game);

        Assert.Empty(game.Stack);
        // heal resolves first (LIFO): p2 gains 3 → 43, then bolt resolves: p2 takes 3 → 40
        Assert.Equal(40, game.Players[1].Life);
    }

    [Fact]
    public void StateBasedActions_CreatureDiesFromLethalDamage()
    {
        GameState game = CreateGame();
        CardDefinition creatureDef = new CardDefinition
        {
            Id = "bear",
            Name = "Bear",
            Type = CardType.Creature,
            Power = 2,
            Toughness = 2
        };
        Permanent perm = new Permanent(new CardInstance(creatureDef, "p2"), 1);
        game.Players[1].Battlefield.Add(perm);

        CardDefinition boltDef = new CardDefinition
        {
            Id = "bolt",
            Name = "Lightning Bolt",
            Type = CardType.Instant,
            Effects =
            [
                new EffectDefinition
                {
                    Trigger = EffectTrigger.OnCast,
                    Action = EffectAction.DealDamage,
                    Target = TargetType.AnyCreature,
                    Value = 3
                }
            ]
        };
        _stackManager.PushToStack(game, new CardInstance(boltDef, "p1"), "p1", perm.InstanceId);

        _stackManager.ResolveTop(game);

        Assert.Empty(game.Players[1].Battlefield);
        _ = Assert.Single(game.Players[1].Graveyard);
    }

    [Fact]
    public void StateBasedActions_PlayerEliminatedAt0Life()
    {
        GameState game = CreateGame();
        game.Players[1].Life = 3;

        CardDefinition boltDef = new CardDefinition
        {
            Id = "bolt",
            Name = "Lightning Bolt",
            Type = CardType.Instant,
            Effects =
            [
                new EffectDefinition
                {
                    Trigger = EffectTrigger.OnCast,
                    Action = EffectAction.DealDamage,
                    Target = TargetType.AnyPlayer,
                    Value = 3
                }
            ]
        };
        _stackManager.PushToStack(game, new CardInstance(boltDef, "p1"), "p1", "p2");

        _stackManager.ResolveTop(game);

        Assert.True(game.Players[1].IsEliminated);
        Assert.Equal(0, game.Players[1].Life);
    }
}
