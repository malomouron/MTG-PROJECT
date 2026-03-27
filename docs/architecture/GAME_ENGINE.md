# Design du moteur de jeu

## Vue d'ensemble

Le moteur de jeu est le cœur du serveur. Il est responsable de :

- Gérer l'état de chaque partie (GameState)
- Exécuter les phases de jeu (TurnManager)
- Résoudre la pile de sorts (StackManager)
- Interpréter et appliquer les effets des cartes (EffectResolver)
- Valider les actions des joueurs

## Composants principaux

```
┌────────────────────────────────────────────────┐
│                  GameEngine                     │
│                                                 │
│  ┌─────────────┐  ┌──────────────┐             │
│  │ TurnManager │  │ StackManager │             │
│  │             │  │              │             │
│  │ Phases      │  │ Push/Pop     │             │
│  │ Priority    │  │ Resolve      │             │
│  └──────┬──────┘  └──────┬───────┘             │
│         │                │                      │
│  ┌──────▼────────────────▼───────┐             │
│  │         GameState             │             │
│  │                               │             │
│  │  Players[]                    │             │
│  │  Battlefield                  │             │
│  │  Stack                        │             │
│  │  TurnOrder                    │             │
│  │  CurrentPhase                 │             │
│  └──────────────┬────────────────┘             │
│                 │                               │
│  ┌──────────────▼────────────────┐             │
│  │       CardEngine              │             │
│  │                               │             │
│  │  CardParser                   │             │
│  │  EffectResolver               │             │
│  │  TargetValidator              │             │
│  └───────────────────────────────┘             │
│                                                 │
│  ┌───────────────────────────────┐             │
│  │       EventBus                │             │
│  │                               │             │
│  │  Publish / Subscribe          │             │
│  │  Trigger chain resolution     │             │
│  └───────────────────────────────┘             │
└────────────────────────────────────────────────┘
```

## GameState

L'état complet d'une partie à un instant T :

```
GameState
├── id: string                    # ID unique de la partie
├── status: enum                  # Waiting, InProgress, Finished
├── turnOrder: Player[]           # Ordre des tours
├── activePlayerIndex: int        # Joueur actif
├── currentPhase: Phase           # Phase en cours
├── stack: StackItem[]            # Pile de sorts/capacités
├── players: PlayerState[]
│   ├── id: string
│   ├── life: int                 # Points de vie (40 en Commander)
│   ├── manaPool: ManaPool        # Mana disponible
│   ├── commanderDamage: dict     # Dégâts de commandant par source
│   ├── landPlayedThisTurn: bool
│   ├── zones:
│   │   ├── hand: Card[]
│   │   ├── library: Card[]       # Deck (cachée)
│   │   ├── graveyard: Card[]
│   │   ├── exile: Card[]
│   │   ├── battlefield: Permanent[]
│   │   └── commandZone: Card[]
└── config: GameConfig
    ├── maxPlayers: int
    ├── startingLife: int
    └── modsEnabled: string[]
```

## Système d'événements (EventBus)

Le moteur est **event-driven**. Chaque action génère des événements qui peuvent déclencher des effets en cascade.

### Événements du moteur

| Événement                | Données                              | Déclencheur                    |
|--------------------------|--------------------------------------|--------------------------------|
| `GameStarted`            | gameId, players                      | Début de partie                |
| `TurnStarted`            | playerId, turnNumber                 | Début de tour                  |
| `PhaseChanged`           | phase                                | Changement de phase            |
| `CardDrawn`              | playerId, card                       | Pioche                         |
| `CardPlayed`             | playerId, card, targets              | Joueur lance un sort           |
| `SpellResolved`          | card, effects                        | Sort résolu depuis la stack    |
| `CreatureEnteredBattlefield` | permanent                        | Créature arrive en jeu         |
| `CreatureDied`           | permanent                            | Créature détruite              |
| `DamageDealt`            | source, target, amount               | Dégâts infligés                |
| `LifeChanged`            | playerId, oldLife, newLife            | PV modifiés                    |
| `AttackersDeclared`      | attackers[]                          | Déclaration attaquants         |
| `BlockersDeclared`       | blockers[]                           | Déclaration bloqueurs          |
| `PlayerEliminated`       | playerId, reason                     | Joueur éliminé                 |
| `GameEnded`              | winnerId                             | Fin de partie                  |

### Cycle d'un événement

```
1. Action du joueur (via WebSocket)
2. Validation de l'action
3. Modification du GameState
4. Publication de l'événement sur l'EventBus
5. Les abonnés (triggered abilities) réagissent
6. Nouveaux événements potentiels (chaîne)
7. Broadcast du nouvel état aux clients
```

