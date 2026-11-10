# 2. Architecture du jeu

## 2.1 Couches

```
┌───────────────────────────────────────────────────────────────┐
│ Arsenal.Editor (éditeur uniquement)                            │
│  ProjectSetup (URP, iOS, scènes) · BuildScript (CI) · Debug    │
├───────────────────────────────────────────────────────────────┤
│ Arsenal.Game (Unity)                                           │
│  Bootstrap   GameServices (services + sauvegarde auto)         │
│  Rendering   MaterialLibrary · ProceduralTextures · PostFX     │
│  Weapons     WeaponVisualBuilder · WeaponVisual (sockets 3D)   │
│  Arsenal     ArsenalSceneController · Environment · Turntable  │
│  UI          ArsenalHud · UiFactory · SafeAreaFitter           │
│  Input       PointerInput (tactile/souris, 2 systèmes d'entrée)│
│  (à venir)   FPS · AR · Missions · AI · Audio · Gyro           │
├───────────────────────────────────────────────────────────────┤
│ Arsenal.Core (C# pur, AUCUNE dépendance Unity, testé en CI)    │
│  Data        définitions armes/accessoires/revêtements/éco     │
│  Rules       CompatibilityService · StatCalculator             │
│  Services    ArsenalService · EconomyService · Progression     │
│  Profile     PlayerProfile · WeaponBuild · GameSettings        │
│  Save        SaveService · FileSaveStorage (atomique + .bak)   │
│  Simulation  WeaponSimulation (munitions, cadence, dispersion) │
├───────────────────────────────────────────────────────────────┤
│ Données : Resources/Data/{weapons,attachments,finishes,economy}.json │
└───────────────────────────────────────────────────────────────┘
```

Règle : **Core ne connaît pas Unity** (`noEngineReferences: true` dans l'asmdef). Toute la logique de jeu vérifiable (prix, compatibilité, statistiques, sauvegarde, tir) y vit et est couverte par `Tests/Arsenal.Core.Tests`. La couche Game ne fait qu'afficher et relayer les entrées.

## 2.2 Correspondance avec les modules demandés

| Module demandé | Emplacement | État |
|---|---|---|
| Gestion de l'arsenal | `Core/Services/ArsenalService.cs`, `Game/Arsenal/*` | ✅ Phase 1 |
| Données des armes | `Core/Data/Definitions.cs`, `Resources/Data/weapons.json` | ✅ |
| Personnalisation (configurations) | `ArsenalService` (créer, dupliquer, renommer, supprimer, installer, revêtement) | ✅ logique — UI Atelier en Phase 3 |
| Système d'accessoires 3D | `WeaponVisual` + `WeaponVisualBuilder` (sockets, pièces masquées) | ✅ (accessoires par défaut visibles) |
| Économie | `EconomyService`, `ProgressionService`, `economy.json` | ✅ achat d'armes en Phase 1 |
| Sauvegardes | `Core/Save/SaveService.cs` + autosave dans `GameServices` | ✅ |
| Gameplay FPS | `Core/Simulation/WeaponSimulation.cs` (+ `Game/FPS` en Phase 2) | 🟡 logique prête et testée |
| Gameplay AR | `Game/AR` (Phase 5) | ⏳ |
| Missions | `Core/Missions` + `Game/Missions` (Phase 6) | ⏳ |
| IA | `Game/AI` (Phase 6) | ⏳ |
| Interface | `Game/UI` | ✅ arsenal + réglages |
| Audio | `Game/Audio` (Phase 2) | ⏳ |
| Paramètres et gyroscope | `GameSettings` (persistés) + écran Réglages ; contrôleur gyroscope en Phase 2 | 🟡 |

## 2.3 Ajouter du contenu sans code

### Une arme
Ajouter une entrée dans `weapons.json` :
- `id`, `displayName` (nom **fictif**), `family` (`pistol|revolver|smg|rifle|sniper|shotgun`), `price`, `unlockLevel`, `fireMode`, `pellets`
- `baseStats` : `damage, accuracy, recoil, fireRate, magazineSize, handling, reloadTime`
- `slots` : emplacements (`types` acceptés, `mount` = fixation fictive, `defaultAttachment`, `required`, `position`, `rotation`)
- `parts` : modèle provisoire en primitives **ou** `modelId` pour un vrai modèle

Le validateur (`DatabaseValidator`) signale toute incohérence dans la console Unity **et** fait échouer la CI.

### Un accessoire
Entrée dans `attachments.json` : `type`, `mounts`, `families` (vide = toutes), `conflicts`, `hidesParts`, `modifiers` (`{stat, add, mul}`), `parts` ou `modelId`.

### Règles de compatibilité (règles de jeu explicites et fictives)
Un accessoire s'installe si :
1. son `type` figure dans les `types` de l'emplacement ;
2. l'emplacement n'exige pas de fixation, ou la fixation fictive (`rail-t`, `rail-c`, `filetage-s`, `mag-r30`…) figure dans ses `mounts` ;
3. sa liste `families` est vide ou contient la famille de l'arme ;
4. aucun accessoire installé ailleurs n'est en conflit (`conflicts` par id ou par type) ;
5. le joueur le possède.

Ces règles sont purement ludiques et ne décrivent aucune pièce ni procédure réelle.

### Statistiques finales
`(base + Σ add) × Π mul`, puis bornage (`StatCalculator`). Elles pilotent `WeaponSimulation` : intervalle de tir = 60 / cadence, dispersion selon précision/visée/chaleur de recul, recul caméra selon recul, temps de visée selon maniabilité, temps de rechargement, portée efficace.

## 2.4 Remplacer les modèles provisoires

- Arme : placer le prefab dans `Assets/_Project/Resources/Models/Weapons/<modelId>.prefab` et renseigner `modelId`.
  - Points de fixation : objets enfants nommés `Socket_<idEmplacement>` (ex. `Socket_optic`). Axe +Z = vers la bouche, +Y = haut.
  - Matériaux teintés par les revêtements : nom contenant `Body`.
  - Pièces masquables (ex. `rear_sight`) : objets nommés comme dans `hidesParts`.
- Accessoire : `Resources/Models/Attachments/<modelId>.prefab`, origine = point de fixation.

Aucun script n'est à modifier.

## 2.5 Sauvegarde

`persistentDataPath/saves/profile.json` contient une enveloppe `{version, savedUtcTicks, checksum, payload}`.
- Écriture : fichier `.tmp` puis remplacement atomique, l'ancienne version devient `.bak`.
- Lecture : si la somme de contrôle est invalide → `.bak` ; sinon nouveau profil (message affiché).
- Sauvegarde automatique 0,75 s après chaque modification, et à la mise en arrière-plan de l'app.
- `Migrate()` accueille les futures évolutions de format.

## 2.6 Arborescence

```
jeu-ihone/
├─ Game/                         ← projet Unity (ouvrir ce dossier dans Unity Hub)
│  ├─ Assets/_Project/
│  │  ├─ Resources/Data/         ← équilibrage (JSON)
│  │  ├─ Resources/Materials/    ← généré par Arsenal > Configurer le projet
│  │  ├─ Scenes/                 ← Arsenal.unity généré automatiquement
│  │  ├─ Settings/               ← asset URP généré
│  │  └─ Scripts/{Core,Game,Editor}
│  ├─ Packages/manifest.json
│  └─ ProjectSettings/
├─ Tests/Arsenal.Core.Tests/     ← dotnet test
├─ Tools/                        ← générateur des données provisoires
├─ docs/
└─ .github/workflows/            ← CI + build iOS cloud
```
