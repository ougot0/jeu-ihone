// Outils pour la voix, utilisés dans le navigateur.
// - Reconnaissance vocale : on parle, le navigateur écrit le texte.
// - Synthèse vocale : le navigateur lit le texte à voix haute.
// Les deux sont intégrées aux navigateurs (Chrome, Edge, Safari) : gratuit, sans clé.

// --- Types minimaux pour la reconnaissance vocale (absents de TypeScript) ---
export type SpeechRecognitionLike = {
  lang: string;
  continuous: boolean;
  interimResults: boolean;
  onresult: ((event: SpeechRecognitionEventLike) => void) | null;
  onerror: ((event: { error: string }) => void) | null;
  onend: (() => void) | null;
  start: () => void;
  stop: () => void;
  abort: () => void;
};

export type SpeechRecognitionEventLike = {
  resultIndex: number;
  results: ArrayLike<{ isFinal: boolean; 0: { transcript: string } }>;
};

type SpeechRecognitionConstructor = new () => SpeechRecognitionLike;

export function getSpeechRecognition(): SpeechRecognitionConstructor | null {
  if (typeof window === "undefined") return null;
  const w = window as unknown as {
    SpeechRecognition?: SpeechRecognitionConstructor;
    webkitSpeechRecognition?: SpeechRecognitionConstructor;
  };
  return w.SpeechRecognition ?? w.webkitSpeechRecognition ?? null;
}

export function isVoiceSupported(): boolean {
  return getSpeechRecognition() !== null && typeof window.speechSynthesis !== "undefined";
}

/** Choisit la plus belle voix française disponible sur l'appareil. */
export function pickFrenchVoice(): SpeechSynthesisVoice | null {
  const voices = window.speechSynthesis.getVoices().filter((v) => v.lang.toLowerCase().startsWith("fr"));
  if (voices.length === 0) return null;
  const preferred = ["natural", "online", "google", "neural", "premium", "enhanced"];
  for (const word of preferred) {
    const found = voices.find((v) => v.name.toLowerCase().includes(word));
    if (found) return found;
  }
  return voices.find((v) => v.lang === "fr-FR") ?? voices[0];
}

/** Retire ce qui se lit mal à voix haute : Markdown, emojis, symboles. */
export function cleanForSpeech(text: string): string {
  return text
    .replace(/```[\s\S]*?```/g, " ")
    .replace(/[*_`#>~|]/g, "")
    .replace(/^\s*[-•]\s+/gm, "")
    .replace(/\p{Extended_Pictographic}|️|‍/gu, "")
    .replace(/→/g, ", ")
    .replace(/\s+/g, " ")
    .trim();
}

/**
 * Découpe le texte reçu en streaming en phrases complètes à lire.
 * Renvoie les phrases prêtes et le reste (phrase pas encore terminée).
 */
export function takeSentences(buffer: string): { sentences: string[]; rest: string } {
  const sentences: string[] = [];
  let start = 0;
  for (let i = 0; i < buffer.length - 1; i++) {
    const isEnd = ".!?…\n".includes(buffer[i]);
    // Fin de phrase = ponctuation suivie d'un espace (pour ne pas couper "3.14").
    if (isEnd && /\s/.test(buffer[i + 1])) {
      const sentence = buffer.slice(start, i + 1);
      if (sentence.trim()) sentences.push(sentence);
      start = i + 1;
    }
  }
  return { sentences, rest: buffer.slice(start) };
}

/**
 * Sur iPhone, la voix doit être "débloquée" par un clic de l'utilisateur.
 * On appelle cette fonction dans le clic du bouton micro.
 */
export function unlockSpeech() {
  try {
    const u = new SpeechSynthesisUtterance(" ");
    u.volume = 0;
    window.speechSynthesis.speak(u);
  } catch {
    // pas grave si ça échoue
  }
}
