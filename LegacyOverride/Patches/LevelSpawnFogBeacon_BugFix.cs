using HarmonyLib;
using LEGACY.LegacyOverride.FogBeacon;
using LEGACY.Utils;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal static class LevelSpawnFogBeacon_BugFix
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HeavyFogRepellerGlobalState), nameof(HeavyFogRepellerGlobalState.AttemptInteract))]
        private static void Post_HeavyFogRepellerGlobalState_AttemptInteract(HeavyFogRepellerGlobalState __instance)
        {
            var LSFBDef = LevelSpawnedFogBeaconManager.Current.GetLSFBDef(__instance);
            if (LSFBDef == null) return;

            __instance.m_repellerSphere.Range = LSFBDef.Range;
        }

        //[HarmonyPrefix]
        //[HarmonyWrapSafe]
        //[HarmonyPatch(typeof(HeavyFogRepellerGlobalState), nameof(HeavyFogRepellerGlobalState.OnStateChange))]
        //private static bool Pre_OnStateChanged(HeavyFogRepellerGlobalState __instance, 
        //    pCarryItemWithGlobalState_State oldState, pCarryItemWithGlobalState_State newState, bool isDropinState)
        //{
        //    if(!isDropinState) return true;

        //    var def = LevelSpawnedFogBeaconManager.Current.GetLSFBDef(__instance);
        //    if(def == null) return true;

        //    eHeavyFogRepellerStatus status = (eHeavyFogRepellerStatus)newState.status;

        //    switch (status)
        //    {
        //        case eHeavyFogRepellerStatus.Activated:
        //            __instance.m_repellerSphere.StartRepelling();
        //            LegacyLogger.Warning("Start repelling please :)");
        //            break;
        //        default:
        //            LegacyLogger.Warning($"killed, state: {status}");
        //            __instance.m_repellerSphere.KillRepellerInstantly();
        //            break;
        //    }

        //    return false;
        //}
    }
}
