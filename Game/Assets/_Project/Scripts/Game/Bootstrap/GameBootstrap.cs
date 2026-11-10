using Arsenal.Game.ArsenalScene;
using UnityEngine;

namespace Arsenal.Game.Bootstrap
{
    /// <summary>
    /// Démarrage automatique : appuyer sur Play dans n'importe quelle scène vide (ou dans Scenes/Arsenal)
    /// lance l'arsenal. Aucun réglage manuel de scène n'est nécessaire.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnGameStart()
        {
            _ = GameServices.Instance;
            if (Object.FindAnyObjectByType<SceneControllerBase>() == null)
            {
                // Scène vide : on retire la caméra/lumière par défaut et on construit l'arsenal.
                foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) Object.Destroy(cam.gameObject);
                foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) Object.Destroy(light.gameObject);
                new GameObject("[ArsenalScene]").AddComponent<ArsenalSceneController>();
            }
        }
    }

    /// <summary>Base des contrôleurs de scène (arsenal, stand de tir, missions, AR...).</summary>
    public abstract class SceneControllerBase : MonoBehaviour { }
}
