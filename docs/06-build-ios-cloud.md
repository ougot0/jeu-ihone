# 6. Compilation iOS dans le cloud, secrets et prérequis Apple

## 6.1 Principe

```
Windows (vous)            GitHub Actions
──────────────            ─────────────────────────────────────────────────────────
git push  ───────────►    Job 1  ubuntu + Docker GameCI (Unity)  →  projet Xcode
                          Job 2  macos-15 + Xcode                →  archive signée → IPA
                                                                 →  (option) TestFlight
iPhone  ◄── TestFlight ◄──────────────────────────────────────────────┘
```

Fichier : `.github/workflows/ios-build.yml`, lancé manuellement (**Actions → iOS Build → Run workflow**).

## 6.2 Prérequis Apple (une seule fois)

1. **Apple Developer Program** (99 USD/an) sur developer.apple.com — obligatoire pour TestFlight et l'installation sur iPhone sans Mac.
2. **Identifiant d'app** (Certificates, IDs & Profiles → Identifiers) : ex. `com.votrenom.arsenal`. Capacités : aucune particulière (la caméra ne demande qu'une description d'usage, déjà fournie par le projet).
3. **Certificat Apple Distribution** créé depuis Windows avec OpenSSL (inclus dans Git pour Windows) :
   ```bash
   openssl req -new -newkey rsa:2048 -nodes -keyout arsenal_dist.key -out arsenal_dist.csr -subj "/emailAddress=VOTRE_EMAIL/CN=Arsenal Distribution/C=FR"
   ```
   - Portail Apple → Certificates → **+** → *Apple Distribution* → envoyer `arsenal_dist.csr` → télécharger `distribution.cer`.
   ```bash
   openssl x509 -inform DER -in distribution.cer -out distribution.pem
   ```
   ```bash
   openssl pkcs12 -export -legacy -inkey arsenal_dist.key -in distribution.pem -out arsenal_dist.p12
   ```
   (Choisir un mot de passe d'export solide. L'option `-legacy` assure la compatibilité avec le trousseau macOS ; retirez-la si votre OpenSSL ne la connaît pas.)
4. **Profil de provisionnement** : Profiles → **+** → *App Store Connect* (distribution) → votre identifiant d'app → votre certificat → télécharger `Arsenal_AppStore.mobileprovision`. Notez son **UUID** (visible dans le portail, ou dans le fichier ouvert avec un éditeur de texte, clé `UUID`).
5. **App Store Connect** → Mes apps → **+** → nouvelle app avec le même identifiant (nécessaire pour TestFlight).
6. (Option TestFlight automatique) App Store Connect → Utilisateurs et accès → **Intégrations → Clés API** → créer une clé (rôle *App Manager*), télécharger `AuthKey_XXXX.p8`, noter *Key ID* et *Issuer ID*.

> Ces fichiers (`.key`, `.p12`, `.cer`, `.mobileprovision`, `.p8`) ne doivent **jamais** être commités. Le `.gitignore` les bloque ; conservez-les dans un coffre (gestionnaire de mots de passe).

## 6.3 Licence Unity pour la CI

GameCI a besoin d'une licence Unity :
- **Personal** : activez Unity Personal dans Unity Hub sur votre PC, puis copiez le contenu du fichier de licence `C:\ProgramData\Unity\Unity_lic.ulf` dans le secret `UNITY_LICENSE`, avec `UNITY_EMAIL` et `UNITY_PASSWORD` (votre compte Unity).
- **Pro** : `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`.
Suivez la documentation à jour de GameCI (game.ci → *Activation*) si la procédure Unity a évolué.

## 6.4 Secrets et variables GitHub

Dépôt → **Settings → Secrets and variables → Actions**.

**Secrets** (chiffrés, jamais affichés dans les logs) :

| Nom | Contenu |
|---|---|
| `UNITY_LICENSE` | Contenu du fichier `.ulf` |
| `UNITY_EMAIL` / `UNITY_PASSWORD` | Compte Unity |
| `IOS_P12_BASE64` | `arsenal_dist.p12` encodé en base64 |
| `IOS_P12_PASSWORD` | Mot de passe d'export du .p12 |
| `IOS_PROVISIONING_PROFILE_BASE64` | Le `.mobileprovision` encodé en base64 |
| `APPSTORE_API_KEY_ID` | (option) Key ID App Store Connect |
| `APPSTORE_API_ISSUER_ID` | (option) Issuer ID |
| `APPSTORE_API_PRIVATE_KEY_BASE64` | (option) Le `.p8` encodé en base64 |

Encodage base64 sous Windows (PowerShell) :

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("arsenal_dist.p12")) | Set-Clipboard
```

**Variables** (non secrètes, onglet *Variables*) :

| Nom | Exemple |
|---|---|
| `IOS_BUNDLE_ID` | `com.votrenom.arsenal` |
| `APPLE_TEAM_ID` | `ABCDE12345` (Membership details) |
| `IOS_PROFILE_UUID` | UUID du profil de provisionnement |

## 6.5 Lancer un build

1. Actions → **iOS Build** → *Run workflow* (cocher « Envoyer sur TestFlight » si la clé API est configurée).
2. Job 1 (~20–40 min la première fois, cache ensuite) : export Xcode.
3. Job 2 (~10–20 min) : archive signée, IPA en artefact `arsenal-ipa`, envoi TestFlight en option.
4. Sur l'iPhone : installer l'app **TestFlight**, accepter l'invitation (testeur interne = vous) → installer *Arsenal*.

Coûts : en dépôt **public**, les runners standard GitHub sont gratuits ; en dépôt **privé**, les minutes macOS sont décomptées avec un multiplicateur élevé sur votre quota (vérifiez la grille GitHub en vigueur). Attention : un dépôt public ne doit contenir aucun asset sous licence non redistribuable (Asset Store).

## 6.6 Dépannage courant

| Symptôme | Cause probable |
|---|---|
| Job 1 : « License activation failed » | `UNITY_LICENSE` invalide/expiré, ou mauvaise version d'Unity → réactiver dans Hub et recopier le `.ulf`. |
| Job 1 : manque d'espace disque | L'image iOS est lourde ; l'étape « Libérer de l'espace » est déjà présente. |
| Job 2 : « No signing certificate » | `.p12` sans clé privée, mauvais mot de passe, ou certificat expiré/révoqué. |
| Job 2 : « Provisioning profile doesn't match » | `IOS_BUNDLE_ID` ≠ identifiant du profil, ou `IOS_PROFILE_UUID` erroné. |
| TestFlight refuse le build | Numéro de build déjà utilisé (le workflow utilise `GITHUB_RUN_NUMBER`), ou conformité export : répondre au questionnaire de chiffrement dans App Store Connect. |

## 6.7 Alternatives

- **Unity Build Automation** : connecter le dépôt dans le dashboard Unity, cible iOS, importer `.p12` + profil ; distribution vers TestFlight possible. Payant à la minute.
- **Codemagic** : modèle de workflow Unity iOS, gestion des certificats intégrée.
Les deux remplacent les jobs 1 et 2 sans modifier le projet (`BuildScript.BuildiOS` reste le point d'entrée).
