# Vision du projet

## Résumé

MTG Commander Engine est une **plateforme de jeu MTG** spécialisée dans le format **Commander (EDH)**, avec automatisation complète des règles de jeu, support multijoueur et un écosystème moddable.

## Problème

- MTG Arena ne supporte pas le format Commander
- Les solutions existantes (Tabletop Simulator, Cockatrice) sont manuelles : les joueurs doivent gérer les règles eux-mêmes
- Aucune solution ne combine automatisation + Commander + modding

## Solution

Un moteur de jeu MTG qui :

1. **Automatise les règles** — les effets des cartes s'appliquent automatiquement sur le champ de bataille
2. **Supporte le Commander** — format multijoueur 2 à 6 joueurs
3. **Est moddable** — architecture serveur/client type Minecraft permettant des serveurs personnalisés

## Principes fondamentaux

### Le serveur est la source de vérité

Le serveur contrôle 100% de la logique de jeu. Le client ne fait qu'afficher l'état et transmettre les actions du joueur. Cela garantit :

- **Anti-triche** — aucune logique sensible côté client
- **Synchronisation fiable** — un seul état de jeu fait référence
- **Cohérence** — tous les joueurs voient le même état

### Le joueur garde le contrôle

Malgré l'automatisation, le joueur :

- Choisit ses cibles
- Sélectionne ses attaquants et bloqueurs
- Décide de l'ordre de résolution quand applicable

Le moteur ne prend pas de décisions à la place du joueur.

### Implémentation progressive des cartes

MTG comprend plus de 25 000 cartes avec des règles extrêmement complexes. L'approche est :

1. Commencer avec un ensemble de cartes simples (créatures, terrains, sorts basiques)
2. Enrichir progressivement via les contributeurs GitHub
3. Permettre l'ajout de cartes via le système de mods

### Communauté au centre

Le projet repose sur deux canaux de contribution :

- **Core** — cartes ajoutées au repo via Pull Requests GitHub
- **Mods** — packages de cartes/effets custom chargés par les serveurs

## Public cible

- Joueurs MTG Commander qui veulent jouer en ligne avec leurs amis
- Développeurs/contributeurs passionnés par MTG qui veulent implémenter des cartes
- Hébergeurs de serveurs qui veulent personnaliser l'expérience

## Ce que le projet N'EST PAS

- Un clone complet de MTG Arena (pas de matchmaking, pas de collection, pas de boutique)
- Un simulateur manuel (les règles sont automatisées)
- Une application mobile (desktop uniquement)
