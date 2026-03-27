using MtgEngine.Application.CardEngine;
using MtgEngine.Application.GameEngine.Events;
using MtgEngine.Domain.Entities;
using MtgEngine.Domain.Interfaces;
using MtgEngine.Shared.Enums;

namespace MtgEngine.Application.GameEngine;

public sealed class TurnManager
{
    private readonly IEventBus _eventBus;

    public TurnManager(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void StartTurn(GameState game)
    {
        var player = game.ActivePlayer!;
        player.LandPlayedThisTurn = false;

        // Remove summoning sickness from permanents that were here before this turn
        foreach (var perm in player.Battlefield)
        {
            if (perm.TurnEnteredBattlefield < game.TurnNumber)
                perm.HasSummoningSickness = false;
        }

        SetPhase(game, Phase.Untap);
        ExecuteUntapPhase(game);

        SetPhase(game, Phase.Upkeep);
        // Upkeep triggers would go here

        SetPhase(game, Phase.Draw);
        ExecuteDrawPhase(game);

        SetPhase(game, Phase.MainPre);

        _eventBus.Publish(new TurnStartedEvent
        {
            GameId = game.GameId,
            PlayerId = player.PlayerId,
            TurnNumber = game.TurnNumber
        });
    }

    public void AdvancePhase(GameState game)
    {
        var nextPhase = game.CurrentPhase switch
        {
            Phase.MainPre => Phase.CombatBegin,
            Phase.CombatBegin => Phase.DeclareAttackers,
            Phase.DeclareAttackers => Phase.DeclareBlockers,
            Phase.DeclareBlockers => Phase.CombatDamage,
            Phase.CombatDamage => Phase.CombatEnd,
            Phase.CombatEnd => Phase.MainPost,
            Phase.MainPost => Phase.End,
            Phase.End => Phase.Cleanup,
            Phase.Cleanup => Phase.Untap, // signals end of turn
            _ => game.CurrentPhase
        };

        if (nextPhase == Phase.Untap)
        {
            EndTurn(game);
            return;
        }

        SetPhase(game, nextPhase);
    }

    public void EndTurn(GameState game)
    {
        ExecuteCleanupPhase(game);
        game.NextActivePlayer();
        StartTurn(game);
    }

    private void ExecuteUntapPhase(GameState game)
    {
        var player = game.ActivePlayer!;
        foreach (var permanent in player.Battlefield)
        {
            permanent.IsTapped = false;
        }
    }

    private void ExecuteDrawPhase(GameState game)
    {
        var player = game.ActivePlayer!;

        // First player on first turn doesn't draw (Commander rule variant — optional)
        if (game.TurnNumber == 1)
            return;

        var drawn = player.DrawCard();
        if (drawn == null)
        {
            player.IsEliminated = true;
            _eventBus.Publish(new PlayerEliminatedEvent
            {
                GameId = game.GameId,
                PlayerId = player.PlayerId,
                Reason = "Library empty"
            });
            CheckWinCondition(game);
            return;
        }

        _eventBus.Publish(new CardDrawnEvent
        {
            GameId = game.GameId,
            PlayerId = player.PlayerId,
            CardInstanceId = drawn.InstanceId
        });
    }

    private void ExecuteCleanupPhase(GameState game)
    {
        var player = game.ActivePlayer!;

        // Discard down to max hand size (7)
        while (player.Hand.Count > 7)
        {
            var discarded = player.Hand[^1];
            player.Hand.RemoveAt(player.Hand.Count - 1);
            player.Graveyard.Add(discarded);
        }

        // Clear damage on creatures
        foreach (var permanent in player.Battlefield)
        {
            permanent.DamageMarked = 0;
        }

        // Empty mana pool
        player.ManaPool.Clear();
    }

    private void CheckWinCondition(GameState game)
    {
        var alive = game.GetAlivePlayers();
        if (alive.Count == 1)
        {
            game.Status = GameStatus.Finished;
            _eventBus.Publish(new GameEndedEvent
            {
                GameId = game.GameId,
                WinnerId = alive[0].PlayerId
            });
        }
        else if (alive.Count == 0)
        {
            game.Status = GameStatus.Finished;
            _eventBus.Publish(new GameEndedEvent
            {
                GameId = game.GameId,
                WinnerId = string.Empty // draw
            });
        }
    }

    private void SetPhase(GameState game, Phase phase)
    {
        game.CurrentPhase = phase;
        _eventBus.Publish(new PhaseChangedEvent
        {
            GameId = game.GameId,
            Phase = phase
        });
    }
}
