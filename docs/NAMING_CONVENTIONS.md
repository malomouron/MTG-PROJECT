# Naming Conventions

Conventions uniformes pour tout le projet.

## Dossiers et fichiers

| Élément                    | Convention      | Exemple                       |
|----------------------------|-----------------|-------------------------------|
| Dossiers (repo)            | `kebab-case`    | `user-stories/`, `game-engine/` |
| Fichiers source C#         | `PascalCase`    | `GameState.cs`, `CardEngine.cs` |
| Fichiers JSON (cartes)     | `snake_case`    | `lightning_bolt.json`         |
| Fichiers config / docs     | `UPPER_CASE` ou `kebab-case` | `README.md`, `CONTRIBUTING.md` |
| Namespaces C#              | `PascalCase`    | `MtgEngine.Server.GameEngine` |

## Code C#

| Élément          | Convention      | Exemple                  |
|------------------|-----------------|--------------------------|
| Classes          | `PascalCase`    | `GameState`              |
| Interfaces       | `IPascalCase`   | `ICardEffect`            |
| Méthodes         | `PascalCase`    | `ResolveEffect()`        |
| Propriétés       | `PascalCase`    | `PlayerHealth`           |
| Variables locales| `camelCase`     | `currentPlayer`          |
| Champs privés    | `_camelCase`    | `_gameState`             |
| Constantes       | `PascalCase`    | `MaxPlayers`             |
| Enums            | `PascalCase`    | `CardType.Creature`      |

## JSON (cartes & mods)

| Élément          | Convention      | Exemple                  |
|------------------|-----------------|--------------------------|
| Clés JSON        | `camelCase`     | `"manaCost"`, `"cardType"` |
| IDs de cartes    | `snake_case`    | `"lightning_bolt"`       |
| Noms de fichiers | `snake_case`    | `lightning_bolt.json`    |

## Git

| Élément          | Convention           | Exemple                        |
|------------------|----------------------|--------------------------------|
| Branches         | `kebab-case`         | `feature/deck-import`          |
| Commits          | Conventional Commits | `feat: add deck import parser` |
| Tags             | Semantic Versioning  | `v0.1.0`                       |

### Conventional Commits

Format : `type: description`

| Type       | Usage                          |
|------------|--------------------------------|
| `feat`     | Nouvelle fonctionnalité        |
| `fix`      | Correction de bug              |
| `docs`     | Documentation uniquement       |
| `refactor` | Refactoring sans changement fonctionnel |
| `test`     | Ajout ou modification de tests |
| `chore`    | Maintenance, config, tooling   |

### Branches

Format : `type/description-courte`

| Type        | Usage                    |
|-------------|--------------------------|
| `feature/`  | Nouvelle fonctionnalité  |
| `fix/`      | Correction de bug        |
| `docs/`     | Documentation            |
| `refactor/` | Refactoring              |
