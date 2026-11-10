# Arsenal — jeu 3D iOS : arsenal personnalisable, missions FPS et stand de tir en réalité mixte

*Titre provisoire.* Jeu Unity pour iPhone, développé sur **Windows** avec **Claude Code** et **GitHub**, compilé pour iOS **dans le cloud** (aucun Mac personnel).

Toutes les armes sont **fictives** (noms et designs originaux, aucune marque réelle), tous les projectiles, cibles et impacts sont **virtuels**, et la monnaie (crédits) se gagne **uniquement en jouant**.

## État actuel — Phase 1 (Fondations) livrée

| Élément | État |
|---|---|
| Coeur logique (arsenal, configurations, compatibilité, statistiques, économie, progression, sauvegarde, simulation de tir) | ✅ implémenté, **23 tests automatisés qui passent** |
| Données : 6 armes, 35 accessoires, 8 revêtements, économie (JSON éditables) | ✅ validées automatiquement |
| Armurerie 3D (décor, éclairage, présentoir tactile, mur de collection, post-traitement URP) | ✅ écrite — **à valider dans Unity** (voir limites) |
| Interface : catalogue, fiche technique, achat, équipement, configurations, réglages (gyroscope, sensibilités, qualité, volume) | ✅ écrite — à valider dans Unity |
| Configuration automatique du projet + builds iOS/Windows | ✅ écrite — à valider dans Unity |
| CI tests (.NET) + workflow de build iOS cloud | ✅ écrits — le build iOS nécessite vos secrets |
| Atelier, stand de tir, AR, missions | ⏳ phases 2 à 6 (boutons visibles mais désactivés) |

**Limite importante :** l'environnement où ce code a été écrit ne dispose pas de l'éditeur Unity. Le coeur logique est compilé et testé ; le code Unity a seulement été vérifié syntaxiquement. Ouvrez le projet, et transmettez à Claude Code toute erreur de la console Unity.

## Démarrage rapide (Windows)

1. Installer Unity Hub + **Unity 6 LTS** (modules *iOS Build Support*), Git LFS, .NET 8.
2. Unity Hub → *Add project from disk* → dossier **`Game/`**.
3. Accepter l'activation de l'Input System si proposée ; la configuration automatique crée URP, matériaux et scène.
4. Ouvrir `Assets/_Project/Scenes/Arsenal.unity` → **Play**.
5. Tests du coeur logique : `dotnet test Tests/Arsenal.Core.Tests`

Détails et procédure de test : [docs/05-installation-et-tests-windows.md](docs/05-installation-et-tests-windows.md).

## Documentation

| Document | Contenu |
|---|---|
| [01 — Faisabilité et stack](docs/01-faisabilite-et-stack.md) | Ce qui est possible sans Mac, contraintes iOS réelles, options cloud, stack retenue |
| [02 — Architecture](docs/02-architecture.md) | Couches, modules, ajout d'armes/accessoires sans code, remplacement des modèles, sauvegarde |
| [03 — Assets](docs/03-assets.md) | Liste complète des modèles 3D, animations, décors, effets et sons ; sources gratuites/payantes ; licences ; priorités |
| [04 — V1 et feuille de route](docs/04-mvp-et-roadmap.md) | Périmètre de la première version jouable, 8 phases, contenu exact de la Phase 1 |
| [05 — Installation et tests Windows](docs/05-installation-et-tests-windows.md) | Mise en route, scénario de test, simulation AR |
| [06 — Build iOS cloud](docs/06-build-ios-cloud.md) | GitHub Actions + GameCI + macOS, certificats depuis Windows, secrets, TestFlight, prérequis Apple |

## Structure

```
Game/                       Projet Unity (Unity 6 LTS, URP)
  Assets/_Project/
    Resources/Data/         weapons.json, attachments.json, finishes.json, economy.json
    Scripts/Core/           Logique pure C# (sans Unity) — testée
    Scripts/Game/           Scènes, rendu, armes 3D, interface, entrées
    Scripts/Editor/         Configuration auto, builds, menu Debug
Tests/Arsenal.Core.Tests/   Tests xUnit du coeur logique
Tools/                      Générateur des données/modèles provisoires
docs/                       Documentation
.github/workflows/          CI (tests) + build iOS cloud
```

## Sécurité et contenu

- Aucun certificat, mot de passe ou clé n'est stocké dans le dépôt (`.gitignore` dédié, secrets GitHub chiffrés).
- Le mode réalité mixte affichera en permanence qu'armes et cibles sont virtuelles, rappellera de dégager l'espace et de ne jamais viser de personnes, et invitera à rester immobile pendant le tir.
- Les règles de compatibilité des accessoires sont des règles de jeu fictives, sans rapport avec des procédures réelles.
