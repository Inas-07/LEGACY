using HarmonyLib;
using LevelGeneration;
using GameData;
using SNetwork;

namespace LEGACY.Patch
{
    [HarmonyPatch]
    internal class Patch_ReactorStartup_ExtraEventsExecution
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnTerminalStartupSequenceVerify))]
        private static void Post_ExecuteEventsOnEndOnClientSide(LG_WardenObjective_Reactor __instance)
        {
            // execute events on client side
            if (SNet.IsMaster) return;

            /* LG_WardenObjective_Reactor.OnTerminalStartupSequenceVerify is called on correct verification */
            WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(__instance.m_currentWaveData.Events, eWardenObjectiveEventTrigger.OnEnd, false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnStateChange))]
        private static void Post_ExecuteOnNoneEventsOnDefenseStart(LG_WardenObjective_Reactor __instance, pReactorState oldState, pReactorState newState)
        {
            if (oldState.status == newState.status) return;
            if (newState.status != eReactorStatus.Startup_intense) return;

            WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(__instance.m_currentWaveData.Events, eWardenObjectiveEventTrigger.None, false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnBuildDone))]
        private static void Post_OnBuildDone(LG_WardenObjective_Reactor __instance)
        {
            WardenObjectiveDataBlock db = WardenObjectiveManager.ActiveWardenObjective(__instance.SpawnNode.LayerType);

            if (db.Type != eWardenObjectiveType.Reactor_Startup || db.OnActivateOnSolveItem == true) return;

            __instance.m_chainedPuzzleToStartSequence.OnPuzzleSolved += new System.Action(() => {
                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(db.EventsOnActivate, eWardenObjectiveEventTrigger.None, true);
            });
        }
    }
}
