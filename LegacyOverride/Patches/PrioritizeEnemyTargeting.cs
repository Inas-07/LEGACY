using Enemies;
using HarmonyLib;
using LEGACY.LegacyOverride.EnemyTargeting;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal class PrioritizeEnemyTargeting
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAgent), nameof(EnemyAgent.Setup))]
        private static void Post_Dam_SyncedDamageBase_Setup(EnemyAgent __instance)
        {
            var prioritizer = __instance.gameObject.AddComponent<EnemyTargetingPrioritizer>();
            prioritizer.enemy = __instance;
        }
    }
}
