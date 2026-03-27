using MtgEngine.Application.CardEngine;
using MtgEngine.Domain.Entities;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Tests.CardEngine;

public class TargetValidatorTests
{
    private readonly TargetValidator _validator = new();

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
    public void None_AlwaysValid()
    {
        GameState game = CreateGame();
        Assert.True(_validator.IsValidTarget(game, game.Players[0], TargetType.None, null));
    }

    [Fact]
    public void Self_AlwaysValid()
    {
        GameState game = CreateGame();
        Assert.True(_validator.IsValidTarget(game, game.Players[0], TargetType.Self, null));
    }

    [Fact]
    public void AnyPlayer_ValidWithPlayerId()
    {
        GameState game = CreateGame();
        Assert.True(_validator.IsValidTarget(game, game.Players[0], TargetType.AnyPlayer, "p2"));
    }

    [Fact]
    public void AnyPlayer_InvalidWithNoTarget()
    {
        GameState game = CreateGame();
        Assert.False(_validator.IsValidTarget(game, game.Players[0], TargetType.AnyPlayer, null));
    }

    [Fact]
    public void AnyCreature_ValidWithCreatureOnBattlefield()
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

        Assert.True(_validator.IsValidTarget(game, game.Players[0], TargetType.AnyCreature, perm.InstanceId));
    }

    [Fact]
    public void AnyCreature_InvalidWithNonExistentId()
    {
        GameState game = CreateGame();
        Assert.False(_validator.IsValidTarget(game, game.Players[0], TargetType.AnyCreature, "nonexistent"));
    }

    [Fact]
    public void Opponent_ValidWithOpponentId()
    {
        GameState game = CreateGame();
        Assert.True(_validator.IsValidTarget(game, game.Players[0], TargetType.Opponent, "p2"));
    }

    [Fact]
    public void Opponent_InvalidWithSelfId()
    {
        GameState game = CreateGame();
        Assert.False(_validator.IsValidTarget(game, game.Players[0], TargetType.Opponent, "p1"));
    }
}