## CardEngine — Interprétation des cartes

### Stratégie d'évolution

| Phase   | Format        | Description                                          |
|---------|---------------|------------------------------------------------------|
| Phase 1 | JSON simple   | Effets prédéfinis, interpréteur C#                   |
| Phase 2 | DSL custom    | Langage dédié pour décrire des effets complexes      |
| Phase 3 | Scripting     | Code contrôlé côté serveur (sandboxé)                |

### Phase 1 — JSON + Interpréteur

Chaque carte est un fichier JSON. L'interpréteur C# lit les effets et les exécute via des handlers prédéfinis.

```json
{
  "id": "lightning_bolt",
  "name": "Lightning Bolt",
  "type": "instant",
  "cost": "R",
  "effects": [
    {
      "trigger": "onCast",
      "action": "dealDamage",
      "target": "any",
      "value": 3
    }
  ]
}
```

```json
{
  "id": "llanowar_elves",
  "name": "Llanowar Elves",
  "type": "creature",
  "cost": "G",
  "power": 1,
  "toughness": 1,
  "abilities": [
    {
      "type": "activated",
      "cost": "tap",
      "action": "addMana",
      "value": "G"
    }
  ]
}
```

```json
{
  "id": "mulldrifter",
  "name": "Mulldrifter",
  "type": "creature",
  "cost": "4U",
  "power": 2,
  "toughness": 2,
  "keywords": ["flying"],
  "effects": [
    {
      "trigger": "onEnterBattlefield",
      "action": "drawCard",
      "value": 2
    }
  ]
}
```

### EffectResolver — Actions disponibles (V1)

| Action            | Paramètres             | Description                   |
|-------------------|------------------------|-------------------------------|
| `dealDamage`      | target, value          | Inflige des dégâts            |
| `gainLife`        | value                  | Gain de points de vie         |
| `drawCard`        | value                  | Piocher N cartes              |
| `destroy`         | target                 | Détruire un permanent         |
| `tap`             | target                 | Engager un permanent          |
| `untap`           | target                 | Dégager un permanent          |
| `addMana`         | value (color)          | Ajouter du mana               |
| `createToken`     | tokenDef               | Créer un jeton créature       |
| `returnToHand`    | target                 | Renvoyer en main              |
| `exile`           | target                 | Exiler un permanent           |

### TargetValidator — Types de cibles

| Cible              | Description                         |
|--------------------|-------------------------------------|
| `any`              | N'importe quel joueur ou créature   |
| `anyPlayer`        | N'importe quel joueur               |
| `anyCreature`      | N'importe quelle créature           |
| `self`             | Le joueur qui lance le sort         |
| `opponent`         | Un adversaire                       |
| `selfCreature`     | Une créature que vous contrôlez     |
| `opponentCreature` | Une créature adverse                |

## TurnManager — Gestion des tours

### Flow d'un tour

```
beginTurn(activePlayer)
│
├── untapPhase()
│   └── Untap tous les permanents du joueur actif
│
├── upkeepPhase()
│   └── Résoudre les triggers "at the beginning of upkeep"
│
├── drawPhase()
│   └── Le joueur actif pioche 1 carte
│
├── mainPhase1()
│   └── Le joueur peut jouer des sorts, poser 1 terrain
│       └── Boucle: attente action joueur ou pass
│
├── combatPhase()
│   ├── beginCombat()
│   ├── declareAttackers()   ← input joueur
│   ├── declareBlockers()    ← input défenseurs
│   └── resolveDamage()
│
├── mainPhase2()
│   └── Même chose que mainPhase1
│
├── endPhase()
│   └── Résoudre les triggers "at end of turn"
│
└── cleanupPhase()
    └── Défausser jusqu'à 7 cartes en main
        Effacer les dégâts marqués sur les créatures
```

## StackManager — Pile de sorts

### Fonctionnement

```
1. Joueur A joue Lightning Bolt ciblant Joueur B
   Stack: [Lightning Bolt]

2. Joueur B a la priorité → joue Counterspell ciblant Lightning Bolt
   Stack: [Lightning Bolt, Counterspell]

3. Tous passent la priorité → résolution LIFO
   → Counterspell résout → Lightning Bolt est contrecarré
   → Stack vide
```

### V1 — Simplifications

- Priorité gérée en tour par tour (chaque joueur peut répondre)
- Pas de priorité rapide (shortcutting automatique possible en V2+)
- Les capacités déclenchées vont sur la stack dans l'ordre APNAP (Active Player, Non-Active Player)
