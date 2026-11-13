"use client";

// Conversation vocale "mains libres" :
//   1. le micro écoute ce que tu dis,
//   2. le texte est envoyé à l'IA,
//   3. la réponse est lue à voix haute, phrase par phrase, dès qu'elle arrive,
//   4. puis le micro se rallume tout seul pour la suite.
// Touche le gros bouton pour interrompre l'IA ou pour dire « j'ai fini ».

import { useEffect, useRef, useState } from "react";
import {
  cleanForSpeech,
  getSpeechRecognition,
  pickFrenchVoice,
  takeSentences,
  type SpeechRecognitionLike,
} from "@/lib/voice";

type Status = "listening" | "thinking" | "speaking" | "paused";

type Props = {
  modeName: string;
  icon: string;
  /** Message d'erreur venant du chat (quota, réseau…). */
  error: string | null;
  ask: (text: string, onChunk: (chunk: string) => void) => Promise<string | null>;
  stopAnswer: () => void;
  onClose: () => void;
};

/** Après ce nombre de silences d'affilée, on met le micro en pause. */
const MAX_SILENT_RESTARTS = 2;

const LABELS: Record<Status, string> = {
  listening: "Je t'écoute…",
  thinking: "Je réfléchis…",
  speaking: "Je parle… (touche pour me couper)",
  paused: "Touche le bouton pour parler",
};

