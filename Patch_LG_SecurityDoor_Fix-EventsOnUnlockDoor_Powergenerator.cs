using HarmonyLib;
using LevelGeneration;
using GameData;

namespace LEGACY.Patch
{
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
                    if (__instance.m_lastState.status == eDoorStatus.Closed_LockedWithPowerGenerator)
                        WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(__instance.LinkedToZoneData.EventsOnUnlockDoor, eWardenObjectiveEventTrigger.None, true, 0.0f);
                    return;
            }
        }
    }
}
