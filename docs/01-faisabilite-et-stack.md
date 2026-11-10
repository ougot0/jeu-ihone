# 1. Faisabilité, contraintes iOS sans Mac et stack technique

## 1.1 Verdict de faisabilité

| Volet | Faisable ? | Commentaire honnête |
|---|---|---|
| Code du jeu (gameplay, IA, économie, personnalisation, sauvegarde) | **Oui, entièrement sous Windows** | Unity + C# tournent nativement sur Windows. C'est la partie que Claude Code peut produire le mieux. |
| Test du jeu classique (arsenal, stand de tir, missions) | **Oui, sous Windows** | Mode Play de l'éditeur, souris = doigt, build Windows de test. |
| Test de la réalité augmentée | **Partiellement sous Windows** | AR Foundation propose la *XR Simulation* (pièces virtuelles simulées dans l'éditeur, Windows inclus). Le vrai suivi ARKit (caméra, plans réels) ne se teste **que sur iPhone**. |
| Compilation iOS | **Pas sous Windows** | Unity sous Windows sait *exporter un projet Xcode* (module « iOS Build Support »), mais **la compilation, la signature et l'export IPA exigent Xcode, donc macOS**. Il n'existe pas de contournement légal et fiable. Solution : **un Mac dans le cloud** (runner macOS GitHub Actions, Unity Build Automation, Codemagic ou Mac loué). |
| Installation sur iPhone | **Oui, via TestFlight** | Nécessite le **Apple Developer Program (99 USD/an)**. Un identifiant Apple gratuit ne permet d'installer qu'en passant par Xcode sur un Mac (profils valables 7 jours) : inutilisable ici. |
| Graphismes « premium » réalistes | **Oui, mais dépend des modèles 3D** | Le code, l'éclairage, les matériaux PBR et le post-traitement sont prêts. Le rendu final dépend de modèles 3D de qualité (achetés, CC0 ou commandés à un artiste). Claude Code ne produit pas de modèles 3D détaillés : il fournit des **modèles provisoires** et le système qui permet de les remplacer sans toucher au code. |
| Animations (tir, rechargement, ennemis) | **Oui** | Procédurales au début (recul, culasse, chargeur), puis clips Mixamo / sur mesure. |

**Conclusion :** le projet est réaliste en développement incrémental. Le goulot d'étranglement n'est pas le code mais **les assets 3D/audio** et **le pipeline Apple** (compte développeur, certificats, Mac cloud). Les deux sont documentés ici.

## 1.2 Contraintes réelles du développement iOS sans Mac

1. **Xcode est obligatoire** pour compiler une app iOS ; il ne fonctionne que sur macOS.
2. **Signature de code** : il faut un certificat *Apple Distribution* + un profil de provisionnement. Ils peuvent être créés **depuis Windows** (OpenSSL + portail Apple Developer), voir `06-build-ios-cloud.md`.
3. **Débogage sur appareil** : pas de débogueur Xcode. On utilise : logs Unity, *Development Build* + connexion du Profiler Unity par Wi‑Fi (build ad hoc), et les rapports de plantage TestFlight.
4. **ARKit** fonctionne sur les iPhone compatibles iOS 15+ (cible du projet) ; une puce A12 ou plus récente est recommandée pour les performances et les fonctions avancées (occlusion de personnes, etc.). Le LiDAR (iPhone Pro) améliore la détection mais n'est pas requis.
5. **Coûts** : Apple Developer 99 USD/an ; minutes macOS du cloud (gratuites pour un dépôt **public** sur GitHub, décomptées avec un multiplicateur élevé pour un dépôt privé) ; licence Unity Personal gratuite sous le seuil de revenus Unity.
6. **App Store** : les jeux de tir sont autorisés avec la classification d'âge adaptée ; armes et cibles doivent rester fictives et virtuelles (c'est le cas ici).

## 1.3 Options de compilation iOS dans le cloud

| Option | Coût | Avantages | Inconvénients | Recommandation |
|---|---|---|---|---|
| **GitHub Actions** : runner Linux (GameCI) pour l'export Unity + runner **macOS** pour Xcode | Gratuit en dépôt public ; minutes incluses puis payant en privé | Tout est dans le dépôt, versionné, reproductible | Configuration initiale (licence Unity, certificats) | **Choix principal** — workflow fourni : `.github/workflows/ios-build.yml` |
| **Unity Build Automation** (ex-Cloud Build) | Payant à la minute | Gère Mac + signature dans l'interface Unity | Coût, moins de contrôle | Plan B simple |
| **Codemagic** | Offre gratuite limitée puis payant | Spécialisé mobile, Mac Apple Silicon, envoi TestFlight intégré | Service tiers supplémentaire | Plan B |
| Mac loué (MacinCloud, AWS EC2 Mac) | À l'heure / au mois | Xcode complet, débogage | Le plus cher, manuel | Seulement pour un débogage natif ponctuel |

## 1.4 Stack technique définitive

| Domaine | Choix | Raison |
|---|---|---|
| Moteur | **Unity 6 LTS (6000.0.x)** | Meilleur support iOS + ARKit, éditeur Windows, C#, grande communauté. Unreal est plus lourd sur mobile et son pipeline iOS sans Mac est plus pénible. |
| Rendu | **URP** (Universal Render Pipeline) | PBR mobile performant, post-traitement (bloom, tonemapping ACES, vignette), compatible AR Foundation. |
| Langage | C# 9 | Version supportée par Unity. |
| Réalité augmentée | **AR Foundation 6 + Apple ARKit XR Plugin 6** | API officielle ; *XR Simulation* pour tester dans l'éditeur Windows. |
| Entrées | **Input System** (avec repli sur l'ancien gestionnaire) | Tactile multi-doigts, gyroscope (`AttitudeSensor`), manettes. |
| Interface | uGUI construit par code (Phase 1) → prefabs + TextMeshPro (Phase 3+) | Aucun asset requis au départ, remplaçable. |
| Données | **Fichiers JSON** (`Resources/Data`) + classes C# sérialisables | Équilibrage sans recompiler, lisibles par les tests .NET. (Les ScriptableObjects restent possibles pour les références d'assets.) |
| Coeur logique | Assembly `Arsenal.Core` **sans dépendance Unity** | Testable avec `dotnet test` sur n'importe quelle machine et en CI gratuite. |
| Sauvegarde | JSON local versionné + somme de contrôle + copie de secours + écriture atomique | Fiable même en cas de coupure. |
| CI | GitHub Actions (tests .NET) + GameCI + runner macOS | Voir `06-build-ios-cloud.md`. |
| Gros fichiers | Git LFS (`.gitattributes` fourni) | Modèles, textures et sons. |
