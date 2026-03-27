using MtgEngine.Application.GameEngine;
using MtgEngine.Domain.Entities;
using MtgEngine.Shared.Enums;
using MtgEngine.Shared.Models;

namespace MtgEngine.Tests.GameEngine;

public class CombatManagerTests
{
    private readonly EventBus _eventBus = new();
    private readonly CombatManager _combatManager;

    public CombatManagerTests()
    {
        _combatManager = new CombatManager(_eventBus);
    }

    private static GameState CreateCombatGame()
    {
        GameState game = new GameState
        {
            GameName = "Test",
            Status = GameStatus.InProgress,
            TurnNumber = 2,
            ActivePlayerIndex = 0,
            CurrentPhase = Phase.DeclareAttackers
        };

        game.Players.Add(new PlayerState("p1", "Player 1", 40));
        game.Players.Add(new PlayerState("p2", "Player 2", 40));
        return game;
    }

    private static Permanent AddCreature(PlayerState player, string name, int power, int toughness,
        int turnEntered = 1, List<Keyword>? keywords = null)
    {
        CardDefinition def = new CardDefinition
        {
            Id = name.ToLowerInvariant().Replace(' ', '_'),
            Name = name,
            Type = CardType.Creature,
            Power = power,
            Toughness = toughness,
            Keywords = keywords ?? []
        };
        Permanent perm = new Permanent(new CardInstance(def, player.PlayerId), turnEntered)
        {
            HasSummoningSickness = false
        };
        player.Battlefield.Add(perm);
        return perm;
    }

    [Fact]
    public void DeclareAttackers_ValidAttacker_Succeeds()
    {
        GameState game = CreateCombatGame();
        Permanent attacker = AddCreature(game.Players[0], "Bear", 2, 2);

        bool result = _combatManager.DeclareAttackers(game, "p1", [
            new CombatAssignment
            {
                AttackerId = attacker.InstanceId,
                AttackerControllerId = "p1",
                DefendingPlayerId = "p2"
            }
        ]);

        Assert.True(result);
        _ = Assert.Single(game.CombatAttackers);
    }

    [Fact]
    public void DeclareAttackers_TappedCreature_Fails()
    {
        GameState game = CreateCombatGame();
        Permanent attacker = AddCreature(game.Players[0], "Bear", 2, 2);
        attacker.IsTapped = true;

        bool result = _combatManager.DeclareAttackers(game, "p1", [
            new CombatAssignment
            {
                AttackerId = attacker.InstanceId,
                AttackerControllerId = "p1",
                DefendingPlayerId = "p2"
            }
        ]);

        Assert.False(result);
    }

    [Fact]
    public void DeclareAttackers_SummoningSick_Fails()
    {
        GameState game = CreateCombatGame();
        CardDefinition def = new CardDefinition
        {
            Id = "bear",
            Name = "Bear",
            Type = CardType.Creature,
            Power = 2,
            Toughness = 2
        };
        Permanent perm = new Permanent(new CardInstance(def, "p1"), game.TurnNumber); // entered this turn
        game.Players[0].Battlefield.Add(perm);

        bool result = _combatManager.DeclareAttackers(game, "p1", [
            new CombatAssignment
            {
                AttackerId = perm.InstanceId,
                AttackerControllerId = "p1",
                DefendingPlayerId = "p2"
            }
        ]);

        Assert.False(result);
    }

    [Fact]
    public void DeclareAttackers_HasteIgnoresSummoningSickness()
    {
        GameState game = CreateCombatGame();
        CardDefinition def = new CardDefinition
        {
            Id = "haste_creature",
            Name = "Hasty",
            Type = CardType.Creature,
            Power = 3,
            Toughness = 2,
            Keywords = [Keyword.Haste]
        };
        Permanent perm = new Permanent(new CardInstance(def, "p1"), game.TurnNumber);
        game.Players[0].Battlefield.Add(perm);

        bool result = _combatManager.DeclareAttackers(game, "p1", [
            new CombatAssignment
            {
                AttackerId = perm.InstanceId,
                AttackerControllerId = "p1",
                DefendingPlayerId = "p2"
            }
        ]);

        Assert.True(result);
    }

