// Route API côté serveur : POST /api/chat
// C'est ici (et seulement ici) que la clé API est utilisée.
// Le navigateur envoie { modeId, messages } et reçoit la réponse en streaming.

import { AIError, getAIProvider, type ChatMessage } from "@/lib/ai";
import { MAX_HISTORY_MESSAGES, MAX_MESSAGE_CHARS, MAX_OUTPUT_TOKENS } from "@/lib/limits";
import { getMode } from "@/lib/modes";
import { checkRateLimit, getVisitorId } from "@/lib/rate-limit";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

function errorResponse(message: string, status: number, headers: HeadersInit = {}) {
  return Response.json({ error: message }, { status, headers });
}

export async function POST(request: Request) {
  // 1. Lire et vérifier le corps de la requête.
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return errorResponse("Requête invalide.", 400);
  }
  const { modeId, messages } = (body ?? {}) as { modeId?: unknown; messages?: unknown };

  const mode = typeof modeId === "string" ? getMode(modeId) : undefined;
  if (!mode) return errorResponse("Mode inconnu.", 400);

  if (!Array.isArray(messages) || messages.length === 0) {
    return errorResponse("Aucun message reçu.", 400);
  }

  const cleaned: ChatMessage[] = [];
  for (const m of messages) {
    if (
      !m ||
      (m.role !== "user" && m.role !== "assistant") ||
      typeof m.content !== "string" ||
      m.content.trim() === ""
    ) {
      return errorResponse("Format de message invalide.", 400);
    }
    // Les réponses de l'IA peuvent être longues ; les messages du visiteur non.
    const limit = m.role === "user" ? MAX_MESSAGE_CHARS : MAX_MESSAGE_CHARS * 8;
    if (m.content.length > limit) {
      return errorResponse(`Message trop long (maximum ${MAX_MESSAGE_CHARS} caractères).`, 413);
    }
    cleaned.push({ role: m.role, content: m.content });
  }

  // On ne garde que les derniers messages, en commençant par un message du visiteur.
  let history = cleaned.slice(-MAX_HISTORY_MESSAGES);
  while (history.length > 0 && history[0].role !== "user") history = history.slice(1);
  if (history.length === 0 || history[history.length - 1].role !== "user") {
    return errorResponse("Le dernier message doit venir de toi.", 400);
  }

  // 2. Rate limiting (après la validation, pour ne compter que les vrais messages).
  const limit = checkRateLimit(getVisitorId(request.headers));
  if (!limit.ok) {
    const minutes = Math.ceil(limit.retryAfterSeconds / 60);
    return errorResponse(
      `Tu as envoyé beaucoup de messages ! Réessaie dans ${minutes} minute${minutes > 1 ? "s" : ""}. ⏳`,
      429,
      { "Retry-After": String(limit.retryAfterSeconds) },
    );
  }

  // 3. Préparer l'IA (erreur claire si la clé manque).
  let provider;
  try {
    provider = getAIProvider();
  } catch (error) {
    const e = error instanceof AIError ? error : new AIError("Erreur de configuration.", 500);
    console.error("[chat]", e.message);
    return errorResponse(e.publicMessage, e.status);
  }

  const generator = provider.streamChat({
    systemPrompt: mode.systemPrompt,
    messages: history,
    maxOutputTokens: MAX_OUTPUT_TOKENS,
  });

  // 4. On attend le premier morceau : si l'IA échoue tout de suite,
  //    on peut encore renvoyer une vraie erreur HTTP au navigateur.
  let first: IteratorResult<string>;
  try {
    first = await generator.next();
  } catch (error) {
    const e = error instanceof AIError ? error : new AIError("Erreur inattendue de l'IA.", 500);
    return errorResponse(e.publicMessage, e.status);
  }

  // 5. Envoyer la suite en streaming (texte brut, morceau par morceau).
  const encoder = new TextEncoder();
  const stream = new ReadableStream<Uint8Array>({
    async start(controller) {
      try {
        if (!first.done) controller.enqueue(encoder.encode(first.value));
        if (!first.done) {
          for await (const piece of generator) controller.enqueue(encoder.encode(piece));
        }
      } catch (error) {
        const message =
          error instanceof AIError ? error.publicMessage : "La réponse a été interrompue.";
        controller.enqueue(encoder.encode(`\n\n⚠️ ${message}`));
      } finally {
        controller.close();
      }
    },
    cancel() {
      // Le visiteur a fermé la page : on arrête de lire l'IA.
      void generator.return(undefined);
    },
  });

  return new Response(stream, {
    headers: {
      "Content-Type": "text/plain; charset=utf-8",
      "Cache-Control": "no-store",
      "X-RateLimit-Remaining": String(limit.remaining),
    },
  });
}
