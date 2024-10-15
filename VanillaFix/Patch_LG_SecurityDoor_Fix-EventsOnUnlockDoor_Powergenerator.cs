using HarmonyLib;
using LevelGeneration;
using GameData;
using ChainedPuzzles;
using LEGACY.Utils;
using GTFO.API;
using FloLib.Utils.Extensions;

namespace LEGACY.VanillaFix
{
    [HarmonyPatch]
    class Patch_LG_SecurityDoor_Fix_EventsOnUnlockDoor_Powergenerator
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_SecurityDoor), nameof(LG_SecurityDoor.OnSyncDoorStatusChange))]
        private static void Pre_OnSyncDoorStatusChange(LG_SecurityDoor __instance, pDoorState state, bool isRecall)
        {
            switch (state.status)
            {
                case eDoorStatus.Closed_LockedWithChainedPuzzle:
                case eDoorStatus.Unlocked:
                case eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm:
                    if (__instance.m_lastState.status == eDoorStatus.Closed_LockedWithPowerGenerator && !isRecall)
                        WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(__instance.LinkedToZoneData.EventsOnUnlockDoor, eWardenObjectiveEventTrigger.None, true, 0.0f);
                    //Utils.CheckAndExecuteEventsOnTrigger(__instance.LinkedToZoneData.EventsOnUnlockDoor, eWardenObjectiveEventTrigger.None, true, 0.0f);
                    return;
            }
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(CP_Holopath_Spline), nameof(CP_Holopath_Spline.Setup))]
        //private static void Post_test(CP_Holopath_Spline __instance)
        //{

        //    LegacyLogger.Warning($"{__instance.m_splineGeneratorPrefab.}");
        //}
    }
}