export default function VoiceConversation({ modeName, icon, error, ask, stopAnswer, onClose }: Props) {
  const [status, setStatus] = useState<Status>("listening");
  const [heard, setHeard] = useState("");
  const [said, setSaid] = useState("");
  const [problem, setProblem] = useState<string | null>(null);

  const activeRef = useRef(false);
  const recRef = useRef<SpeechRecognitionLike | null>(null);
  const turnRef = useRef(0); // numéro du tour de parole (pour ignorer les anciens)
  const pendingRef = useRef(0); // phrases en attente de lecture
  const streamDoneRef = useRef(true);
  const silentRef = useRef(0);
  const statusRef = useRef<Status>("listening");
  const voiceRef = useRef<SpeechSynthesisVoice | null>(null);
  const askRef = useRef(ask);
  askRef.current = ask;
  const stopRef = useRef(stopAnswer);
  stopRef.current = stopAnswer;

  function changeStatus(s: Status) {
    statusRef.current = s;
    setStatus(s);
  }

  // ---------- Lecture à voix haute ----------
  function speak(text: string, turn: number) {
    const clean = cleanForSpeech(text);
    if (!clean) return;
    const utterance = new SpeechSynthesisUtterance(clean);
    utterance.lang = "fr-FR";
    if (voiceRef.current) utterance.voice = voiceRef.current;
    utterance.rate = 1.05;
    pendingRef.current += 1;
    utterance.onstart = () => {
      if (turn !== turnRef.current) return;
      changeStatus("speaking");
      setSaid(clean);
    };
    const finished = () => {
      if (turn !== turnRef.current) return;
      pendingRef.current -= 1;
      listenAgainIfDone(turn);
    };
    utterance.onend = finished;
    utterance.onerror = finished;
    window.speechSynthesis.speak(utterance);
  }

  function listenAgainIfDone(turn: number) {
    if (turn !== turnRef.current || !activeRef.current) return;
    if (streamDoneRef.current && pendingRef.current <= 0) listen();
  }

  // ---------- Écoute du micro ----------
  function listen() {
    if (!activeRef.current) return;
    const Recognition = getSpeechRecognition();
    if (!Recognition) return;

    recRef.current?.abort();
    const rec = new Recognition();
    rec.lang = "fr-FR";
    rec.continuous = false;
    rec.interimResults = true;

    let finalText = "";
    let fatal = false;

    rec.onresult = (event) => {
      let interim = "";
      for (let i = event.resultIndex; i < event.results.length; i++) {
        const result = event.results[i];
        if (result.isFinal) finalText += result[0].transcript;
        else interim += result[0].transcript;
      }
      setHeard((finalText + " " + interim).trim());
    };

    rec.onerror = (event) => {
      if (event.error === "not-allowed" || event.error === "service-not-allowed") {
        fatal = true;
        setProblem("Autorise l'accès au micro (icône 🔒 ou 🎙️ dans la barre d'adresse), puis réessaie.");
      } else if (event.error === "audio-capture") {
        fatal = true;
        setProblem("Aucun micro n'a été trouvé sur cet appareil.");
      } else if (event.error === "network") {
        fatal = true;
        setProblem("La reconnaissance vocale a besoin d'une connexion internet.");
      }
      // "no-speech" et "aborted" sont normaux : on gère dans onend.
    };

    rec.onend = () => {
      if (recRef.current !== rec) return; // un ancien micro, on l'ignore
      recRef.current = null;
      if (!activeRef.current) return;

      const text = finalText.trim();
      if (text) {
        silentRef.current = 0;
        void handleUserText(text);
      } else if (!fatal && statusRef.current === "listening" && silentRef.current < MAX_SILENT_RESTARTS) {
        silentRef.current += 1;
        listen(); // silence : on réécoute
      } else {
        changeStatus("paused");
      }
    };

    recRef.current = rec;
    setHeard("");
    setProblem(null);
    changeStatus("listening");
    try {
      rec.start();
    } catch {
      changeStatus("paused");
    }
  }

  // ---------- Envoi à l'IA ----------
  async function handleUserText(text: string) {
    const turn = ++turnRef.current;
    pendingRef.current = 0;
    streamDoneRef.current = false;
    changeStatus("thinking");
    setSaid("");

    let buffer = "";
    const answer = await askRef.current(text, (chunk) => {
      if (turn !== turnRef.current) return;
      buffer += chunk;
      const { sentences, rest } = takeSentences(buffer);
      buffer = rest;
      for (const sentence of sentences) speak(sentence, turn);
    });

    if (turn !== turnRef.current) return; // interrompu entre-temps
    if (answer === null) {
      changeStatus("paused");
      return;
    }
    if (buffer.trim()) speak(buffer, turn);
    streamDoneRef.current = true;
    listenAgainIfDone(turn);
  }

  // ---------- Le gros bouton ----------
  function onOrbClick() {
    const s = statusRef.current;
    if (s === "listening") {
      recRef.current?.stop(); // « j'ai fini de parler »
      return;
    }
    // Interrompre l'IA (ou reprendre après une pause) et écouter.
    turnRef.current += 1;
    if (s === "thinking") stopRef.current();
    window.speechSynthesis.cancel();
    silentRef.current = 0;
    listen();
  }

  // Démarrage / arrêt du mode vocal.
  useEffect(() => {
    activeRef.current = true;
    const synth = window.speechSynthesis;
    voiceRef.current = pickFrenchVoice();
    const onVoices = () => {
      voiceRef.current = pickFrenchVoice();
    };
    synth.addEventListener("voiceschanged", onVoices);
    listen();

    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);

    return () => {
      activeRef.current = false;
      turnRef.current += 1;
      recRef.current?.abort();
      recRef.current = null;
      synth.cancel();
      synth.removeEventListener("voiceschanged", onVoices);
      window.removeEventListener("keydown", onKey);
      if (statusRef.current === "thinking") stopRef.current();
    };
    // listen et onClose ne doivent lancer l'effet qu'une seule fois.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const message = problem ?? (status === "paused" ? error : null);

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label="Conversation vocale"
      className="fixed inset-0 z-50 flex flex-col bg-background"
    >
      <div className="mx-auto flex w-full max-w-3xl items-center justify-between px-4 py-3">
        <p className="font-semibold">
          <span aria-hidden>{icon}</span> {modeName} · à voix haute
        </p>
        <button
          onClick={onClose}
          className="rounded-full border border-line px-4 py-2 text-sm font-semibold hover:bg-chip"
        >
          Terminer
        </button>
      </div>

      <div className="flex flex-1 flex-col items-center justify-center gap-8 px-6 text-center">
        <button
          onClick={onOrbClick}
          aria-label={LABELS[status]}
          className={`orb orb-${status} relative flex h-40 w-40 items-center justify-center rounded-full text-5xl`}
        >
          <span aria-hidden>{status === "thinking" ? "💭" : status === "speaking" ? icon : "🎙️"}</span>
        </button>

        <p aria-live="polite" className="text-lg font-semibold">
          {LABELS[status]}
        </p>

        <div className="min-h-24 max-w-xl space-y-3">
          {heard && (
            <p className="text-muted">
              <span className="font-semibold">Toi :</span> {heard}
            </p>
          )}
          {said && status === "speaking" && <p className="text-lg">{said}</p>}
          {message && (
            <p role="alert" className="rounded-xl bg-error-bg px-3 py-2 text-sm text-error">
              {message}
            </p>
          )}
        </div>
      </div>

      <p className="px-6 pb-6 text-center text-xs text-muted">
        Parle normalement, puis attends un instant : l&apos;IA répond toute seule. La conversation
        s&apos;affiche aussi dans le chat.
      </p>
    </div>
  );
}
