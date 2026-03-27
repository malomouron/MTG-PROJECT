# Roadmap

## V0.1 — Fondations (MVP technique)

Objectif : prouver que l'architecture fonctionne.

- [ ] Structure du projet C# (solution .sln, projets server/client/shared)
- [ ] Modèle de données de base (GameState, Player, Card)
- [ ] Parsing de cartes JSON simples
- [ ] Serveur WebSocket minimal (connexion/déconnexion)
- [ ] Client minimal (connexion au serveur, affichage console)
- [ ] Boucle de jeu basique (tour par tour, phases simplifiées)

## V0.2 — Jeu minimal fonctionnel

Objectif : pouvoir jouer une partie 1v1 très basique.

- [ ] Authentification (comptes, connexion)
- [ ] Lobby (créer/rejoindre une partie)
- [ ] Pioche, main, terrain, mana
- [ ] Jouer des créatures simples
- [ ] Phase de combat basique (attaque/blocage/dégâts)
- [ ] Condition de victoire (PV à 0)
- [ ] Client 2D basique (plateau, cartes, drag & drop)

## V0.3 — Sorts et effets

Objectif : les cartes ont des effets qui s'appliquent automatiquement.

- [ ] Stack de sorts (Instant, Sorcery)
- [ ] Système d'effets V1 (dealDamage, gainLife, drawCard, destroy, tap/untap)
- [ ] Triggered abilities (onEnterBattlefield, onDeath)
- [ ] Système d'événements (EventBus)
- [ ] Validation des cibles
- [ ] Priorité simplifiée

## V0.4 — Commander

Objectif : supporter le format Commander.

- [ ] Zone de commandement
- [ ] Taxe de commandant
- [ ] Commander damage (21 dégâts)
- [ ] Identité de couleur
- [ ] Support 2-6 joueurs (Commander = 4 par défaut)
- [ ] Deck validation (100 cartes, 1 commandant, pas de doublons sauf terrains de base)

## V0.5 — Import de decks et cartes

Objectif : pouvoir importer des decks existants et étendre la base de cartes.

- [ ] Import format texte standard (Moxfield, Archidekt)
- [ ] Index des cartes en base SQL
- [ ] Validation du deck avec les cartes disponibles
- [ ] Premières cartes core implémentées (~50-100)
- [ ] Documentation pour contributeurs de cartes

## V0.6 — Mods

Objectif : permettre l'ajout de cartes via mods.

- [ ] ModLoader (chargement au démarrage)
- [ ] ModValidator (validation des mods)
- [ ] ModRegistry (registre des mods actifs)
- [ ] Format manifest.json
- [ ] Serveur officiel vanilla vs serveurs moddés
- [ ] Documentation pour créateurs de mods

## V0.7 — Social et statistiques

Objectif : persistance et social.

- [ ] Historique des parties (base SQL)
- [ ] Statistiques joueur (parties jouées, gagnées, commandant favori)
- [ ] Chat texte en jeu
- [ ] Liste d'amis
- [ ] Decks sauvegardés en compte

## V1.0 — Release communautaire

Objectif : version jouable et partageable.

- [ ] Serveur officiel déployé
- [ ] Client installable
- [ ] +100 cartes core fonctionnelles
- [ ] Documentation complète
- [ ] Guide de contribution
- [ ] Bug fixes, stabilité, performance

---

## V2+ (futur)

- Stack MTG complète (priorités, split second, etc.)
- Chat vocal
- DSL custom pour définition d'effets
- Effets continus et remplacement
- Keywords avancés (double strike, indestructible, hexproof...)
- Scripting contrôlé pour mods
- Spectateur mode
- Replay de parties
- Interface UI améliorée
