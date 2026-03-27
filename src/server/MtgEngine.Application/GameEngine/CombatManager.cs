using MtgEngine.Application.GameEngine.Events;
using MtgEngine.Domain.Entities;
using MtgEngine.Domain.Interfaces;
using MtgEngine.Shared.Enums;

namespace MtgEngine.Application.GameEngine;

public sealed class CombatManager
{
    private readonly IEventBus _eventBus;

    public CombatManager(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public bool DeclareAttackers(GameState game, string playerId, List<CombatAssignment> attackers)
    {
        if (game.ActivePlayer?.PlayerId != playerId)
            return false;

        var player = game.GetPlayer(playerId)!;

        foreach (var atk in attackers)
        {
            var creature = player.Battlefield.FirstOrDefault(p => p.InstanceId == atk.AttackerId);
            if (creature == null || !creature.CanAttack)
                return false;

            if (game.GetPlayer(atk.DefendingPlayerId) is not { IsEliminated: false })
                return false;
        }

        game.CombatAttackers.Clear();
        game.CombatAttackers.AddRange(attackers);

        // Tap attackers (unless Vigilance)
        foreach (var atk in attackers)
        {
            var creature = player.Battlefield.First(p => p.InstanceId == atk.AttackerId);
            if (!creature.HasKeyword(Keyword.Vigilance))
                creature.IsTapped = true;
        }

        return true;
    }

    public bool DeclareBlockers(GameState game, string playerId, List<CombatBlock> blockers)
    {
        var player = game.GetPlayer(playerId);
        if (player == null || player.IsEliminated) return false;

        foreach (var block in blockers)
        {
            var blocker = player.Battlefield.FirstOrDefault(p =>
                p.InstanceId == block.BlockerId && p.IsCreature && !p.IsTapped);
            if (blocker == null) return false;

            var attackerExists = game.CombatAttackers.Any(a => a.AttackerId == block.AttackerId);
            if (!attackerExists) return false;
        }

        game.CombatBlockers.AddRange(blockers);
        return true;
    }

    public void ResolveCombatDamage(GameState game)
    {
        var activePlayer = game.ActivePlayer!;

        foreach (var atk in game.CombatAttackers)
        {
            var attacker = activePlayer.Battlefield.FirstOrDefault(p => p.InstanceId == atk.AttackerId);
            if (attacker == null) continue;

            var blocks = game.CombatBlockers.Where(b => b.AttackerId == atk.AttackerId).ToList();

            if (blocks.Count == 0)
            {
                // Unblocked — damage goes to defending player
                var defender = game.GetPlayer(atk.DefendingPlayerId);
                if (defender != null)
                {
                    defender.Life -= attacker.Power;

                    _eventBus.Publish(new DamageDealtEvent
                    {
                        GameId = game.GameId,
                        SourceId = attacker.InstanceId,
                        TargetId = defender.PlayerId,
                        Amount = attacker.Power
                    });

                    // Track commander damage
                    if (attacker.Definition.IsLegendary)
                    {
                        defender.CommanderDamageReceived.TryGetValue(attacker.Definition.Id, out int current);
                        defender.CommanderDamageReceived[attacker.Definition.Id] = current + attacker.Power;

                        if (defender.CommanderDamageReceived[attacker.Definition.Id] >= 21)
                        {
                            defender.IsEliminated = true;
                            _eventBus.Publish(new PlayerEliminatedEvent
                            {
                                GameId = game.GameId,
                                PlayerId = defender.PlayerId,
                                Reason = "Commander damage"
                            });
                        }
                    }

                    // Lifelink
                    if (attacker.HasKeyword(Keyword.Lifelink))
                        activePlayer.Life += attacker.Power;

                    // Trample with no blockers — full damage to player (already handled above)
                }
            }
            else
            {
                // Blocked — mutual damage
                int attackerDamageRemaining = attacker.Power;

                foreach (var block in blocks)
                {
                    var blocker = FindCreatureOnBattlefield(game, block.BlockerId);
                    if (blocker == null) continue;

                    // Blocker deals damage to attacker
                    attacker.DamageMarked += blocker.Power;

                    // Attacker deals damage to blocker
                    int damageToBlocker = Math.Min(attackerDamageRemaining, blocker.Toughness - blocker.DamageMarked);

                    if (attacker.HasKeyword(Keyword.Deathtouch))
                        damageToBlocker = Math.Min(attackerDamageRemaining, 1); // 1 is enough with deathtouch

                    blocker.DamageMarked += damageToBlocker;
                    attackerDamageRemaining -= damageToBlocker;
                }

                // Trample: excess damage to defending player
                if (attacker.HasKeyword(Keyword.Trample) && attackerDamageRemaining > 0)
                {
                    var defender = game.GetPlayer(atk.DefendingPlayerId);
                    if (defender != null)
                        defender.Life -= attackerDamageRemaining;
                }

                // Lifelink
                if (attacker.HasKeyword(Keyword.Lifelink))
                    activePlayer.Life += attacker.Power;
            }
        }

        // Clear combat state
        game.CombatAttackers.Clear();
        game.CombatBlockers.Clear();
    }

    private static Permanent? FindCreatureOnBattlefield(GameState game, string permanentId)
    {
        return game.Players
            .SelectMany(p => p.Battlefield)
            .FirstOrDefault(p => p.InstanceId == permanentId);
    }
}
