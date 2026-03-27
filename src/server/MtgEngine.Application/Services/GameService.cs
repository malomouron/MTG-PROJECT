using MtgEngine.Application.CardEngine;
using MtgEngine.Application.GameEngine;
using MtgEngine.Application.GameEngine.Events;
using MtgEngine.Domain.Entities;
using MtgEngine.Domain.Interfaces;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Application.Services;

public sealed class GameService
{
    private readonly Dictionary<string, GameState> _games = new();
    private readonly ICardRepository _cardRepository;
    private readonly TurnManager _turnManager;
    private readonly StackManager _stackManager;
    private readonly CombatManager _combatManager;
    private readonly TargetValidator _targetValidator;
    private readonly IEventBus _eventBus;

    public GameService(
        ICardRepository cardRepository,
        TurnManager turnManager,
        StackManager stackManager,
        CombatManager combatManager,
        TargetValidator targetValidator,
        IEventBus eventBus)
    {
        _cardRepository = cardRepository;
        _turnManager = turnManager;
        _stackManager = stackManager;
        _combatManager = combatManager;
        _targetValidator = targetValidator;
        _eventBus = eventBus;
    }

    public GameState CreateGame(string gameName, int maxPlayers)
    {
        if (maxPlayers < 2 || maxPlayers > 6)
            throw new ArgumentOutOfRangeException(nameof(maxPlayers), "Must be between 2 and 6");

        var game = new GameState
        {
            GameName = gameName,
            MaxPlayers = maxPlayers
        };

        _games[game.GameId] = game;
        return game;
    }

    public GameState? GetGame(string gameId) =>
        _games.GetValueOrDefault(gameId);

    public IReadOnlyList<GameState> GetAllGames() =>
        _games.Values.ToList();

    public Result JoinGame(string gameId, string playerId, string playerName, List<string> deckCardIds, string commanderId)
    {
        var game = GetGame(gameId);
        if (game == null)
            return Result.Fail("Game not found");
        if (game.Status != GameStatus.WaitingForPlayers)
            return Result.Fail("Game already started");
        if (game.Players.Count >= game.MaxPlayers)
            return Result.Fail("Game is full");
        if (game.Players.Any(p => p.PlayerId == playerId))
            return Result.Fail("Already in this game");

        // Validate deck
        if (deckCardIds.Count != 100)
            return Result.Fail("Deck must contain exactly 100 cards");

        if (!deckCardIds.Contains(commanderId))
            return Result.Fail("Commander must be in the deck list");

        var commanderDef = _cardRepository.GetById(commanderId);
        if (commanderDef == null)
            return Result.Fail($"Commander card not found: {commanderId}");
        if (commanderDef.Type != CardType.Creature || !commanderDef.IsLegendary)
            return Result.Fail("Commander must be a legendary creature");

        var player = new PlayerState(playerId, playerName, game.StartingLife);

        foreach (var cardId in deckCardIds)
        {
            var def = _cardRepository.GetById(cardId);
            if (def == null)
                return Result.Fail($"Card not found: {cardId}");

            var instance = new CardInstance(def, playerId);

            if (cardId == commanderId)
                player.CommandZone.Add(instance);
            else
                player.Library.Add(instance);
        }

        player.ShuffleLibrary();
        game.Players.Add(player);
        return Result.Ok();
    }

    public Result StartGame(string gameId, string requestingPlayerId)
    {
        var game = GetGame(gameId);
        if (game == null)
            return Result.Fail("Game not found");
        if (game.Status != GameStatus.WaitingForPlayers)
            return Result.Fail("Game already started");
        if (game.Players.Count < 2)
            return Result.Fail("Need at least 2 players");
        if (game.Players[0].PlayerId != requestingPlayerId)
            return Result.Fail("Only the game creator can start");

        game.Status = GameStatus.InProgress;
        game.TurnNumber = 1;
        game.ActivePlayerIndex = 0;

        // Shuffle turn order
        var rng = Random.Shared;
        int n = game.Players.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (game.Players[k], game.Players[n]) = (game.Players[n], game.Players[k]);
        }

        // Draw initial hands (7 cards each)
        foreach (var player in game.Players)
        {
            for (int i = 0; i < 7; i++)
                player.DrawCard();
        }

