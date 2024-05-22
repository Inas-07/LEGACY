using HarmonyLib;
using LEGACY.Utils;
using LevelGeneration;
using LEGACY.LegacyOverride.ForceFail;
//using LEGACY.LegacyOverride.Terminal;

namespace LEGACY.ExtraEvents
{
    [HarmonyPatch]
    internal class LegacyExtraEventsPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WardenObjectiveManager), nameof(WardenObjectiveManager.CheckExpeditionFailed))]
        private static bool Pre_CheckExpeditionFailed(WardenObjectiveManager __instance, ref bool __result)
        {
            if (!ForceFailManager.Current.IsCheckEnabled()) return true;

            bool forceFailed = ForceFailManager.Current.CheckLevelForceFailed(); 
            if(forceFailed)
            {
                __result = true;
                LegacyLogger.Debug("Condition satisfied - Force failed");
                return false; // negate vanilla check
            }
            else
            {
                return true; // turn to vanilla check
            }
        }
    }
}
