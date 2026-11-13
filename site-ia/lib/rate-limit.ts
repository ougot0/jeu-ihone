// Limite le nombre de messages par visiteur (identifié par son adresse IP).
//
// ⚠️ Cette version garde les compteurs en mémoire : c'est simple et gratuit,
// mais sur Vercel chaque serveur a sa propre mémoire, donc la limite n'est
// pas parfaitement stricte. Pour une limite stricte, voir le README
// (section « Aller plus loin » : Upstash Redis).

import { RATE_LIMIT_MAX, RATE_LIMIT_WINDOW_MINUTES } from "./limits";

const visits = new Map<string, number[]>();

export type RateLimitResult = {
  ok: boolean;
  remaining: number;
  /** Secondes à attendre avant de pouvoir réessayer (si ok = false). */
  retryAfterSeconds: number;
};

export function checkRateLimit(visitorId: string): RateLimitResult {
  const now = Date.now();
  const windowMs = RATE_LIMIT_WINDOW_MINUTES * 60 * 1000;

  // On garde seulement les visites récentes de ce visiteur.
  const recent = (visits.get(visitorId) ?? []).filter((t) => now - t < windowMs);

  if (recent.length >= RATE_LIMIT_MAX) {
    visits.set(visitorId, recent);
    const oldest = recent[0];
    return {
      ok: false,
      remaining: 0,
      retryAfterSeconds: Math.ceil((oldest + windowMs - now) / 1000),
    };
  }

  recent.push(now);
  visits.set(visitorId, recent);

  // Petit ménage pour ne pas remplir la mémoire indéfiniment.
  if (visits.size > 10_000) {
    for (const [id, times] of visits) {
      if (times.every((t) => now - t >= windowMs)) visits.delete(id);
    }
  }

  return { ok: true, remaining: RATE_LIMIT_MAX - recent.length, retryAfterSeconds: 0 };
}

/** Récupère l'adresse IP du visiteur à partir des en-têtes de la requête. */
export function getVisitorId(headers: Headers): string {
  const forwarded = headers.get("x-forwarded-for");
  if (forwarded) return forwarded.split(",")[0].trim();
  return headers.get("x-real-ip") ?? "local";
}
