# 5. Installation et tests sous Windows

## 5.1 Prérequis

1. **Git pour Windows** + **Git LFS** (`git lfs install`).
2. **Unity Hub** puis **Unity 6 LTS (6000.0.x)** avec les modules :
   - *Windows Build Support (IL2CPP)* (facultatif, pour un .exe de test)
   - **iOS Build Support** (permet d'exporter le projet Xcode depuis Windows)
3. Un éditeur C# : Visual Studio 2022 (charge de travail « Développement de jeux avec Unity ») ou JetBrains Rider.
4. **.NET SDK 8** (pour lancer les tests du coeur logique hors Unity).
5. Claude Code.

> La version d'Unity indiquée dans `Game/ProjectSettings/ProjectVersion.txt` est une 6000.0.x LTS. Si Unity Hub propose une version 6000.0 plus récente, acceptez la mise à niveau puis commitez le fichier mis à jour (la CI lit cette version).

## 5.2 Premier lancement

```bash
git clone https://github.com/ougot0/jeu-ihone.git
```

1. Unity Hub → **Add** → **Add project from disk** → sélectionner le dossier **`jeu-ihone/Game`**.
2. Ouvrir le projet (le premier import dure plusieurs minutes : téléchargement des packages URP, Input System, AR Foundation…).
3. Si Unity demande d'**activer le nouveau Input System** : répondre **Yes** (redémarrage de l'éditeur). Le jeu fonctionne aussi avec l'ancien système.
4. La configuration automatique se lance à la première ouverture (sinon : menu **Arsenal > Configurer le projet**). Elle crée le pipeline URP, les matériaux, les réglages iOS et la scène `Assets/_Project/Scenes/Arsenal.unity`.
5. Ouvrir la scène **Arsenal** et appuyer sur **Play**.
6. Commiter les fichiers générés (`.meta`, `Settings/`, `Scenes/`, `Resources/Materials/`, `ProjectSettings/`).

## 5.3 Procédure de test de la Phase 1

| # | Action | Résultat attendu |
|---|---|---|
| 1 | Play | L'armurerie apparaît, le VK-9 tourne sur le piédestal, message « Bienvenue ». Crédits : 1 500 cr, niveau 1. |
| 2 | Clic gauche + glisser sur l'arme | Rotation avec inertie ; molette = zoom ; double-clic = recentrage ; après 4 s sans interaction, rotation lente. |
| 3 | Cliquer « Talon AR-7 » dans le catalogue | Animation de changement ; fiche technique ; repères clairs = stats de l'arme équipée ; bouton « Acheter » désactivé, « Niveau 4 requis ». |
| 4 | Menu **Arsenal > Debug > +1 niveau** (×3) puis **+5 000 crédits** | Message de montée de niveau ; le bouton « Acheter — 5 500 cr » devient actif. |
| 5 | Acheter | Crédits débités, statut « POSSÉDÉE », l'arme s'allume sur le mur de collection (témoin vert). |
| 6 | « Équiper » | Statut « ÉQUIPÉE » dans le catalogue. |
| 7 | Quitter Play puis relancer | Crédits, armes et arme équipée sont conservés (sauvegarde). |
| 8 | Réglages → modifier gyroscope/qualité/volume → relancer | Valeurs conservées. |
| 9 | Réglages → Réinitialiser (2 clics) | Retour au profil de départ. |
| 10 | Game view en résolution iPhone (ex. 2556×1179) | Interface lisible, rien sous l'encoche (activer *Simulator* : Window > General > Device Simulator). |

Tests automatisés du coeur logique (sans Unity) :

```bash
dotnet test Tests/Arsenal.Core.Tests
```

## 5.4 Tester l'AR sous Windows (à partir de la Phase 5)

- **XR Simulation** (AR Foundation) : *Edit > Project Settings > XR Plug-in Management > onglet PC > cocher XR Simulation*. Une pièce virtuelle simule caméra, plans détectés et déplacements dans l'éditeur.
- Le jeu fournira en plus un mode « AR simulée » qui réutilise la logique de placement sur un sol virtuel, utilisable sans aucun matériel.
- La validation finale (vraie caméra, suivi, éclairage) se fait sur iPhone via TestFlight.

## 5.5 Travailler avec Claude Code

- Décrire la phase/tâche, puis demander des commits sur une branche ; la CI vérifie les tests.
- En cas d'erreur Unity : copier le message de la console (avec le chemin du fichier et la ligne).
- Équilibrage : modifier les JSON de `Resources/Data`, relancer Play ; `dotnet test` valide la cohérence.
