// Choisit le fournisseur d'IA selon la variable AI_PROVIDER de .env.local.
//   AI_PROVIDER=gemini  -> Google Gemini (par défaut)
//   AI_PROVIDER=claude  -> Anthropic Claude
// Pour ajouter un autre fournisseur : crée un fichier comme gemini.ts
// et ajoute une ligne dans PROVIDERS ci-dessous.

import { createClaudeProvider } from "./claude";
import { createGeminiProvider } from "./gemini";
import { AIError, type AIProvider } from "./types";

const PROVIDERS: Record<string, () => AIProvider> = {
  gemini: createGeminiProvider,
  claude: createClaudeProvider,
};

let cached: AIProvider | null = null;

export function getAIProvider(): AIProvider {
  if (cached) return cached;
  const name = (process.env.AI_PROVIDER || "gemini").toLowerCase().trim();
  const factory = PROVIDERS[name];
  if (!factory) {
    throw new AIError(
      `AI_PROVIDER="${name}" est inconnu. Valeurs possibles : ${Object.keys(PROVIDERS).join(", ")}.`,
      500,
    );
  }
  cached = factory();
  return cached;
}

export { AIError } from "./types";
export type { ChatMessage } from "./types";
