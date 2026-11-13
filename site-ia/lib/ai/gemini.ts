// Fournisseur : Google Gemini (offre gratuite disponible).
import { ApiError, GoogleGenAI } from "@google/genai";
import { AIError, type AIProvider } from "./types";

export function createGeminiProvider(): AIProvider {
  const apiKey = process.env.GEMINI_API_KEY;
  if (!apiKey) {
    throw new AIError(
      "La clé GEMINI_API_KEY est absente. Ajoute-la dans le fichier .env.local (voir README).",
      500,
    );
  }
  const model = process.env.GEMINI_MODEL || "gemini-3.5-flash-lite";
  const ai = new GoogleGenAI({ apiKey });

  return {
    name: `Gemini (${model})`,
    async *streamChat({ systemPrompt, messages, maxOutputTokens }) {
      try {
        const stream = await ai.models.generateContentStream({
          model,
          // Gemini appelle le rôle de l'IA "model" au lieu de "assistant".
          contents: messages.map((m) => ({
            role: m.role === "assistant" ? "model" : "user",
            parts: [{ text: m.content }],
          })),
          config: {
            systemInstruction: systemPrompt,
            maxOutputTokens,
          },
        });
        for await (const chunk of stream) {
          if (chunk.text) yield chunk.text;
        }
      } catch (error) {
        throw toAIError(error);
      }
    },
  };
}

function toAIError(error: unknown): AIError {
  if (error instanceof AIError) return error;
  if (error instanceof ApiError) {
    console.error("[Gemini]", error.status, error.message);
    if (error.status === 429)
      return new AIError("Le quota gratuit de l'IA est atteint pour le moment. Réessaie dans une minute.", 429);
    if (error.status === 400 || error.status === 401 || error.status === 403)
      return new AIError("Problème de configuration de l'IA (clé API ou modèle invalide).", 500);
    return new AIError("L'IA est indisponible pour le moment. Réessaie un peu plus tard.", 503);
  }
  console.error("[Gemini]", error);
  return new AIError("Impossible de joindre l'IA. Réessaie un peu plus tard.", 503);
}
