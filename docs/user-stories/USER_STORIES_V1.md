# User Stories — V1

Chaque US référence les règles métier (BR-XXX) définies dans [BUSINESS_RULES.md](../architecture/BUSINESS_RULES.md).

---

## US-001 — Créer un compte

**En tant que** joueur
**Je veux** créer un compte
**Afin de** sauvegarder mes parties et mes statistiques

### Acceptance Criteria

- [ ] Le joueur peut s'inscrire avec un nom d'utilisateur, un email et un mot de passe
- [ ] Le mot de passe est stocké de manière sécurisée (hash + salt)
- [ ] L'email doit être unique dans la base
- [ ] Le nom d'utilisateur doit être unique
- [ ] Un message d'erreur clair est affiché si l'email ou le pseudo est déjà pris
- [ ] Après inscription, le joueur est automatiquement connecté

### Règles métier associées

- BR-010 : Un joueur doit avoir un compte authentifié pour jouer

### Notes techniques

- Table SQL `Users` (id, username, email, password_hash, created_at)
- Hash : bcrypt ou Argon2
- Validation côté serveur (jamais faire confiance au client)

---

## US-002 — Se connecter à son compte

**En tant que** joueur
**Je veux** me connecter à mon compte existant
**Afin d'** accéder à mes decks et mon historique

### Acceptance Criteria

- [ ] Le joueur peut se connecter avec email + mot de passe
- [ ] Un token de session est renvoyé après connexion réussie
- [ ] Le token est utilisé pour authentifier les requêtes WebSocket
- [ ] Message d'erreur clair si identifiants incorrects
- [ ] Le joueur reste connecté tant que le client est ouvert

### Notes techniques

- JWT ou token de session
- Le token est envoyé dans le handshake WebSocket

---

## US-003 — Rejoindre le lobby

**En tant que** joueur connecté
**Je veux** voir la liste des parties disponibles
**Afin de** choisir une partie à rejoindre

### Acceptance Criteria

- [ ] Le lobby affiche les parties en attente de joueurs
- [ ] Chaque partie affiche : nom, nombre de joueurs (actuel/max), créateur
- [ ] Le joueur peut rafraîchir la liste
- [ ] Les parties pleines ou en cours ne sont pas rejoignables

### Notes techniques

- Le lobby est une vue distincte dans le client
- WebSocket envoie les mises à jour du lobby en temps réel

---

## US-004 — Créer une partie

**En tant que** joueur connecté
**Je veux** créer une nouvelle partie
**Afin d'** inviter mes amis à jouer

### Acceptance Criteria

- [ ] Le joueur peut créer une partie avec un nom
- [ ] Le joueur définit le nombre de places (2 à 6)
- [ ] La partie est visible dans le lobby
- [ ] Le créateur est automatiquement ajouté comme premier joueur
- [ ] Le créateur peut lancer la partie quand au moins 2 joueurs sont présents

### Règles métier associées

- BR-001 : Une partie contient entre 2 et 6 joueurs

---

## US-005 — Rejoindre une partie

**En tant que** joueur connecté
**Je veux** rejoindre une partie existante
**Afin de** jouer avec d'autres joueurs

### Acceptance Criteria

- [ ] Le joueur sélectionne une partie dans le lobby
- [ ] Le joueur doit avoir un deck valide sélectionné pour rejoindre
- [ ] Le serveur vérifie qu'il reste de la place
- [ ] Tous les joueurs de la partie sont notifiés de l'arrivée
- [ ] Le joueur voit la liste des joueurs en attente

### Règles métier associées

- BR-001, BR-012

---

## US-006 — Importer un deck

**En tant que** joueur
**Je veux** importer un deck depuis un format texte standard
**Afin de** pouvoir jouer rapidement avec mon deck existant

### Acceptance Criteria

- [ ] Le joueur peut coller une liste de deck au format texte (1 ligne = 1 carte)
- [ ] Le format supporte : `1 Lightning Bolt` ou `1x Lightning Bolt`
- [ ] Le commandant est identifié (section `COMMANDER` ou marqueur spécial)
- [ ] Le parser valide que chaque carte existe dans la base de cartes
- [ ] Le deck est validé : 100 cartes, pas de doublons (sauf terrains de base), identité de couleur
- [ ] Un rapport d'erreurs liste les cartes non trouvées ou les violations de règles
- [ ] Le deck validé est sauvegardé en base

### Règles métier associées

- BR-012, BR-030, BR-035

### Formats supportés (V1)

```
// Format simple
1 Sol Ring
1 Lightning Bolt
1 Command Tower
...

// Avec section commandant
COMMANDER
1 Atraxa, Praetors' Voice

DECK
1 Sol Ring
1 Lightning Bolt
...
```

---

## US-007 — Jouer une carte depuis la main

**En tant que** joueur (pendant son tour)
**Je veux** jouer une carte depuis ma main
**Afin d'** affecter l'état de la partie

### Acceptance Criteria

- [ ] Le joueur sélectionne une carte dans sa main
- [ ] Le serveur vérifie que le joueur a le mana suffisant
- [ ] Le serveur vérifie que la carte peut être jouée dans la phase actuelle (Sorcery = Main phase seulement)
- [ ] Si la carte nécessite une cible, le joueur doit la sélectionner
- [ ] Le mana est déduit du pool du joueur
- [ ] La carte est placée sur la stack (ou directement sur le champ de bataille pour les terrains)
- [ ] Tous les joueurs sont notifiés

### Règles métier associées

