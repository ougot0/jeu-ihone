import Chat from "@/components/Chat";
import { MAX_MESSAGE_CHARS } from "@/lib/limits";
import { getPublicModes } from "@/lib/modes";

// Cette page tourne sur le serveur : elle n'envoie au navigateur
// que les infos publiques des modes (jamais les prompts système).
export default function Home() {
  return <Chat modes={getPublicModes()} maxChars={MAX_MESSAGE_CHARS} />;
}