        _eventBus.Publish(new GameStartedEvent
        {
            GameId = game.GameId,
            PlayerIds = game.Players.Select(p => p.PlayerId).ToList()
        });

        _turnManager.StartTurn(game);
        return Result.Ok();
    }

    public Result PlayCard(string gameId, string playerId, string cardInstanceId, string? targetId)
    {
        var game = GetGame(gameId);
        if (game == null) return Result.Fail("Game not found");
        if (game.Status != GameStatus.InProgress) return Result.Fail("Game not in progress");

        var player = game.GetPlayer(playerId);
        if (player == null || player.IsEliminated) return Result.Fail("Invalid player");

        // Find card in hand
        var card = player.Hand.FirstOrDefault(c => c.InstanceId == cardInstanceId);
        // Or check command zone
        card ??= player.CommandZone.FirstOrDefault(c => c.InstanceId == cardInstanceId);

        if (card == null) return Result.Fail("Card not in hand or command zone");

        var def = card.Definition;
        var isFromCommandZone = player.CommandZone.Contains(card);

        // Validate timing
        if (def.Type == CardType.Land)
            return PlayLand(game, player, card);

        if (def.Type is CardType.Sorcery or CardType.Creature or CardType.Enchantment or CardType.Artifact)
        {
            if (game.ActivePlayer?.PlayerId != playerId)
                return Result.Fail("Not your turn");
            if (game.CurrentPhase is not (Phase.MainPre or Phase.MainPost))
                return Result.Fail("Can only play this during a main phase");
        }

        if (def.Type == CardType.Instant)
        {
            // Instants can be played anytime with priority
        }

        // Validate mana cost
        var manaCost = ManaCost.Parse(def.Cost);
        if (isFromCommandZone)
        {
            // Commander tax: +2 per previous cast
            manaCost = ManaCost.Parse(def.Cost);
            var taxAmount = player.CommanderCastCount * 2;
            manaCost = new ManaCost
            {
                Generic = manaCost.Generic + taxAmount,
                White = manaCost.White,
                Blue = manaCost.Blue,
                Black = manaCost.Black,
                Red = manaCost.Red,
                Green = manaCost.Green
            };
        }

        if (!player.ManaPool.CanPay(manaCost))
            return Result.Fail("Not enough mana");

        // Validate target if needed
        var firstEffect = def.Effects.FirstOrDefault();
        if (firstEffect != null && firstEffect.Target != TargetType.None && firstEffect.Target != TargetType.Self)
        {
            if (!_targetValidator.IsValidTarget(game, player, firstEffect.Target, targetId))
                return Result.Fail("Invalid target");
        }

        // Pay the cost
        player.ManaPool.Pay(manaCost);

        // Remove from hand or command zone
        if (isFromCommandZone)
        {
            player.CommandZone.Remove(card);
            player.CommanderCastCount++;
        }
        else
        {
            player.Hand.Remove(card);
        }

        // Push to stack
        _stackManager.PushToStack(game, card, playerId, targetId);

        return Result.Ok();
    }

    private Result PlayLand(GameState game, PlayerState player, CardInstance card)
    {
        if (game.ActivePlayer?.PlayerId != player.PlayerId)
            return Result.Fail("Not your turn");
        if (game.CurrentPhase is not (Phase.MainPre or Phase.MainPost))
            return Result.Fail("Can only play lands during a main phase");
        if (player.LandPlayedThisTurn)
            return Result.Fail("Already played a land this turn");

        player.Hand.Remove(card);
        player.LandPlayedThisTurn = true;

        var permanent = new Permanent(card, game.TurnNumber)
        {
            HasSummoningSickness = false // Lands don't have summoning sickness
        };
        player.Battlefield.Add(permanent);

        _eventBus.Publish(new PermanentEnteredBattlefieldEvent
        {
            GameId = game.GameId,
            PermanentId = permanent.InstanceId,
            ControllerId = player.PlayerId
        });

        return Result.Ok();
    }

    public Result PassPriority(string gameId, string playerId)
    {
        var game = GetGame(gameId);
        if (game == null) return Result.Fail("Game not found");
        if (game.Status != GameStatus.InProgress) return Result.Fail("Game not in progress");

        var player = game.GetPlayer(playerId);
        if (player == null || player.IsEliminated) return Result.Fail("Invalid player");

        game.PlayersPassedPriority.Add(playerId);

        var alivePlayers = game.GetAlivePlayers();
        if (game.PlayersPassedPriority.IsSupersetOf(alivePlayers.Select(p => p.PlayerId)))
        {
            game.PlayersPassedPriority.Clear();

            if (game.Stack.Count > 0)
            {
                _stackManager.ResolveTop(game);
            }
            else
            {
                _turnManager.AdvancePhase(game);
            }
        }

        return Result.Ok();
    }

    public Result DeclareAttackers(string gameId, string playerId, List<CombatAssignment> attackers)
    {
        var game = GetGame(gameId);
        if (game == null) return Result.Fail("Game not found");
        if (game.CurrentPhase != Phase.DeclareAttackers) return Result.Fail("Not in attack phase");

        if (!_combatManager.DeclareAttackers(game, playerId, attackers))
            return Result.Fail("Invalid attackers");

        return Result.Ok();
    }

    public Result DeclareBlockers(string gameId, string playerId, List<CombatBlock> blockers)
    {
        var game = GetGame(gameId);
        if (game == null) return Result.Fail("Game not found");
        if (game.CurrentPhase != Phase.DeclareBlockers) return Result.Fail("Not in block phase");

        if (!_combatManager.DeclareBlockers(game, playerId, blockers))
            return Result.Fail("Invalid blockers");

        return Result.Ok();
    }

    public Result ResolveCombat(string gameId)
    {
        var game = GetGame(gameId);
        if (game == null) return Result.Fail("Game not found");
        if (game.CurrentPhase != Phase.CombatDamage) return Result.Fail("Not in damage phase");

        _combatManager.ResolveCombatDamage(game);
        return Result.Ok();
    }

    public Result ActivateAbility(string gameId, string playerId, string permanentId, int abilityIndex, string? targetId)
    {
        var game = GetGame(gameId);
        if (game == null) return Result.Fail("Game not found");

        var player = game.GetPlayer(playerId);
        if (player == null || player.IsEliminated) return Result.Fail("Invalid player");

        var permanent = player.Battlefield.FirstOrDefault(p => p.InstanceId == permanentId);
        if (permanent == null) return Result.Fail("Permanent not found");

        if (abilityIndex < 0 || abilityIndex >= permanent.Definition.Abilities.Count)
            return Result.Fail("Invalid ability index");

        var ability = permanent.Definition.Abilities[abilityIndex];

        // Handle tap cost
        if (ability.Cost == "tap")
        {
            if (permanent.IsTapped) return Result.Fail("Permanent is already tapped");
            if (permanent.HasSummoningSickness && permanent.IsCreature)
                return Result.Fail("Summoning sickness");
            permanent.IsTapped = true;
        }

        // Execute ability effect
        var effectDef = new EffectDefinition
        {
            Trigger = EffectTrigger.OnCast,
            Action = ability.Action,
            Target = ability.Target,
            Value = int.TryParse(ability.Value, out var val) ? val : 0,
            ValueString = ability.Value
        };

        var handler = _stackManager;
        // For activated abilities, resolve immediately (simplified V1)
        var resolver = new EffectResolver(GetEffectHandlers());
        resolver.ResolveEffects(game, player, [effectDef], EffectTrigger.OnCast, targetId);

        return Result.Ok();
    }

    private IEnumerable<IEffectHandler> GetEffectHandlers()
    {
        // This would normally come from DI, but for the ability activation path
        return
        [
            new Effects.DealDamageHandler(),
            new Effects.GainLifeHandler(),
            new Effects.DrawCardHandler(),
            new Effects.DestroyHandler(),
            new Effects.TapHandler(),
            new Effects.UntapHandler(),
            new Effects.AddManaHandler(),
            new Effects.CreateTokenHandler(),
            new Effects.ReturnToHandHandler(),
            new Effects.ExileHandler()
        ];
    }
}

public sealed class Result
{
    public bool Success { get; private init; }
    public string? Error { get; private init; }

    public static Result Ok() => new() { Success = true };
    public static Result Fail(string error) => new() { Success = false, Error = error };
}
