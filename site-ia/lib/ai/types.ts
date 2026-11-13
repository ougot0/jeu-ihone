// Format commun à tous les fournisseurs d'IA.

export type ChatMessage = {
  role: "user" | "assistant";
  content: string;
};

export type ChatRequest = {
  systemPrompt: string;
  messages: ChatMessage[];
  maxOutputTokens: number;
};

/**
 * Un fournisseur d'IA = une fonction qui reçoit la conversation
 * et renvoie le texte de la réponse morceau par morceau (streaming).
 */
export type AIProvider = {
  name: string;
  streamChat: (request: ChatRequest) => AsyncGenerator<string>;
};

/** Erreur "propre" avec un message à afficher au visiteur. */
export class AIError extends Error {
  constructor(
    public publicMessage: string,
    public status: number = 502,
  ) {
    super(publicMessage);
  }
}
