"use client";

import { useEffect, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import type { PublicMode } from "@/lib/modes";

type Message = { role: "user" | "assistant"; content: string };

type Props = {
  modes: PublicMode[];
  maxChars: number;
};

export default function Chat({ modes, maxChars }: Props) {
  const [modeId, setModeId] = useState(modes[0].id);
  // Une conversation séparée pour chaque mode.
  const [conversations, setConversations] = useState<Record<string, Message[]>>({});
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const abortRef = useRef<AbortController | null>(null);
  const bottomRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);

  const mode = modes.find((m) => m.id === modeId) ?? modes[0];
  const messages = conversations[modeId] ?? [];

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
  }, [conversations, modeId, loading]);

  function setMessagesFor(id: string, update: (prev: Message[]) => Message[]) {
    setConversations((all) => ({ ...all, [id]: update(all[id] ?? []) }));
  }

  async function send(text: string) {
    const content = text.trim();
    if (!content || loading) return;
    if (content.length > maxChars) {
      setError(`Ton message est trop long (maximum ${maxChars} caractères).`);
      return;
    }

    const currentMode = modeId;
    const history: Message[] = [...messages, { role: "user", content }];
    setMessagesFor(currentMode, () => [...history, { role: "assistant", content: "" }]);
    setInput("");
    setError(null);
    setLoading(true);

    const controller = new AbortController();
    abortRef.current = controller;

    try {
      const res = await fetch("/api/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ modeId: currentMode, messages: history }),
        signal: controller.signal,
      });

      if (!res.ok || !res.body) {
        const data = await res.json().catch(() => null);
        throw new Error(data?.error ?? "Une erreur est survenue. Réessaie.");
      }

      // Lecture du streaming : on ajoute le texte au fur et à mesure.
      const reader = res.body.getReader();
      const decoder = new TextDecoder();
      let answer = "";
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        answer += decoder.decode(value, { stream: true });
        setMessagesFor(currentMode, (prev) => [
          ...prev.slice(0, -1),
          { role: "assistant", content: answer },
        ]);
      }
      if (!answer.trim()) throw new Error("L'IA n'a rien répondu. Réessaie.");
    } catch (e) {
      const aborted = e instanceof DOMException && e.name === "AbortError";
      // On retire la bulle vide de l'IA si elle n'a rien écrit.
      setMessagesFor(currentMode, (prev) => {
        const last = prev[prev.length - 1];
        return last?.role === "assistant" && !last.content ? prev.slice(0, -1) : prev;
      });
      if (!aborted) {
        setError(e instanceof Error ? e.message : "Une erreur est survenue.");
        // On remet le message dans le champ pour pouvoir le renvoyer.
        setMessagesFor(currentMode, (prev) => {
          const last = prev[prev.length - 1];
          return last?.role === "user" ? prev.slice(0, -1) : prev;
        });
        setInput(content);
      }
    } finally {
      setLoading(false);
      abortRef.current = null;
      inputRef.current?.focus();
    }
  }

  function stop() {
    abortRef.current?.abort();
  }

  function changeMode(id: string) {
    if (loading) stop();
    setModeId(id);
    setError(null);
  }

  function resetConversation() {
    if (loading) stop();
    setMessagesFor(modeId, () => []);
    setError(null);
  }

  const remaining = maxChars - input.length;

  return (
    <div className="flex h-dvh flex-col">
      {/* En-tête + sélecteur de mode */}
      <header className="border-b border-line bg-surface">
        <div className="mx-auto flex max-w-3xl items-center justify-between gap-3 px-4 pt-3">
          <h1 className="text-lg font-bold">
            <span aria-hidden>✨</span> Mon IA
          </h1>
          {messages.length > 0 && (
            <button
              onClick={resetConversation}
              className="rounded-full px-3 py-1 text-sm text-muted hover:bg-chip"
            >
              Nouvelle discussion
            </button>
          )}
        </div>
        <nav
          aria-label="Choisir un mode"
          className="mx-auto flex max-w-3xl gap-2 overflow-x-auto px-4 py-3"
        >
          {modes.map((m) => {
            const active = m.id === modeId;
            return (
              <button
                key={m.id}
                onClick={() => changeMode(m.id)}
                aria-pressed={active}
                className={`flex shrink-0 items-center gap-2 rounded-2xl border px-3 py-2 text-left transition ${
                  active
                    ? "border-accent bg-accent text-on-accent"
                    : "border-line bg-chip hover:border-accent"
                }`}
              >
                <span className="text-xl" aria-hidden>
                  {m.icon}
                </span>
                <span>
                  <span className="block text-sm font-semibold leading-tight">{m.name}</span>
                  <span className={`block text-xs ${active ? "opacity-90" : "text-muted"}`}>
                    {m.description}
                  </span>
                </span>
              </button>
            );
          })}
        </nav>
      </header>

      {/* Messages */}
      <main className="flex-1 overflow-y-auto">
        <div className="mx-auto flex max-w-3xl flex-col gap-4 px-4 py-6">
          <Bubble role="assistant" content={mode.welcome} icon={mode.icon} />
          {messages.map((m, i) => (
            <Bubble
              key={i}
              role={m.role}
              content={m.content}
              icon={mode.icon}
              typing={loading && i === messages.length - 1 && m.role === "assistant" && !m.content}
            />
          ))}
          <div ref={bottomRef} />
        </div>
      </main>

      {/* Zone de saisie */}
      <footer className="border-t border-line bg-surface">
        <form
          className="mx-auto max-w-3xl px-4 py-3"
          onSubmit={(e) => {
            e.preventDefault();
            send(input);
          }}
        >
          {error && (
            <p role="alert" className="mb-2 rounded-xl bg-error-bg px-3 py-2 text-sm text-error">
              {error}
            </p>
          )}
          <div className="flex items-end gap-2">
            <textarea
              ref={inputRef}
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={(e) => {
                // Entrée = envoyer, Maj+Entrée = retour à la ligne.
                if (e.key === "Enter" && !e.shiftKey && !e.nativeEvent.isComposing) {
                  e.preventDefault();
                  send(input);
                }
              }}
              placeholder={mode.placeholder}
              rows={1}
              maxLength={maxChars}
              aria-label="Ton message"
              className="max-h-40 min-h-12 flex-1 resize-none rounded-2xl border border-line bg-background px-4 py-3 text-base outline-none focus:border-accent"
            />
            {loading ? (
              <button
                type="button"
                onClick={stop}
                className="h-12 shrink-0 rounded-2xl border border-line px-4 font-semibold hover:bg-chip"
              >
                Stop
              </button>
            ) : (
              <button
                type="submit"
                disabled={!input.trim()}
                className="h-12 shrink-0 rounded-2xl bg-accent px-5 font-semibold text-on-accent disabled:opacity-40"
              >
                Envoyer
              </button>
            )}
          </div>
          <p className="mt-1 flex justify-between text-xs text-muted">
            <span>L&apos;IA peut se tromper : vérifie les infos importantes.</span>
            <span className={remaining < 100 ? "text-error" : ""}>{remaining}</span>
          </p>
        </form>
      </footer>
    </div>
  );
}

function Bubble({
  role,
  content,
  icon,
  typing = false,
}: {
  role: "user" | "assistant";
  content: string;
  icon: string;
  typing?: boolean;
}) {
  if (role === "user") {
    return (
      <div className="flex justify-end">
        <div className="max-w-[85%] whitespace-pre-wrap break-words rounded-2xl rounded-br-md bg-accent px-4 py-2.5 text-on-accent">
          {content}
        </div>
      </div>
    );
  }
  return (
    <div className="flex gap-2">
      <span className="mt-1 text-xl" aria-hidden>
        {icon}
      </span>
      <div className="markdown min-w-0 max-w-[85%] rounded-2xl rounded-bl-md bg-chip px-4 py-2.5">
        {typing ? (
          <span className="typing" aria-label="L'IA écrit">
            <span />
            <span />
            <span />
          </span>
        ) : (
          <ReactMarkdown remarkPlugins={[remarkGfm]}>{content}</ReactMarkdown>
        )}
      </div>
    </div>
  );
}
