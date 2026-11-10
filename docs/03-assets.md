# 3. Liste des modèles 3D et assets nécessaires

Légende — **V1** : indispensable dès la première version jouable · **Prio** : P0 (bloquant) → P3 (bonus).
**Statut actuel** : toutes les armes, tous les accessoires et l'armurerie existent déjà en **modèles provisoires** générés à partir de `weapons.json` / `attachments.json` (primitives + matériaux PBR procéduraux). Chaque asset ci-dessous les **remplace sans modifier le code** (voir `02-architecture.md` §2.4).

## 3.1 Sources recommandées et licences

| Source | Type | Licence | Usage dans ce projet |
|---|---|---|---|
| **Poly Haven** (polyhaven.com) | Textures PBR, HDRI, quelques modèles | **CC0** (aucune attribution) | Sols, murs, métaux, bois, HDRI d'éclairage |
| **ambientCG** (ambientcg.com) | Textures PBR | **CC0** | Béton, métal peint, caoutchouc, panneaux |
| **Kenney** (kenney.nl) | Modèles stylisés, sons, UI | **CC0** | Sons d'interface et d'impacts, icônes |
| **Quaternius** (quaternius.com) | Modèles low-poly, personnages animés | **CC0** | Personnages/props provisoires (style stylisé) |
| **Mixamo** (Adobe) | Personnages riggés + animations | Gratuit, usage commercial autorisé **intégré au jeu** ; pas de redistribution des fichiers bruts | Locomotion, visée, réactions aux impacts, chutes des adversaires |
| **Unity Asset Store** | Tout type | *Standard Unity Asset Store EULA* : utilisation dans le jeu OK, **interdiction de publier les fichiers dans un dépôt public** | Packs d'armes réalistes, décors industriels, FX. ⚠️ Si le dépôt GitHub est public, stocker ces assets hors dépôt (ou passer le dépôt en privé). |
| **Sketchfab** | Modèles | Variable par modèle (CC-BY = attribution, CC-BY-NC = **non commercial**, licences Store payantes) | Au cas par cas ; vérifier l'absence de logos/marques |
| **Freesound** | Sons | CC0 / CC-BY / CC-BY-NC selon le son | Préférer CC0 ; tenir le registre `CREDITS.md` pour CC-BY |
| **Sonniss GDC Game Audio Bundle** | Sons pro | Libre de droits, usage commercial, pas de revente brute | Tirs, mécanismes, ambiances |
| **Synty (POLYGON Military)** | Pack stylisé payant | Licence commerciale Synty | Alternative cohérente si style low-poly choisi |
| **Artiste 3D freelance** (ArtStation, Fiverr, Malt) | Sur mesure | Cession de droits à prévoir au contrat | **Armes « héros » originales et réalistes** (recommandé) |
| **Blender** | Outil gratuit | GPL (n'affecte pas les assets produits) | Retouches, création des sockets `Socket_*`, export FBX/GLB |

Règles du projet : noms **fictifs**, **aucun logo ni marque** de fabricant réel, designs originaux ou suffisamment génériques ; chaque asset importé est inscrit dans `CREDITS.md` (source, auteur, licence, lien) ; aucune source douteuse (sites de « rips », modèles extraits d'autres jeux).

**Formats** : FBX pour les modèles riggés/animés (import Unity le plus fiable) ; GLB accepté pour les props statiques (paquet `com.unity.cloud.gltfast`). Textures PNG/TGA 2048² max pour les armes (1024² sur mobile via *texture import overrides*), ORM (occlusion/rugosité/métal) empaquetées.

## 3.2 Armes

| ID | Nom | Fonction | V1 | Format | Rig / anim | Gratuit ? | Licence / contrainte | Sur mesure ? | Prio |
|---|---|---|---|---|---|---|---|---|---|
| WPN-01 | VK-9 Sentinel (pistolet) | Arme de départ | ✅ | FBX | Rig mécanique : culasse, détente, arrêtoir, chargeur séparé | Possible (packs CC0 stylisés) ; réaliste = payant | Pas de marque | **Recommandé** | P0 |
| WPN-02 | Hollis R6 Warden (revolver) | Arme à déblocage niv. 2 | ✅ | FBX | Barillet rotatif, chien, détente, éjecteur | Rare en gratuit réaliste | idem | Recommandé | P1 |
| WPN-03 | Kestrel K-45 (PM) | Cadence élevée | ✅ | FBX | Culasse, crosse pliante (os séparé), chargeur | Asset Store (payant) | Pas de marque | Recommandé | P1 |
| WPN-04 | Talon AR-7 (fusil d'assaut) | Plateforme modulaire phare | ✅ | FBX | Levier d'armement, culasse, détente, chargeur | Asset Store (payant) | Pas de marque | **Recommandé** | P0 |
| WPN-05 | Breaker 12 (pompe) | Courte portée | ✅ | FBX | Pompe coulissante, culasse, tube | Asset Store | idem | Recommandé | P2 |
| WPN-06 | Longreach SR-12 (précision) | Longue portée | ✅ | FBX | Verrou rotatif + recul, chargeur | Asset Store | idem | Recommandé | P2 |
| WPN-C | Composants séparés (culasses, carcasses, canons, gardes-mains) | Animations mécaniques + variantes visuelles | ✅ | FBX (objets séparés, pivots corrects) | Pivots placés pour l'animation | — | — | Inclus dans la commande WPN | P0 |
| MAG-* | 8 chargeurs (standard/étendus par famille) | Visible + animation de rechargement | ✅ | FBX | Aucun (animé par le bras/l'arme) | Souvent inclus dans les packs | — | Avec WPN | P0 |
| OPT-* | 5 optiques (point rouge, reflex, holographique, lunettes 4x/8x) | Visée, statistiques | ✅ | FBX + matériau réticule (shader emissif) | Non | Asset Store / sur mesure | Pas de marque | Possible | P1 |
| MUZ-* | 5 accessoires de bouche | Recul / précision | ✅ | FBX | Non | Facile à modéliser | — | Simple (Blender) | P2 |
| GRP-* | 5 poignées/bipied | Recul / maniabilité | ✅ | FBX | Bipied : 2 os (jambes) | Facile | — | Simple | P2 |
| STK-* | 7 crosses | Recul / maniabilité | ✅ | FBX | Crosse pliante : 1 os | Facile | — | Simple | P2 |
| RAIL-* | Lampes (2), laser virtuel (1) | Éclairage, aide à la visée | ✅ | FBX | Non (faisceau = effet) | Facile | — | Simple | P2 |
| COS-* | Breloques (2+), autocollants | Cosmétique, collection | ❌ | FBX / PNG | Breloque : physique de balancement (script) | Facile | — | Simple | P3 |
| FIN-* | Revêtements (8+) : couleurs, motifs camouflage | Cosmétique | ✅ (unis) | Textures masques PNG | Non | CC0 (motifs) | — | Motifs sur mesure | P2 |
| FPA-01 | Bras à la première personne (gants) | Tenir/animer toutes les armes | ✅ Phase 2 | FBX | **Rig humanoïde bras** + anims : idle, tir, recharge (par famille), inspection, sortie/rangement | Asset Store / sur mesure | — | Recommandé | P1 |

## 3.3 Personnages et cibles

| ID | Nom | Fonction | V1 | Format | Rig / anim | Gratuit ? | Licence | Sur mesure ? | Prio |
|---|---|---|---|---|---|---|---|---|---|
| TGT-01 | Cible carton (silhouette fictive + zones) | Stand de tir classique et AR | ✅ | FBX/GLB | Non ; perforations = décalques | Facile (Blender) | — | Simple | P0 |
| TGT-02 | Cible métallique basculante | Retour sonore/visuel | ✅ | FBX | 1 charnière (animation script) | Facile | — | Simple | P0 |
| TGT-03 | Cible mobile sur rail | Mode cibles mobiles | ✅ | FBX | Déplacement script | Facile | — | Simple | P1 |
| TGT-04 | Mannequin d'entraînement | Défi précision / AR | ✅ | FBX | Rig simple (chute, oscillation) | Quaternius/Mixamo | CC0 / Mixamo | Non | P1 |
| CHR-01 | Adversaire fictif (tenue d'entraînement, couleurs de jeu) | Missions IA, AR tactique | Phase 6 | FBX | **Rig humanoïde** compatible Mixamo | Mixamo / Quaternius | Mixamo : intégré au jeu seulement | Variantes sur mesure | P1 |
| ANM-01 | Locomotion (marche, course, accroupi, strafe) | IA | Phase 6 | FBX (Humanoid) | Oui | Mixamo | idem | Non | P1 |
| ANM-02 | Visée / tir / rechargement (adversaire) | IA | Phase 6 | FBX | Oui | Mixamo | idem | Non | P1 |
| ANM-03 | Réactions aux impacts, chute, mise à couvert | IA | Phase 6 | FBX | Oui (+ ragdoll Unity) | Mixamo | idem | Non | P1 |

## 3.4 Environnements

| ID | Nom | Fonction | V1 | Format | Rig | Gratuit ? | Licence | Sur mesure ? | Prio |
|---|---|---|---|---|---|---|---|---|---|
| ENV-01 | Armurerie (menu principal) | Présentation premium | ✅ (provisoire en place) | FBX modulaire + textures PBR | Non | Textures CC0 ; kit modulaire Asset Store | — | Mur de présentation / piédestal sur mesure | P1 |
| ENV-02 | Stand de tir intérieur | Phase 4, missions précision | ✅ | FBX modulaire | Non | Kits Asset Store, textures CC0 | — | Possible | P0 |
| ENV-03 | Centre d'entraînement (kill house fictive) | Missions | Phase 6 | FBX modulaire | Non | Asset Store | — | — | P2 |
| ENV-04 | Entrepôt | Missions IA | Phase 6 | FBX modulaire | Non | Asset Store / Quaternius | — | — | P2 |
| ENV-05 | Zone industrielle | Missions | Phase 6+ | FBX | Non | Asset Store | — | — | P3 |
| ENV-06 | Terrain d'entraînement extérieur | Longue portée (fusil de précision) | Phase 6+ | Terrain Unity + HDRI CC0 | Non | Poly Haven | CC0 | — | P3 |
| ENV-HDR | HDRI studio / extérieur | Reflets réalistes des métaux | ✅ | EXR/HDR | — | Poly Haven | CC0 | Non | P1 |
| ENV-PRP | Props (caisses, établis, étagères, mallettes) | Habillage | ✅ | FBX/GLB | Non | Poly Haven / Kenney / Quaternius | CC0 | Non | P2 |

## 3.5 Effets visuels et sonores

| ID | Nom | Fonction | V1 | Format | Anim | Gratuit ? | Licence | Sur mesure ? | Prio |
|---|---|---|---|---|---|---|---|---|---|
| FX-01 | Flamme de bouche (3 variantes + modérée) | Retour de tir | ✅ | Texture flipbook PNG + Particle System | Flipbook | CC0 (Kenney) / sur mesure | — | Simple | P0 |
| FX-02 | Fumée, étincelles, douilles éjectées | Réalisme | ✅ | Particules + mesh douille | Physique | Facile | — | Simple | P1 |
| FX-03 | Impacts (carton, métal, béton, bois) + décalques | Lisibilité des touches | ✅ | PNG + particules | Non | CC0 | — | Simple | P0 |
| FX-04 | Traçantes, faisceau laser, faisceau lampe | Accessoires | ✅ | Shader + LineRenderer | Non | — | — | Code | P2 |
| FX-05 | Réticule de placement AR, maillage de plan détecté | Mode AR | Phase 5 | Shader / PNG | Oui | — | — | Code | P0 (AR) |
| SFX-01 | Tirs par famille (+ variante modérée) | Retour sonore | ✅ | WAV 48 kHz → compressé ADPCM/Vorbis | — | Sonniss / Freesound CC0 | Vérifier chaque son | Mixage | P0 |
| SFX-02 | Mécanismes : rechargement, culasse, pompe, verrou, tir à vide | Animations | ✅ | WAV | — | Sonniss / Freesound | idem | — | P0 |
| SFX-03 | Impacts par matériau, chute de cible | Feedback | ✅ | WAV | — | Kenney CC0 / Sonniss | — | — | P1 |
| SFX-04 | Interface (survol, achat, équipement, montée de niveau) | Menu | ✅ | WAV/OGG | — | Kenney CC0 | CC0 | — | P2 |
| SFX-05 | Ambiances (armurerie, stand, entrepôt), musique de menu | Immersion | ❌ | OGG | — | Freesound / compositeur | Variable | Musique sur mesure | P3 |

## 3.6 Budget indicatif

- **Version 100 % gratuite** (CC0 + Mixamo + modèles provisoires améliorés dans Blender) : jouable et cohérente, rendu « stylisé propre ».
- **Version réaliste premium** : 1 pack d'armes réalistes sans marques (Asset Store, ~30–100 €) **ou** commande de 2 armes « héros » à un artiste (ordre de grandeur 200–600 € par arme avec composants séparés et accessoires), + bras FPS animés (~30–80 €), + kit d'environnement industriel (~20–60 €). Les montants sont indicatifs et à vérifier au moment de l'achat.