- BR-024, BR-025, BR-027, BR-028, BR-080

---

## US-008 — Poser un terrain

**En tant que** joueur (pendant son tour)
**Je veux** poser un terrain depuis ma main
**Afin de** générer du mana pour jouer mes sorts

### Acceptance Criteria

- [ ] Le joueur peut poser un terrain pendant sa Main Phase
- [ ] Maximum 1 terrain par tour (sauf effets contraires)
- [ ] Le terrain arrive sur le champ de bataille directement (pas sur la stack)
- [ ] Le terrain peut être engagé pour produire du mana dès ce tour

### Règles métier associées

- BR-016, BR-025, BR-026, BR-042

---

## US-009 — Phase de combat

**En tant que** joueur actif
**Je veux** attaquer avec mes créatures
**Afin d'** infliger des dégâts à mes adversaires

### Acceptance Criteria

- [ ] Le joueur actif peut déclarer des attaquants parmi ses créatures non engagées (sans maladie d'invocation)
- [ ] Les créatures attaquantes sont automatiquement engagées
- [ ] En Commander, le joueur choisit quel adversaire chaque créature attaque
- [ ] Les défenseurs peuvent déclarer des bloqueurs parmi leurs créatures non engagées
- [ ] Les dégâts sont calculés et appliqués automatiquement
- [ ] Les créatures dont les dégâts ≥ toughness sont détruites
- [ ] Les dégâts non bloqués sont appliqués au joueur ciblé
- [ ] Les dégâts de commandant sont comptabilisés séparément

### Règles métier associées

- BR-050 à BR-057, BR-018

---

## US-010 — Résolution automatique des effets

**En tant que** système
**Je veux** résoudre automatiquement les effets des cartes
**Afin de** appliquer les règles MTG sans intervention manuelle

### Acceptance Criteria

- [ ] Quand un sort se résout depuis la stack, ses effets s'appliquent automatiquement
- [ ] Les triggered abilities (onEnterBattlefield, onDeath...) se déclenchent automatiquement
- [ ] Les effets qui nécessitent une cible demandent l'input du joueur concerné
- [ ] Le GameState est mis à jour après chaque effet
- [ ] Tous les joueurs reçoivent la mise à jour d'état
- [ ] Les effets en cascade sont gérés (un effet peut déclencher un autre effet)

### Règles métier associées

- BR-060 à BR-063

---

## US-011 — Gestion de la stack (pile de sorts)

**En tant que** joueur
**Je veux** pouvoir répondre aux sorts de mes adversaires
**Afin de** interagir avec leurs actions

### Acceptance Criteria

- [ ] Quand un sort est joué, il est placé sur la stack (visible par tous)
- [ ] Chaque joueur a la priorité pour répondre (jouer un Instant, activer une capacité)
- [ ] Quand tous les joueurs passent, le sort au sommet de la stack se résout
- [ ] La stack se résout en LIFO
- [ ] L'UI affiche clairement la stack et indique qui a la priorité

### Règles métier associées

- BR-080 à BR-084

---

## US-012 — Système de mods

**En tant qu'** administrateur de serveur
**Je veux** charger des mods sur mon serveur
**Afin d'** ajouter des cartes personnalisées pour mon groupe de jeu

### Acceptance Criteria

- [ ] Le serveur détecte les mods dans le dossier `mods/`
- [ ] Chaque mod est validé au chargement (structure, compatibilité, IDs uniques)
- [ ] Les cartes des mods sont ajoutées au pool de cartes disponibles
- [ ] Un mod invalide est ignoré avec un message d'erreur clair (ne bloque pas le serveur)
- [ ] Les joueurs voient quels mods sont actifs sur le serveur
- [ ] Un mod ne peut pas écraser une carte core

### Règles métier associées

- Voir [MODDING.md](../architecture/MODDING.md)

---

## US-013 — Lancer son commandant

**En tant que** joueur
**Je veux** lancer mon commandant depuis la zone de commandement
**Afin de** mettre ma carte clé en jeu

### Acceptance Criteria

- [ ] Le commandant est visible dans la zone de commandement
- [ ] Le joueur peut le lancer en payant son coût + taxe de commandant
- [ ] La taxe augmente de {2} à chaque relance
- [ ] Quand le commandant meurt ou est exilé, le joueur peut le renvoyer en zone de commandement
- [ ] Le nombre de relances est affiché dans l'UI

### Règles métier associées

- BR-030 à BR-034

---

## US-014 — Chat texte en jeu

**En tant que** joueur en partie
**Je veux** envoyer des messages texte aux autres joueurs
**Afin de** communiquer pendant la partie

### Acceptance Criteria

- [ ] Un panneau de chat est visible pendant la partie
- [ ] Les messages sont envoyés à tous les joueurs de la partie
- [ ] Chaque message affiche le pseudo de l'expéditeur et l'heure
- [ ] Les messages sont transmis via WebSocket

---

## US-015 — Fin de partie et statistiques

**En tant que** joueur
**Je veux** que les résultats de la partie soient enregistrés
**Afin de** consulter mes statistiques

### Acceptance Criteria

- [ ] La partie se termine quand il ne reste qu'un joueur en vie
- [ ] Le gagnant est affiché à tous les joueurs
- [ ] La partie est enregistrée en base (joueurs, gagnant, durée, commandants utilisés)
- [ ] Les statistiques du joueur sont mises à jour (parties jouées, victoires)
- [ ] Le joueur peut consulter son historique depuis le client

### Règles métier associées

- BR-005, BR-017, BR-018, BR-019
