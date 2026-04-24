using BattleSim.Gameplay;
using UnityEngine;

namespace BattleSim.Core
{
    public static class BattleBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateRuntimeBattleManager()
        {
            if (Object.FindAnyObjectByType<BattleManager>() != null)
            {
                return;
            }

            GameObject root = new GameObject("BattleManager_Runtime");
            root.AddComponent<BattleManager>();
        }
    }
}
