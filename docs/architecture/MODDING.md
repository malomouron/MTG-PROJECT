# Système de mods

## Concept

Le projet adopte une architecture **type Minecraft** :

- Un **serveur officiel (vanilla)** est hébergé avec uniquement les cartes du repository principal
- N'importe qui peut **héberger son propre serveur** et y ajouter des mods
- Les mods sont **partagés** au sein de la communauté

```
                    ┌──────────────────────┐
                    │   Serveur officiel    │
                    │   (vanilla)           │
                    │   Cards: core only    │
     Joueurs ──────►│                      │
                    └──────────────────────┘

                    ┌──────────────────────┐
                    │   Serveur privé      │
                    │   (moddé)            │
                    │   Cards: core + mods │
     Amis ─────────►│                      │
                    └──────────────────────┘
```

## Deux sources de cartes

### 1. Core (repository GitHub)

- Cartes implémentées et validées par l'équipe / les contributeurs
- Ajoutées via **Pull Requests** sur le repository
- Revue de code obligatoire
- Disponibles sur **tous les serveurs** (vanilla + moddés)
- Stockées dans `cards/`

### 2. Mods (plugins serveur)

- Cartes et effets custom créés par la communauté
- Chargés **au démarrage du serveur**
- Disponibles **uniquement** sur les serveurs qui les ont installés
- Stockés dans `mods/`

## Format d'un mod

Chaque mod est un **dossier** contenant des fichiers JSON :

```
mods/
└── my-custom-cards/
    ├── manifest.json      # Métadonnées du mod
    ├── cards.json          # Définitions de cartes
    └── effects.json        # Effets custom (optionnel)
```

### manifest.json

```json
{
  "id": "my-custom-cards",
  "name": "My Custom Cards",
  "version": "1.0.0",
  "author": "username",
  "description": "A set of custom cards for my playgroup",
  "compatibleEngineVersion": ">=0.1.0"
}
```

### cards.json

```json
[
  {
    "id": "mod_fire_elemental",
    "name": "Fire Elemental",
    "type": "creature",
    "cost": "3RR",
    "power": 5,
    "toughness": 4,
    "effects": [
      {
        "trigger": "onEnterBattlefield",
        "action": "dealDamage",
        "target": "anyCreature",
        "value": 2
      }
    ]
  }
]
```

## Cycle de vie d'un mod

```
1. Développeur crée le dossier du mod
2. Place le dossier dans mods/
3. Démarre (ou redémarre) le serveur
4. Le ModLoader lit mods/
5. Le ModValidator vérifie la structure et la compatibilité
6. Les cartes sont ajoutées au ModRegistry
7. Le CardEngine peut maintenant résoudre les cartes moddées
```

## Sécurité

### V1 — Mode SAFE (recommandé)

Les mods sont **uniquement des données** (JSON). Aucun code n'est exécuté.

Avantages :
- Impossible d'exécuter du code malveillant
- Pas besoin de sandboxing
- Simple à valider et à auditer

Limitations :
- Les effets sont limités à ceux implémentés dans le moteur
- Impossible de créer des mécaniques totalement nouvelles

### V2+ — Mode SCRIPT (futur, dangereux)

Les mods pourraient contenir du **code C# exécuté côté serveur**.

Risques :
- Exécution de code arbitraire
- Nécessite un système de sandboxing strict
- Audit de sécurité obligatoire

> **Décision** : Commencer en mode SAFE. Le mode SCRIPT sera envisagé uniquement quand le moteur sera suffisamment mature.

## Validation d'un mod

Le `ModValidator` vérifie à chaque chargement :

| Vérification              | Description                                        |
|---------------------------|----------------------------------------------------|
| Structure valide          | Le manifest.json existe et est bien formé          |
| Compatibilité version     | Le mod est compatible avec la version du moteur    |
| IDs uniques               | Aucun ID de carte n'entre en conflit avec le core  |
| JSON valide               | Tous les fichiers JSON sont syntaxiquement corrects|
| Types d'effets valides    | Les effets référencent des actions connues du moteur|
| Pas de surcharge core     | Un mod ne peut pas redéfinir une carte core        |
