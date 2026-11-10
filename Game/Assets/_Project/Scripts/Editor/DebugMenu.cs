using Arsenal.Game.Bootstrap;
using UnityEditor;
using UnityEngine;

namespace Arsenal.EditorTools
{
    /// <summary>
    /// Outils de test en mode Play (éditeur uniquement, jamais inclus dans le jeu) :
    /// simulent des gains pour tester boutique et déblocages avant l'arrivée des missions.
    /// </summary>
    public static class DebugMenu
    {
        [MenuItem("Arsenal/Debug/+5 000 crédits", priority = 200)]
        private static void AddMoney() => GameServices.Instance.Economy.GrantReward(5000, 0);

        [MenuItem("Arsenal/Debug/+1 niveau", priority = 201)]
        private static void AddLevel()
        {
            var gs = GameServices.Instance;
            int next = gs.Economy.Level + 1;
            int needed = gs.Economy.Progression.TotalXpForLevel(next) - gs.Profile.xp;
            gs.Economy.GrantReward(0, Mathf.Max(0, needed));
        }

        [MenuItem("Arsenal/Debug/Ouvrir le dossier de sauvegarde", priority = 220)]
        private static void OpenSaveFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath + "/saves/");

        [MenuItem("Arsenal/Debug/+5 000 crédits", true)]
        [MenuItem("Arsenal/Debug/+1 niveau", true)]
        private static bool PlayModeOnly() => Application.isPlaying;
    }
}
