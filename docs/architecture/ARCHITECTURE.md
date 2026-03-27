# Architecture technique

## Vue d'ensemble

Le projet suit une architecture **client-serveur** avec une séparation stricte des responsabilités.

```
┌─────────────┐     WebSocket (JSON)     ┌─────────────────┐
│   Client    │ ◄──────────────────────► │     Serveur     │
│  (Desktop)  │                          │   (Game Engine) │
│             │  Actions du joueur ──►   │                 │
│  Affichage  │  ◄── État de jeu         │  Logique MTG    │
│  + Input    │  ◄── Événements          │  Validation     │
└─────────────┘                          │  Synchronisation│
                                         │                 │
                                         │  ┌───────────┐  │
                                         │  │    Mods    │  │
                                         │  └───────────┘  │
                                         │                 │
                                         │  ┌───────────┐  │
                                         │  │    SQL     │  │
                                         │  │   (BDD)    │  │
                                         │  └───────────┘  │
                                         └─────────────────┘
```

## Stack technique

| Composant       | Technologie           | Justification                              |
|-----------------|-----------------------|--------------------------------------------|
| Serveur         | C# / ASP.NET Core     | Performance, typage fort, écosystème riche  |
| Client          | C# / WPF ou Avalonia  | Cohérence langage, desktop natif            |
| Communication   | WebSocket             | Temps réel obligatoire pour le jeu          |
| Format messages | JSON                  | Lisible, standard, compatible mods          |
| Base de données | SQL (PostgreSQL/SQLite) | Relationnel, requêtes complexes (stats)   |
| Cartes (V1)     | JSON                  | Simple, contributeur-friendly               |
| Cartes (V2+)    | DSL custom            | Plus expressif pour les effets complexes    |

## Structure du code source

```
mtg-project/
├── src/
│   ├── server/                    # Serveur de jeu
│   │   ├── GameEngine/            # Moteur de règles MTG
│   │   │   ├── TurnManager        # Gestion des tours et phases
│   │   │   ├── StackManager       # Pile MTG (cast, résolution)
│   │   │   ├── CombatManager      # Phase de combat
│   │   │   └── PriorityManager    # Gestion des priorités joueurs
│   │   ├── GameState/             # État de la partie
│   │   │   ├── Game               # État global d'une partie
│   │   │   ├── Player             # État d'un joueur
│   │   │   ├── Zone               # Zones de jeu (main, battlefield...)
│   │   │   └── Card               # Instance de carte en jeu
│   │   ├── CardEngine/            # Interprétation des cartes
│   │   │   ├── CardParser         # Lecture JSON → objet carte
│   │   │   ├── EffectResolver     # Résolution des effets
│   │   │   └── TargetSelector     # Validation des cibles
│   │   ├── Effects/               # Système d'effets
│   │   │   ├── DealDamage
│   │   │   ├── GainLife
│   │   │   ├── DrawCard
│   │   │   ├── TapUntap
│   │   │   └── ...
│   │   ├── Networking/            # Communication réseau
│   │   │   ├── WebSocketServer    # Serveur WebSocket
│   │   │   ├── MessageHandler     # Traitement des messages
│   │   │   └── SessionManager     # Gestion des sessions joueurs
│   │   └── Mods/                  # Système de plugins
│   │       ├── ModLoader          # Chargement des mods
│   │       ├── ModValidator       # Validation des mods
│   │       └── ModRegistry        # Registre des mods actifs
│   │
│   ├── client/                    # Client desktop
│   │   ├── UI/                    # Interface graphique
│   │   │   ├── MainWindow         # Fenêtre principale
│   │   │   ├── LoginView          # Connexion / création compte
│   │   │   ├── LobbyView          # Lobby / sélection de partie
│   │   │   └── SettingsView       # Paramètres
│   │   ├── Board/                 # Plateau de jeu
│   │   │   ├── BoardView          # Vue du plateau 2D
│   │   │   ├── CardView           # Rendu d'une carte
│   │   │   ├── ZoneView           # Rendu d'une zone
│   │   │   └── DragDropHandler    # Gestion du drag & drop
│   │   └── Networking/            # Communication réseau
│   │       ├── WebSocketClient    # Connexion au serveur
│   │       ├── MessageSender      # Envoi d'actions
│   │       └── StateReceiver      # Réception état de jeu
│   │
│   └── shared/                    # Code partagé serveur/client
│       ├── Models/                # Modèles de données
│       │   ├── CardDefinition     # Définition d'une carte
│       │   ├── GameAction         # Action joueur
│       │   └── GameEvent          # Événement de jeu
│       ├── Enums/                 # Énumérations
│       │   ├── CardType           # Creature, Instant, Sorcery...
│       │   ├── Phase              # Untap, Upkeep, Draw, Main...
│       │   └── Zone               # Hand, Battlefield, Graveyard...
│       └── Protocol/              # Protocole de communication
│           ├── Messages           # Définition des messages
│           └── Serialization      # Sérialisation JSON
│
├── cards/                         # Définitions de cartes (core)
├── mods/                          # Packages de mods
├── docs/                          # Documentation
└── tools/                         # Scripts utilitaires
```

