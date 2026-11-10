# 4. Première version jouable et feuille de route

## 4.1 Version jouable 1 (« V1 ») — périmètre raisonnable

Objectif : une boucle complète **gagner → acheter → personnaliser → tester**, sur Windows puis iPhone.

| Inclus dans la V1 | Reporté |
|---|---|
| Armurerie 3D, catalogue des 6 armes, rotation/zoom | Cartes extérieures, zone industrielle |
| Pistolet + fusil d'assaut entièrement jouables (tir, rechargement, recul, effets, sons) ; 4 autres armes jouables avec les mêmes systèmes | Animations sur mesure par arme (procédurales en V1) |
| Atelier : accessoires 3D, revêtements, plusieurs configurations nommées, sauvegarde auto | Motifs de camouflage, autocollants |
| Stand de tir 3D : tir libre, défi précision, défi chronométré, récompenses | Classements en ligne |
| Mode AR : détection de surfaces, placement/déplacement/suppression de cibles, tir libre + défi précision, avertissements de sécurité | Personnages virtuels en AR (Phase 5b) |
| Réglages : gyroscope optionnel, sensibilités, qualité graphique | — |
| Build iOS via GitHub Actions + TestFlight | Publication App Store |

Missions FPS avec IA = **V2** (Phase 6), une fois la V1 validée sur iPhone.

## 4.2 Phases et état

| Phase | Contenu | Testable par | État |
|---|---|---|---|
| **1 — Fondations** | Structure du dépôt, projet Unity, coeur logique testé, données JSON, économie, sauvegarde, arsenal 3D (décor, éclairage, présentoir tactile, mur de collection), achat et équipement d'armes, réglages, CI | Unity Play (Windows), `dotnet test` | ✅ **livrée (voir 4.3)** |
| 2 — Première arme fonctionnelle | Vue FPS, bras, tir/rechargement pilotés par `WeaponSimulation`, recul, effets, audio spatialisé, gyroscope + calibration | Unity Play, build Windows | ⏳ |
| 3 — Personnalisation | Écran Atelier (emplacements, compatibilités, aperçu des stats, achat d'accessoires, revêtements, renommage) | Unity Play | ⏳ (logique déjà prête et testée) |
| 4 — Stand de tir classique | Scène stand, cibles carton/métal/mobiles, impacts, score, chrono, récompenses | Unity Play | ⏳ |
| 5 — Réalité augmentée | AR Foundation, plans, placement, session de tir, sécurité ; XR Simulation sous Windows | Éditeur (simulation) + iPhone | ⏳ |
| 6 — Missions | Missions data-driven, IA (patrouille, détection, couverture, santé), cartes | Unity Play | ⏳ |
| 7 — Progression | Boutique d'accessoires/revêtements, déblocages, équilibrage | Unity Play + tests | 🟡 (économie déjà prête) |
| 8 — Optimisation iOS | Profilage, LOD, compression, niveaux de qualité, tests appareils | iPhone (TestFlight) | ⏳ |

## 4.3 Ce que la Phase 1 contient réellement

Implémenté :
- **Coeur logique** (`Arsenal.Core`, sans Unity) : définitions de données, validateur, règles de compatibilité, calcul des statistiques, arsenal (configurations multiples, installation/désinstallation, revêtements, sélection), économie (achats, niveaux requis, récompenses, montée de niveau), progression XP, sauvegarde fiable, simulation de tir.
- **23 tests automatisés** qui passent (`dotnet test Tests/Arsenal.Core.Tests`) et s'exécutent en CI.
- **Données** : 6 armes fictives, 35 accessoires, 8 revêtements, configuration économique.
- **Scène Arsenal Unity** générée par code : salle, piédestal, mur de collection (armes possédées / verrouillées), éclairage clé/contre-jour/remplissage, sonde de réflexion, post-traitement URP.
- **Présentoir** : rotation au doigt avec inertie, pincement/molette, glissement à deux doigts, double-toucher, rotation automatique, animation de changement d'arme.
- **Interface** : crédits, niveau/XP, catalogue avec prix et niveaux requis, fiche technique à 7 jauges avec comparaison à l'arme équipée, configurations, achat, équipement, réglages persistants (gyroscope, sensibilités, qualité, volume, réinitialisation), zone sûre des iPhone. Les modes non encore développés sont affichés **désactivés** avec leur phase.
- **Outils éditeur** : configuration automatique (URP, matériaux, iOS, scène), builds iOS/Windows, menu Debug (+crédits, +niveau).
- **CI** : tests .NET à chaque push ; workflow de build iOS dans le cloud (manuel, nécessite vos secrets).

Limites connues de la Phase 1 (à lire) :
- Le code Unity (`Scripts/Game`, `Scripts/Editor`) **n'a pas pu être compilé ni lancé dans Unity** dans l'environnement de développement de Claude (Linux, sans éditeur Unity). Sa syntaxe a été vérifiée avec le compilateur C#, mais pas contre les API Unity. Le premier lancement chez vous peut révéler des erreurs de compilation mineures : copiez la console Unity dans une demande à Claude Code pour correction.
- Le workflow iOS n'a pas été exécuté (il requiert votre licence Unity et vos certificats Apple).
- Modèles 3D, textures et effets sont **provisoires** (primitives + textures procédurales).
- Aucun moyen de gagner des crédits en jeu avant la Phase 4 : utilisez le menu **Arsenal > Debug** en mode Play pour tester les achats.
