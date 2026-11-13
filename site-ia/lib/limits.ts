// Réglages de sécurité. Tu peux les modifier ici.

/** Nombre maximum de caractères dans un message envoyé par un visiteur. */
export const MAX_MESSAGE_CHARS = 1000;

/** Nombre maximum de messages d'historique envoyés à l'IA (les plus récents). */
export const MAX_HISTORY_MESSAGES = 20;

/** Longueur maximale de la réponse de l'IA (en "tokens", environ 3/4 de mot). */
export const MAX_OUTPUT_TOKENS = 2048;

/** Rate limiting : nombre de messages autorisés par visiteur... */
export const RATE_LIMIT_MAX = Number(process.env.RATE_LIMIT_MAX ?? 20);

/** ...sur cette durée (en minutes). */
export const RATE_LIMIT_WINDOW_MINUTES = Number(
  process.env.RATE_LIMIT_WINDOW_MINUTES ?? 10,
);
