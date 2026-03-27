using MtgEngine.Application.Effects;
using MtgEngine.Domain.Entities;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Tests.Effects;

public class EffectHandlerTests
{
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
    public void DealDamage_ReducesPlayerLife()
    {
        GameState game = CreateGame();
        DealDamageHandler handler = new DealDamageHandler();
        EffectDefinition effect = new EffectDefinition
        {
            Trigger = EffectTrigger.OnCast,
            Action = EffectAction.DealDamage,
            Target = TargetType.AnyPlayer,
            Value = 5
        };

        handler.Execute(game, game.Players[0], effect, "p2");

        Assert.Equal(35, game.Players[1].Life);
    }

    [Fact]
    public void DealDamage_MarksCreatureDamage()
    {
        GameState game = CreateGame();
        DealDamageHandler handler = new DealDamageHandler();
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

        EffectDefinition effect = new EffectDefinition
        {
            Trigger = EffectTrigger.OnCast,
            Action = EffectAction.DealDamage,
            Target = TargetType.AnyCreature,
            Value = 1
        };

        handler.Execute(game, game.Players[0], effect, perm.InstanceId);

        Assert.Equal(1, perm.DamageMarked);
    }

    [Fact]
    public void GainLife_IncreasesLife()
    {
        GameState game = CreateGame();
        GainLifeHandler handler = new GainLifeHandler();
        EffectDefinition effect = new EffectDefinition
        {
            Trigger = EffectTrigger.OnCast,
            Action = EffectAction.GainLife,
            Value = 5
        };

        handler.Execute(game, game.Players[0], effect, null);

        Assert.Equal(45, game.Players[0].Life);
    }

    [Fact]
    public void DrawCard_DrawsCards()
    {
        GameState game = CreateGame();
        DrawCardHandler handler = new DrawCardHandler();
        CardDefinition cardDef = new CardDefinition
        {
            Id = "card",
            Name = "Card",
            Type = CardType.Creature
        };
        game.Players[0].Library.Add(new CardInstance(cardDef, "p1"));
        game.Players[0].Library.Add(new CardInstance(cardDef, "p1"));
        game.Players[0].Library.Add(new CardInstance(cardDef, "p1"));

        EffectDefinition effect = new EffectDefinition
        {
            Trigger = EffectTrigger.OnCast,
            Action = EffectAction.DrawCard,
            Value = 2
        };

        handler.Execute(game, game.Players[0], effect, null);

        Assert.Equal(2, game.Players[0].Hand.Count);
        _ = Assert.Single(game.Players[0].Library);
    }

    [Fact]
    public void DrawCard_EmptyLibrary_EliminatesPlayer()
    {
        GameState game = CreateGame();
        DrawCardHandler handler = new DrawCardHandler();

        EffectDefinition effect = new EffectDefinition
        {
            Trigger = EffectTrigger.OnCast,
            Action = EffectAction.DrawCard,
            Value = 1
        };

        handler.Execute(game, game.Players[0], effect, null);

        Assert.True(game.Players[0].IsEliminated);
    }

    [Fact]
    public void Destroy_RemovesFromBattlefield()
    {
        GameState game = CreateGame();
        DestroyHandler handler = new DestroyHandler();
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

        EffectDefinition effect = new EffectDefinition
        {
            Trigger = EffectTrigger.OnCast,
            Action = EffectAction.Destroy,
            Target = TargetType.AnyCreature
        };

        handler.Execute(game, game.Players[0], effect, perm.InstanceId);

        Assert.Empty(game.Players[1].Battlefield);
        _ = Assert.Single(game.Players[1].Graveyard);
    }

    [Fact]
    public void Tap_TapsTarget()
    {
        GameState game = CreateGame();
        TapHandler handler = new TapHandler();
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

        EffectDefinition effect = new EffectDefinition
        {
            Trigger = EffectTrigger.OnCast,
            Action = EffectAction.Tap,
            Target = TargetType.AnyCreature
        };

        handler.Execute(game, game.Players[0], effect, perm.InstanceId);

        Assert.True(perm.IsTapped);
    }

    [Fact]
    public void Exile_RemovesFromBattlefieldToExile()
    {
        GameState game = CreateGame();
        ExileHandler handler = new ExileHandler();
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

        EffectDefinition effect = new EffectDefinition
        {
            Trigger = EffectTrigger.OnCast,
            Action = EffectAction.Exile,
            Target = TargetType.AnyCreature
        };

        handler.Execute(game, game.Players[0], effect, perm.InstanceId);

        Assert.Empty(game.Players[1].Battlefield);
        _ = Assert.Single(game.Players[1].Exile);
    }
}
