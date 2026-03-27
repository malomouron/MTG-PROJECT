# Règles métier

Ce document décrit les règles métier du projet pour la **V1**. Chaque règle est identifiée par un code unique (BR-XXX) pour faciliter le traçage dans les user stories et le code.

---

## 1. Partie (Game)

| Code    | Règle                                                                  |
|---------|------------------------------------------------------------------------|
| BR-001  | Une partie contient entre 2 et 6 joueurs                              |
| BR-002  | Une partie possède un état global (`GameState`) synchronisé en temps réel |
| BR-003  | L'ordre des tours est déterminé au début de la partie (aléatoire)     |
| BR-004  | Une partie possède une pile (stack) pour gérer les sorts et capacités  |
| BR-005  | Une partie se termine quand il ne reste qu'un seul joueur en vie      |
| BR-006  | L'état de la partie est uniquement contrôlé par le serveur            |

## 2. Joueur (Player)

| Code    | Règle                                                                  |
|---------|------------------------------------------------------------------------|
| BR-010  | Un joueur doit avoir un compte authentifié pour jouer                  |
| BR-011  | Chaque joueur commence avec 40 points de vie (format Commander)       |
| BR-012  | Un joueur possède un deck de 100 cartes (dont 1 commandant)           |
| BR-013  | Un joueur possède des zones : main (hand), bibliothèque (library), cimetière (graveyard), exil (exile), champ de bataille (battlefield) |
| BR-014  | Un joueur pioche 7 cartes au début de la partie (main initiale)       |
| BR-015  | Un joueur pioche 1 carte au début de son tour (phase Draw)            |
| BR-016  | Un joueur peut poser un terrain par tour (sauf effets contraires)     |
| BR-017  | Un joueur est éliminé quand ses points de vie tombent à 0 ou moins    |
| BR-018  | Un joueur est éliminé quand il reçoit 21 dégâts de combat d'un même commandant |
| BR-019  | Un joueur est éliminé s'il doit piocher mais que sa bibliothèque est vide |

## 3. Carte (Card)

| Code    | Règle                                                                  |
|---------|------------------------------------------------------------------------|
| BR-020  | Une carte est définie par un fichier JSON côté serveur                 |
| BR-021  | Une carte possède : nom, type, coût de mana, effets                   |
| BR-022  | Les types de cartes V1 : Creature, Instant, Sorcery, Enchantment, Artifact, Land |
| BR-023  | Une créature possède : force (power) et endurance (toughness)         |
| BR-024  | Jouer une carte nécessite de payer son coût en mana                   |
| BR-025  | Les terrains (Land) ne coûtent pas de mana à jouer                    |
| BR-026  | Les terrains produisent du mana quand ils sont engagés (tapped)       |
| BR-027  | Un sort Instant peut être joué à tout moment où le joueur a la priorité |
| BR-028  | Un sort Sorcery ne peut être joué que pendant la Main Phase du joueur actif |

## 4. Commandant (Commander)

| Code    | Règle                                                                  |
|---------|------------------------------------------------------------------------|
| BR-030  | Chaque deck doit avoir exactement 1 commandant (creature légendaire)  |
| BR-031  | Le commandant commence dans la zone de commandement (command zone)    |
| BR-032  | Le commandant peut être lancé depuis la zone de commandement          |
| BR-033  | Chaque relance du commandant coûte {2} de plus (taxe de commandant)   |
| BR-034  | Quand le commandant meurt ou est exilé, le propriétaire peut le renvoyer en zone de commandement |
| BR-035  | Les couleurs du commandant définissent l'identité de couleur du deck  |

## 5. Mana

| Code    | Règle                                                                  |
|---------|------------------------------------------------------------------------|
| BR-040  | Le mana existe en 5 couleurs : White (W), Blue (U), Black (B), Red (R), Green (G) |
| BR-041  | Le mana incolore existe : Colorless (C)                               |
| BR-042  | Le mana est produit en engageant des terrains ou certains permanents   |
| BR-043  | Le mana non dépensé disparaît à la fin de chaque phase               |
| BR-044  | Pour jouer une carte, le joueur doit avoir le mana correspondant au coût |

## 6. Combat (V1 simplifié)

| Code    | Règle                                                                  |
|---------|------------------------------------------------------------------------|
| BR-050  | Le combat se déroule pendant la phase de Combat                       |
| BR-051  | Le joueur actif déclare ses attaquants (créatures non engagées)       |
| BR-052  | Les créatures attaquantes sont engagées (tapped)                      |
| BR-053  | Les défenseurs déclarent leurs bloqueurs                              |
| BR-054  | Les dégâts sont calculés simultanément (power de l'attaquant vs toughness du bloqueur) |
| BR-055  | Une créature non bloquée inflige ses dégâts au joueur ciblé           |
| BR-056  | Une créature dont les dégâts reçus ≥ toughness est détruite           |
| BR-057  | La maladie d'invocation empêche d'attaquer le tour où une créature arrive (sauf Haste) |

## 7. Effets (V1)

| Code    | Règle                                                                  |
|---------|------------------------------------------------------------------------|
| BR-060  | Les effets supportés V1 : `dealDamage`, `gainLife`, `drawCard`, `tap`, `untap`, `destroy`, `createToken` |
| BR-061  | Un effet est déclenché par un trigger : `onCast`, `onEnterBattlefield`, `onDeath`, `onAttack` |
| BR-062  | Les effets qui nécessitent une cible demandent au joueur de choisir   |
| BR-063  | Les effets sont résolus dans l'ordre de la stack (LIFO)              |
| BR-064  | Effets **NON** supportés V1 : effets continus, replacement effects, copy effects, triggered abilities complexes |

## 8. Tour de jeu (Turn)

| Code    | Règle                                                                  |
|---------|------------------------------------------------------------------------|
| BR-070  | Un tour suit les phases : Untap → Upkeep → Draw → Main 1 → Combat → Main 2 → End → Cleanup |
| BR-071  | Phase Untap : tous les permanents du joueur actif se dégagent (untap) |
| BR-072  | Phase Draw : le joueur actif pioche 1 carte                          |
| BR-073  | Phase Main : le joueur peut jouer des sorts, poser un terrain        |
| BR-074  | Phase Combat : déclaration attaquants, bloqueurs, résolution dégâts  |
| BR-075  | Phase End : effets de fin de tour se résolvent                       |
| BR-076  | Phase Cleanup : le joueur défausse jusqu'à 7 cartes en main          |

## 9. Stack (pile) — V1 simplifié

| Code    | Règle                                                                  |
|---------|------------------------------------------------------------------------|
| BR-080  | Quand un sort est lancé, il est placé sur la stack                    |
| BR-081  | Les joueurs ont la priorité pour répondre (jouer des Instants)       |
| BR-082  | Quand tous les joueurs passent la priorité, le sort au sommet se résout |
| BR-083  | La stack se résout en **LIFO** (dernier entré, premier résolu)       |
| BR-084  | V1 : gestion simplifiée de la priorité (tour par tour des joueurs)   |

## 10. Synchronisation

| Code    | Règle                                                                  |
|---------|------------------------------------------------------------------------|
| BR-090  | Le serveur est la **seule source de vérité** de l'état de jeu        |
| BR-091  | Le client envoie des **actions** (intentions du joueur)              |
| BR-092  | Le serveur **valide** chaque action avant de l'appliquer             |
| BR-093  | Le serveur **broadcast** les changements d'état à tous les joueurs   |
| BR-094  | Toute les communications passent par **WebSocket**                   |
