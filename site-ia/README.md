# ✨ Mon IA – chat Logique & Maths + modes Rigolo

Un site de chat IA en **Next.js + TypeScript** :

- 🧮 **Logique & Maths** : répond étape par étape, puis vérifie son résultat.
- 🇲🇦 🇩🇿 **Modes Rigolo** : apprend des mots de darija (mot, prononciation, traduction, exemple) avec humour et bienveillance.
- Réponses en **streaming** (le texte s'affiche au fur et à mesure).
- **Protections** : limite de messages par visiteur, taille maximale des messages, gestion des erreurs.
- La clé API reste **côté serveur** : elle n'est jamais envoyée au navigateur.
- Fournisseur d'IA interchangeable : **Gemini** (gratuit) ou **Claude Haiku**.

---

## 1. Lancer le site sur ton PC (Windows)

### a) Installer les outils (une seule fois)

1. **Node.js** : télécharge la version **LTS** sur https://nodejs.org et installe-la (garde les options par défaut).
2. **Git** : https://git-scm.com/download/win (options par défaut).
3. **VS Code** (éditeur de code conseillé) : https://code.visualstudio.com

Vérifie l'installation : ouvre **PowerShell** (touche Windows, tape « PowerShell ») et tape :

```bash
node -v
```

Tu dois voir un numéro de version, par exemple `v22.x.x`.

### b) Récupérer le projet

```bash
git clone https://github.com/ougot0/jeu-ihone.git
```

```bash
cd jeu-ihone/site-ia
```

### c) Installer les dépendances (une seule fois)

```bash
npm install
```

### d) Créer ton fichier de clés

```bash
copy .env.example .env.local
```

Ouvre `.env.local` dans VS Code et colle ta clé Gemini (voir section 2).

### e) Lancer le site

```bash
npm run dev
```

Ouvre http://localhost:3000 dans ton navigateur. 🎉
Pour arrêter le serveur, appuie sur `Ctrl + C` dans PowerShell.

> 💡 Dès que tu modifies un fichier et que tu l'enregistres, la page se met à jour toute seule.

---

## 2. Obtenir une clé API

### Gemini (gratuit, pour commencer)

1. Va sur https://aistudio.google.com/apikey et connecte-toi avec un compte Google.
2. Clique sur **Create API key** (Créer une clé API).
3. Copie la clé (elle commence souvent par `AIza…`).
4. Colle-la dans `.env.local` :

```
AI_PROVIDER=gemini
GEMINI_API_KEY=AIza...ta-cle...
```

5. Relance `npm run dev` (Ctrl + C, puis la commande à nouveau).

### Claude Haiku (payant, très peu cher)

1. Crée un compte sur https://console.anthropic.com et ajoute quelques euros de crédit.
2. Menu **API Keys** → **Create Key**, puis copie la clé (`sk-ant-…`).
3. Dans `.env.local` :

```
AI_PROVIDER=claude
ANTHROPIC_API_KEY=sk-ant-...ta-cle...
```

Tu n'as **rien à changer dans le code** : seule la ligne `AI_PROVIDER` décide quelle IA est utilisée.

### 🔒 Sécurité des clés

- `.env.local` est ignoré par Git : il ne sera **jamais** envoyé sur GitHub. Ne le renomme pas.
- Ne colle jamais ta clé dans un fichier de code, ni dans une discussion.
- Si ta clé a fuité, supprime-la sur le site du fournisseur et crées-en une nouvelle.

---

## 3. Mettre le site en ligne avec Vercel (gratuit)

1. Pousse ton code sur GitHub (c'est déjà le cas pour ce dépôt).
2. Va sur https://vercel.com et clique sur **Sign up**, puis **Continue with GitHub**.
3. Clique sur **Add New… → Project**, puis **Import** à côté du dépôt `jeu-ihone`.
4. ⚠️ Important : à la ligne **Root Directory**, clique sur **Edit** et choisis le dossier **`site-ia`** (le dépôt contient aussi ton jeu Unity).
5. Déplie **Environment Variables** et ajoute les mêmes lignes que dans ton `.env.local` :
   - `AI_PROVIDER` = `gemini`
   - `GEMINI_API_KEY` = ta clé
6. Clique sur **Deploy**. Après une à deux minutes, tu obtiens une adresse du type `https://ton-projet.vercel.app`. 🚀

Ensuite, **chaque `git push` met le site à jour automatiquement**.
Pour changer une clé plus tard : Vercel → ton projet → **Settings → Environment Variables**, puis **Deployments → Redeploy**.

---

## 4. ➕ Ajouter un nouveau mode

Tout se passe dans **un seul fichier** : `lib/modes.ts`.

1. Ouvre `lib/modes.ts`.
2. Dans la liste `MODES`, copie un bloc existant (de `{` jusqu'à `},`) et colle-le juste après.
3. Modifie les champs :

```ts
  {
    id: "tunisien",                       // unique, minuscules, sans espace ni accent
    name: "Tounsi",                       // nom du bouton
    icon: "🇹🇳",                          // un emoji
    description: "Le dialecte tunisien",  // petite phrase sous le nom
    placeholder: "Ex : Comment on dit « merci » ?",
    welcome: "Aslema ! 😄 Je t'apprends le tunisien. Demande-moi un mot !",
    systemPrompt: `
${REGLES_RIGOLO}
Langue enseignée : le dialecte tunisien (derja tounsi).
Ton style : un ami tunisien joyeux et accueillant.
Tu glisses naturellement des expressions comme "barcha" (beaucoup), "behi" (bien).
`,
  },
```

4. Enregistre : le nouveau bouton apparaît tout de suite sur le site.

**Astuces :**

- Pour un mode « Rigolo » qui enseigne une langue, commence le `systemPrompt` par `${REGLES_RIGOLO}`. Il hérite ainsi des règles communes : format mot / prononciation / traduction / exemple, et ton bienveillant, jamais moqueur.
- Pour un mode totalement différent (ex : « Aide aux devoirs d'histoire »), écris ton propre `systemPrompt`. Décris le rôle de l'IA, sa méthode, son ton, et ce qu'elle doit refuser.
- Le premier mode de la liste est celui affiché par défaut.
- Les `systemPrompt` restent sur le serveur : les visiteurs ne peuvent pas les lire.

---

## 5. Où trouver quoi ?

| Fichier | Rôle |
|---|---|
| `lib/modes.ts` | **Les modes** (nom, icône, prompt). Le fichier à modifier le plus souvent. |
| `lib/limits.ts` | Les réglages de sécurité : taille max d'un message, longueur des réponses, etc. |
| `lib/rate-limit.ts` | La limite de messages par visiteur. |
| `lib/ai/index.ts` | Choisit le fournisseur d'IA selon `AI_PROVIDER`. |
| `lib/ai/gemini.ts`, `lib/ai/claude.ts` | Le code qui parle à chaque IA. |
| `app/api/chat/route.ts` | La route serveur appelée par le chat. Elle vérifie tout, puis renvoie la réponse en streaming. |
| `components/Chat.tsx` | L'interface du chat : boutons de mode, messages, champ de saisie. |
| `app/globals.css` | Les couleurs du site (mode clair et mode sombre). |
| `.env.example` | Le modèle du fichier de clés. |

### Réglages utiles (dans `.env.local` ou sur Vercel)

| Variable | Par défaut | Rôle |
|---|---|---|
| `AI_PROVIDER` | `gemini` | `gemini` ou `claude` |
| `GEMINI_MODEL` | `gemini-3.5-flash-lite` | Modèle Gemini (ex : `gemini-3.8-flash`, plus fort en maths) |
| `CLAUDE_MODEL` | `claude-haiku-5-5` | Modèle Claude |
| `RATE_LIMIT_MAX` | `20` | Messages autorisés par visiteur… |
| `RATE_LIMIT_WINDOW_MINUTES` | `10` | …pendant cette durée |

---

## 6. Commandes utiles

| Commande | Effet |
|---|---|
| `npm run dev` | Lance le site en local, sur http://localhost:3000 |
| `npm run build` | Vérifie que tout compile, comme sur Vercel |
| `npm run lint` | Cherche les erreurs courantes dans le code |

---

## 7. Aller plus loin (plus tard)

- **Limite de messages stricte** : la limite actuelle est gardée en mémoire. Sur Vercel, plusieurs serveurs peuvent tourner en même temps, et chacun compte de son côté. Pour une limite exacte, utilise [Upstash Redis](https://upstash.com) (offre gratuite) avec le paquet `@upstash/ratelimit`, et remplace le contenu de `checkRateLimit` dans `lib/rate-limit.ts`.
- **Plafond de dépenses** : si tu passes à Claude, fixe une limite mensuelle dans la console Anthropic (menu **Limits**).
- **Nouveau fournisseur d'IA** : copie `lib/ai/gemini.ts`, adapte-le, puis ajoute une ligne dans `PROVIDERS` (fichier `lib/ai/index.ts`).

---

## ❓ Problèmes fréquents

| Message | Solution |
|---|---|
| « La clé GEMINI_API_KEY est absente » | Crée `.env.local` (étape 1.d), colle ta clé, puis relance `npm run dev`. |
| « Problème de configuration de l'IA » | La clé est fausse ou le nom du modèle n'existe pas. Vérifie `.env.local`. |
| « Le quota gratuit de l'IA est atteint » | Gemini limite le nombre de requêtes gratuites : attends une minute ou essaie demain. |
| `npm` n'est pas reconnu | Node.js n'est pas installé : refais l'étape 1.a, puis redémarre PowerShell. |
| Erreur de script sur PowerShell | Lance d'abord : `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`, puis réponds `O` (ou `Y`). |
