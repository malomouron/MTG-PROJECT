using MtgEngine.Application.CardEngine;
using MtgEngine.Application.Effects;
using MtgEngine.Application.GameEngine;
using MtgEngine.Application.Services;
using MtgEngine.Domain.Entities;
using MtgEngine.Domain.Interfaces;
using MtgEngine.Infrastructure.Persistence;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Tests.GameEngine;

public class GameServiceTests
{
    private readonly GameService _gameService;
    private readonly InMemoryCardRepository _cardRepo;

    public GameServiceTests()
    {
        _cardRepo = new InMemoryCardRepository();
        var eventBus = new EventBus();
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
        var resolver = new EffectResolver(handlers);
        var turnManager = new TurnManager(eventBus);
        var stackManager = new StackManager(resolver, eventBus);
        var combatManager = new CombatManager(eventBus);
        var targetValidator = new TargetValidator();

        _gameService = new GameService(
            _cardRepo, turnManager, stackManager, combatManager, targetValidator, eventBus);
    }

    private void RegisterCommander()
    {
        _cardRepo.Register(new CardDefinition
        {
            Id = "commander_a",
            Name = "Commander A",
            Type = CardType.Creature,
            Cost = "2RR",
            Power = 3,
            Toughness = 3,
            IsLegendary = true,
            ColorIdentity = [ManaColor.Red]
        });
    }

    private void RegisterBasicCards()
    {
        _cardRepo.Register(new CardDefinition
        {
            Id = "mountain",
            Name = "Mountain",
            Type = CardType.Land,
            ColorIdentity = [ManaColor.Red]
        });

        _cardRepo.Register(new CardDefinition
        {
            Id = "grizzly_bears",
            Name = "Grizzly Bears",
            Type = CardType.Creature,
            Cost = "1G",
            Power = 2,
            Toughness = 2
        });
    }

    private List<string> BuildDeck(string commanderId)
    {
        var deck = new List<string> { commanderId };
        for (int i = 0; i < 99; i++)
            deck.Add("mountain");
        return deck;
    }

    [Fact]
    public void CreateGame_ReturnsNewGame()
    {
        var game = _gameService.CreateGame("Test Game", 4);

        Assert.NotNull(game);
        Assert.Equal("Test Game", game.GameName);
        Assert.Equal(4, game.MaxPlayers);
        Assert.Equal(GameStatus.WaitingForPlayers, game.Status);
    }

    [Fact]
    public void CreateGame_InvalidMaxPlayers_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _gameService.CreateGame("Test", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _gameService.CreateGame("Test", 7));
    }

    [Fact]
    public void JoinGame_ValidDeck_Succeeds()
    {
        RegisterCommander();
        RegisterBasicCards();
        var game = _gameService.CreateGame("Test", 4);
        var deck = BuildDeck("commander_a");

        var result = _gameService.JoinGame(game.GameId, "p1", "Player 1", deck, "commander_a");

        Assert.True(result.Success);
        Assert.Single(game.Players);
    }

    [Fact]
    public void JoinGame_WrongDeckSize_Fails()
    {
        RegisterCommander();
        var game = _gameService.CreateGame("Test", 4);
        var deck = new List<string> { "commander_a", "mountain" }; // Only 2 cards

        var result = _gameService.JoinGame(game.GameId, "p1", "Player 1", deck, "commander_a");

        Assert.False(result.Success);
        Assert.Contains("100", result.Error!);
    }

    [Fact]
    public void JoinGame_NonLegendaryCommander_Fails()
    {
        RegisterBasicCards();
        _cardRepo.Register(new CardDefinition
        {
            Id = "not_legendary",
            Name = "Regular Creature",
            Type = CardType.Creature,
            IsLegendary = false
        });

        var game = _gameService.CreateGame("Test", 4);
        var deck = BuildDeck("not_legendary");

        var result = _gameService.JoinGame(game.GameId, "p1", "Player 1", deck, "not_legendary");

        Assert.False(result.Success);
        Assert.Contains("legendary", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartGame_WithTwoPlayers_Succeeds()
    {
        RegisterCommander();
        RegisterBasicCards();
        var game = _gameService.CreateGame("Test", 4);

        _gameService.JoinGame(game.GameId, "p1", "Player 1", BuildDeck("commander_a"), "commander_a");
        _gameService.JoinGame(game.GameId, "p2", "Player 2", BuildDeck("commander_a"), "commander_a");

        var result = _gameService.StartGame(game.GameId, "p1");

        Assert.True(result.Success);
        Assert.Equal(GameStatus.InProgress, game.Status);
        // Both players should have drawn 7 cards
        foreach (var player in game.Players)
        {
            Assert.Equal(7, player.Hand.Count);
        }
    }

    [Fact]
    public void StartGame_OnlyOnePlayer_Fails()
    {
        RegisterCommander();
        RegisterBasicCards();
        var game = _gameService.CreateGame("Test", 4);
        _gameService.JoinGame(game.GameId, "p1", "Player 1", BuildDeck("commander_a"), "commander_a");

        var result = _gameService.StartGame(game.GameId, "p1");

        Assert.False(result.Success);
        Assert.Contains("2 players", result.Error!);
    }

    [Fact]
    public void GetGame_ExistingGame_Returns()
    {
        var game = _gameService.CreateGame("Test", 4);
        var found = _gameService.GetGame(game.GameId);

        Assert.NotNull(found);
        Assert.Equal(game.GameId, found.GameId);
    }

    [Fact]
    public void GetGame_NonExistent_ReturnsNull()
    {
        var found = _gameService.GetGame("doesnt-exist");
        Assert.Null(found);
    }
}
