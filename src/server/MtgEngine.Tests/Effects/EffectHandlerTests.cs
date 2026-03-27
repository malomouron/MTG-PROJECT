using MtgEngine.Application.Effects;
using MtgEngine.Domain.Entities;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Tests.Effects;

public class EffectHandlerTests
{
    private static GameState CreateGame()
    {
        var game = new GameState
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
        var game = CreateGame();
        var handler = new DealDamageHandler();
        var effect = new EffectDefinition
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
        var game = CreateGame();
        var handler = new DealDamageHandler();
        var creatureDef = new CardDefinition
        {
            Id = "bear",
            Name = "Bear",
            Type = CardType.Creature,
            Power = 2,
            Toughness = 2
        };
        var perm = new Permanent(new CardInstance(creatureDef, "p2"), 1);
        game.Players[1].Battlefield.Add(perm);

        var effect = new EffectDefinition
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
        var game = CreateGame();
        var handler = new GainLifeHandler();
        var effect = new EffectDefinition
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
        var game = CreateGame();
        var handler = new DrawCardHandler();
        var cardDef = new CardDefinition
        {
            Id = "card",
            Name = "Card",
            Type = CardType.Creature
        };
        game.Players[0].Library.Add(new CardInstance(cardDef, "p1"));
        game.Players[0].Library.Add(new CardInstance(cardDef, "p1"));
        game.Players[0].Library.Add(new CardInstance(cardDef, "p1"));

        var effect = new EffectDefinition
        {
            Trigger = EffectTrigger.OnCast,
            Action = EffectAction.DrawCard,
            Value = 2
        };

        handler.Execute(game, game.Players[0], effect, null);

        Assert.Equal(2, game.Players[0].Hand.Count);
        Assert.Single(game.Players[0].Library);
    }

    [Fact]
    public void DrawCard_EmptyLibrary_EliminatesPlayer()
    {
        var game = CreateGame();
        var handler = new DrawCardHandler();

        var effect = new EffectDefinition
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
        var game = CreateGame();
        var handler = new DestroyHandler();
        var creatureDef = new CardDefinition
        {
            Id = "bear",
            Name = "Bear",
            Type = CardType.Creature,
            Power = 2,
            Toughness = 2
        };
        var perm = new Permanent(new CardInstance(creatureDef, "p2"), 1);
        game.Players[1].Battlefield.Add(perm);

        var effect = new EffectDefinition
        {
            Trigger = EffectTrigger.OnCast,
            Action = EffectAction.Destroy,
            Target = TargetType.AnyCreature
        };

        handler.Execute(game, game.Players[0], effect, perm.InstanceId);

        Assert.Empty(game.Players[1].Battlefield);
        Assert.Single(game.Players[1].Graveyard);
    }

    [Fact]
    public void Tap_TapsTarget()
    {
        var game = CreateGame();
        var handler = new TapHandler();
        var creatureDef = new CardDefinition
        {
            Id = "bear",
            Name = "Bear",
            Type = CardType.Creature,
            Power = 2,
            Toughness = 2
        };
        var perm = new Permanent(new CardInstance(creatureDef, "p2"), 1);
        game.Players[1].Battlefield.Add(perm);

        var effect = new EffectDefinition
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
        var game = CreateGame();
        var handler = new ExileHandler();
        var creatureDef = new CardDefinition
        {
            Id = "bear",
            Name = "Bear",
            Type = CardType.Creature,
            Power = 2,
            Toughness = 2
        };
        var perm = new Permanent(new CardInstance(creatureDef, "p2"), 1);
        game.Players[1].Battlefield.Add(perm);

        var effect = new EffectDefinition
        {
            Trigger = EffectTrigger.OnCast,
            Action = EffectAction.Exile,
            Target = TargetType.AnyCreature
        };

        handler.Execute(game, game.Players[0], effect, perm.InstanceId);

        Assert.Empty(game.Players[1].Battlefield);
        Assert.Single(game.Players[1].Exile);
    }
}
