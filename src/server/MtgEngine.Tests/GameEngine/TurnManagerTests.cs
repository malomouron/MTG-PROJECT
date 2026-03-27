using MtgEngine.Application.GameEngine;
using MtgEngine.Domain.Entities;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Tests.GameEngine;

public class TurnManagerTests
{
    private readonly EventBus _eventBus = new();
    private readonly TurnManager _turnManager;

    public TurnManagerTests()
    {
        _turnManager = new TurnManager(_eventBus);
    }

    private static GameState CreateTwoPlayerGame()
    {
        GameState game = new GameState
        {
            GameName = "Test",
            Status = GameStatus.InProgress,
            TurnNumber = 2,
            ActivePlayerIndex = 0
        };

        PlayerState p1 = new PlayerState("p1", "Player 1", 40);
        PlayerState p2 = new PlayerState("p2", "Player 2", 40);

        // Each player needs at least one card in library for draws
        CardDefinition cardDef = new CardDefinition
        {
            Id = "test_card",
            Name = "Test Card",
            Type = CardType.Creature,
            Cost = "1",
            Power = 1,
            Toughness = 1
        };
        p1.Library.Add(new CardInstance(cardDef, "p1"));
        p1.Library.Add(new CardInstance(cardDef, "p1"));
        p2.Library.Add(new CardInstance(cardDef, "p2"));
        p2.Library.Add(new CardInstance(cardDef, "p2"));

        game.Players.Add(p1);
        game.Players.Add(p2);
        return game;
    }

    [Fact]
    public void StartTurn_UntapsAllPermanents()
    {
        GameState game = CreateTwoPlayerGame();
        CardDefinition cardDef = new CardDefinition
        {
            Id = "creature",
            Name = "Creature",
            Type = CardType.Creature,
            Power = 2,
            Toughness = 2
        };
        Permanent perm = new Permanent(new CardInstance(cardDef, "p1"), 1) { IsTapped = true };
        game.Players[0].Battlefield.Add(perm);

        _turnManager.StartTurn(game);

        Assert.False(perm.IsTapped);
    }

    [Fact]
    public void StartTurn_SetsPhaseToMainPre()
    {
        GameState game = CreateTwoPlayerGame();
        _turnManager.StartTurn(game);

        Assert.Equal(Phase.MainPre, game.CurrentPhase);
    }

    [Fact]
    public void StartTurn_DrawsCardOnTurn2()
    {
        GameState game = CreateTwoPlayerGame();
        int initialHandCount = game.Players[0].Hand.Count;

        _turnManager.StartTurn(game);

        Assert.Equal(initialHandCount + 1, game.Players[0].Hand.Count);
    }

    [Fact]
    public void StartTurn_NoDrawOnTurn1()
    {
        GameState game = CreateTwoPlayerGame();
        game.TurnNumber = 1;
        int initialHandCount = game.Players[0].Hand.Count;

        _turnManager.StartTurn(game);

        Assert.Equal(initialHandCount, game.Players[0].Hand.Count);
    }

    [Fact]
    public void StartTurn_ResetsLandPlayedFlag()
    {
        GameState game = CreateTwoPlayerGame();
        game.Players[0].LandPlayedThisTurn = true;

        _turnManager.StartTurn(game);

        Assert.False(game.Players[0].LandPlayedThisTurn);
    }

    [Fact]
    public void AdvancePhase_MainPreToCombatBegin()
    {
        GameState game = CreateTwoPlayerGame();
        game.CurrentPhase = Phase.MainPre;

        _turnManager.AdvancePhase(game);

        Assert.Equal(Phase.CombatBegin, game.CurrentPhase);
    }

    [Fact]
    public void AdvancePhase_MainPostToEnd()
    {
        GameState game = CreateTwoPlayerGame();
        game.CurrentPhase = Phase.MainPost;

        _turnManager.AdvancePhase(game);

        Assert.Equal(Phase.End, game.CurrentPhase);
    }
}