## Patterns architecturaux

### Event-Driven Architecture

Le moteur de jeu est **événementiel**. Chaque action génère des événements qui peuvent déclencher d'autres effets.

```
Joueur joue "Lightning Bolt"
  → Événement: CardPlayed
    → Validation: coût en mana suffisant ?
    → Push sur la stack
      → Résolution de la stack
        → Événement: SpellResolved
          → Effet: DealDamage(target, 3)
            → Événement: DamageDealt
              → Vérification: créature morte ? joueur à 0 PV ?
```

### State Machine (phases de jeu)

Le tour de jeu est géré par une **machine à états** :

```
┌──────────┐    ┌────────┐    ┌──────┐    ┌────────────┐    ┌──────────────┐
│  Untap   │───►│ Upkeep │───►│ Draw │───►│ Main (Pre) │───►│   Combat     │
└──────────┘    └────────┘    └──────┘    └────────────┘    │              │
                                                             │ ┌──────────┐│
                                                             │ │Declare   ││
                                                             │ │Attackers ││
                                                             │ └────┬─────┘│
                                                             │ ┌────▼─────┐│
                                                             │ │Declare   ││
                                                             │ │Blockers  ││
                                                             │ └────┬─────┘│
                                                             │ ┌────▼─────┐│
                                                             │ │ Damage   ││
                                                             │ └──────────┘│
                                                             └──────┬───────┘
                                                             ┌──────▼───────┐
                                                             │ Main (Post)  │
                                                             └──────┬───────┘
                                                             ┌──────▼───────┐
                                                             │   End Step   │
                                                             └──────┬───────┘
                                                             ┌──────▼───────┐
                                                             │   Cleanup    │
                                                             └──────────────┘
```

### Client-Server strict

```
Client                          Serveur
  │                                │
  │  ACTION: PlayCard(id, target)  │
  │ ──────────────────────────────►│
  │                                │  Validate
  │                                │  Apply effects
  │                                │  Update state
  │  EVENT: GameStateUpdated       │
  │ ◄──────────────────────────────│
  │                                │
  │  Render new state              │
  │                                │
```

Le client n'exécute **jamais** de logique de jeu. Il envoie des intentions et reçoit des résultats.

## Modèle de données (SQL)

### Tables principales

```sql
-- Comptes utilisateurs
Users (
    id, username, email, password_hash, created_at
)

-- Parties
Games (
    id, status, created_at, ended_at, winner_id
)

-- Joueurs dans une partie
GamePlayers (
    game_id, user_id, commander_id, starting_life, final_life
)

-- Historique d'actions (replay)
GameActions (
    id, game_id, turn, phase, player_id, action_type, payload, timestamp
)

-- Statistiques
PlayerStats (
    user_id, games_played, games_won, favorite_commander
)

-- Decks sauvegardés
Decks (
    id, user_id, name, commander_id, format, card_list
)
```

## Communication WebSocket

### Types de messages (client → serveur)

| Message           | Description                          |
|-------------------|--------------------------------------|
| `Connect`         | Connexion au serveur                 |
| `JoinGame`        | Rejoindre une partie                 |
| `PlayCard`        | Jouer une carte (id, cibles)         |
| `DeclareAttacker` | Déclarer un attaquant                |
| `DeclareBlocker`  | Déclarer un bloqueur                 |
| `PassPriority`    | Passer la priorité                   |
| `ActivateAbility` | Activer une capacité                 |
| `ChatMessage`     | Message texte dans le chat           |

### Types de messages (serveur → client)

| Message              | Description                       |
|----------------------|-----------------------------------|
| `GameStateUpdate`    | Mise à jour complète de l'état    |
| `CardPlayed`         | Une carte a été jouée             |
| `EffectResolved`     | Un effet a été résolu             |
| `PhaseChanged`       | Changement de phase               |
| `TurnChanged`        | Changement de tour                |
| `PlayerAction`       | Action d'un autre joueur          |
| `GameEnded`          | Fin de partie                     |
| `Error`              | Erreur (action invalide)          |
| `ChatBroadcast`      | Message de chat d'un joueur       |
