// Fournisseur : Anthropic Claude (Claude Haiku par défaut, rapide et peu cher).
import Anthropic from "@anthropic-ai/sdk";
import { AIError, type AIProvider } from "./types";

export function createClaudeProvider(): AIProvider {
  const apiKey = process.env.ANTHROPIC_API_KEY;
  if (!apiKey) {
    throw new AIError(
      "La clé ANTHROPIC_API_KEY est absente. Ajoute-la dans le fichier .env.local (voir README).",
      500,
    );
  }
  const model = process.env.CLAUDE_MODEL || "claude-haiku-5-5";
  const client = new Anthropic({ apiKey });

  return {
    name: `Claude (${model})`,
    async *streamChat({ systemPrompt, messages, maxOutputTokens }) {
      try {
        const stream = client.messages.stream({
          model,
          max_tokens: maxOutputTokens,
          system: systemPrompt,
          // "low" = réponses rapides et économiques, suffisant pour un chat.
          output_config: { effort: "low" },
          messages,
        });
        for await (const event of stream) {
          if (event.type === "content_block_delta" && event.delta.type === "text_delta") {
            yield event.delta.text;
          }
        }
        const final = await stream.finalMessage();
        if (final.stop_reason === "refusal") {
          yield "\n\n_(Je ne peux pas répondre à cette demande. Essaie une autre question !)_";
        }
      } catch (error) {
        throw toAIError(error);
      }
    },
  };
}

function toAIError(error: unknown): AIError {
  if (error instanceof AIError) return error;
  if (error instanceof Anthropic.AuthenticationError || error instanceof Anthropic.PermissionDeniedError) {
    console.error("[Claude] clé invalide", error.message);
    return new AIError("Problème de configuration de l'IA (clé API invalide).", 500);
  }
  if (error instanceof Anthropic.BadRequestError || error instanceof Anthropic.NotFoundError) {
    console.error("[Claude] requête invalide", error.message);
    return new AIError("Problème de configuration de l'IA (modèle ou paramètres invalides).", 500);
  }
  if (error instanceof Anthropic.RateLimitError) {
    return new AIError("L'IA reçoit trop de demandes. Réessaie dans une minute.", 429);
  }
  if (error instanceof Anthropic.APIError) {
    console.error("[Claude]", error.status, error.message);
    return new AIError("L'IA est indisponible pour le moment. Réessaie un peu plus tard.", 503);
  }
  console.error("[Claude]", error);
  return new AIError("Impossible de joindre l'IA. Réessaie un peu plus tard.", 503);
}