    [Fact]
    public void DeclareAttackers_TapsAttackers()
    {
        GameState game = CreateCombatGame();
        Permanent attacker = AddCreature(game.Players[0], "Bear", 2, 2);

        _ = _combatManager.DeclareAttackers(game, "p1", [
            new CombatAssignment
            {
                AttackerId = attacker.InstanceId,
                AttackerControllerId = "p1",
                DefendingPlayerId = "p2"
            }
        ]);

        Assert.True(attacker.IsTapped);
    }

    [Fact]
    public void DeclareAttackers_VigilanceDoesNotTap()
    {
        GameState game = CreateCombatGame();
        Permanent attacker = AddCreature(game.Players[0], "Angel", 4, 4, 1, [Keyword.Vigilance]);

        _ = _combatManager.DeclareAttackers(game, "p1", [
            new CombatAssignment
            {
                AttackerId = attacker.InstanceId,
                AttackerControllerId = "p1",
                DefendingPlayerId = "p2"
            }
        ]);

        Assert.False(attacker.IsTapped);
    }

    [Fact]
    public void ResolveCombatDamage_UnblockedCreature_DamagesPlayer()
    {
        GameState game = CreateCombatGame();
        Permanent attacker = AddCreature(game.Players[0], "Bear", 2, 2);

        game.CombatAttackers.Add(new CombatAssignment
        {
            AttackerId = attacker.InstanceId,
            AttackerControllerId = "p1",
            DefendingPlayerId = "p2"
        });

        _combatManager.ResolveCombatDamage(game);

        Assert.Equal(38, game.Players[1].Life);
    }

    [Fact]
    public void ResolveCombatDamage_BlockedCreature_MutualDamage()
    {
        GameState game = CreateCombatGame();
        Permanent attacker = AddCreature(game.Players[0], "Bear", 2, 2);
        Permanent blocker = AddCreature(game.Players[1], "Wall", 1, 3);

        game.CombatAttackers.Add(new CombatAssignment
        {
            AttackerId = attacker.InstanceId,
            AttackerControllerId = "p1",
            DefendingPlayerId = "p2"
        });
        game.CombatBlockers.Add(new CombatBlock
        {
            BlockerId = blocker.InstanceId,
            AttackerId = attacker.InstanceId
        });

        _combatManager.ResolveCombatDamage(game);

        Assert.Equal(40, game.Players[1].Life); // Blocker absorbed all damage
        Assert.Equal(1, attacker.DamageMarked); // Blocker dealt 1 to attacker
        Assert.Equal(2, blocker.DamageMarked); // Attacker dealt 2 to blocker
    }

    [Fact]
    public void ResolveCombatDamage_Trample_ExcessDamageGoesToPlayer()
    {
        GameState game = CreateCombatGame();
        Permanent attacker = AddCreature(game.Players[0], "Trampler", 6, 6, 1, [Keyword.Trample]);
        Permanent blocker = AddCreature(game.Players[1], "Chump", 1, 1);

        game.CombatAttackers.Add(new CombatAssignment
        {
            AttackerId = attacker.InstanceId,
            AttackerControllerId = "p1",
            DefendingPlayerId = "p2"
        });
        game.CombatBlockers.Add(new CombatBlock
        {
            BlockerId = blocker.InstanceId,
            AttackerId = attacker.InstanceId
        });

        _combatManager.ResolveCombatDamage(game);

        // Deals 1 to blocker (toughness 1), remaining 5 tramples to player
        Assert.True(game.Players[1].Life < 40);
    }

    [Fact]
    public void ResolveCombatDamage_Lifelink_GainsLife()
    {
        GameState game = CreateCombatGame();
        Permanent attacker = AddCreature(game.Players[0], "Lifelinker", 3, 3, 1, [Keyword.Lifelink]);

        game.CombatAttackers.Add(new CombatAssignment
        {
            AttackerId = attacker.InstanceId,
            AttackerControllerId = "p1",
            DefendingPlayerId = "p2"
        });

        _combatManager.ResolveCombatDamage(game);

        Assert.Equal(43, game.Players[0].Life); // Gained 3 from lifelink
        Assert.Equal(37, game.Players[1].Life); // Took 3 damage
    }
}
