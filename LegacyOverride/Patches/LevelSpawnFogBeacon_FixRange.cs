using HarmonyLib;
using LEGACY.LegacyOverride.FogBeacon;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal static class LevelSpawnFogBeacon_FixRange
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HeavyFogRepellerGlobalState), nameof(HeavyFogRepellerGlobalState.AttemptInteract))]
        private static void Post_HeavyFogRepellerGlobalState_AttemptInteract(HeavyFogRepellerGlobalState __instance)
        {
            var LSFBDef = LevelSpawnedFogBeaconManager.Current.GetLSFBDef(__instance);
            if (LSFBDef == null) return;

            __instance.m_repellerSphere.Range = LSFBDef.Range;
        }
    }
}
