using HarmonyLib;
using LevelGeneration;
using GameData;
using Localization;
using LEGACY.Utils;

namespace LEGACY.Reactor
{
    [HarmonyPatch]
    internal class Patch_ReactorShutdown
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnStateChange))]
        private static void Post_OnStateChange(LG_WardenObjective_Reactor __instance, pReactorState oldState, pReactorState newState)
        {
            if (__instance == null) return;

            if (oldState.status == newState.status) return;

            WardenObjectiveDataBlock db = null;
            if (WardenObjectiveManager.Current.TryGetActiveWardenObjectiveData(__instance.SpawnNode.LayerType, out db) == false
                || db == null)
            {
                LegacyLogger.Error("Patch_ReactorShutdown: ");
                LegacyLogger.Error("Failed to get warden objective");
                return;
            }

            if (db.Type != eWardenObjectiveType.Reactor_Shutdown) return;
            if (db.OnActivateOnSolveItem == true) return;

            eWardenObjectiveEventTrigger trigger = eWardenObjectiveEventTrigger.None;

            switch (newState.status)
            {
                case eReactorStatus.Shutdown_waitForVerify: trigger = eWardenObjectiveEventTrigger.OnStart; break;
                case eReactorStatus.Shutdown_puzzleChaos: trigger = eWardenObjectiveEventTrigger.OnMid; break;
                case eReactorStatus.Shutdown_complete: trigger = eWardenObjectiveEventTrigger.OnEnd; break;
                default: return;
            }

            LG_LayerType layer = __instance.SpawnNode.LayerType;

            WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(db.EventsOnActivate, trigger, false);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnBuildDone))]
        private static void Pre_OnBuildDone_ChainedPuzzleMidObjectiveFix(LG_WardenObjective_Reactor __instance)
        {
            WardenObjectiveDataBlock objective = null;
            if (WardenObjectiveManager.Current.TryGetActiveWardenObjectiveData(__instance.SpawnNode.LayerType, out objective) == false
                || objective == null)
            {
                LegacyLogger.Error("Patch_ReactorShutdown: Failed to get warden objective");
                return;
            }

            if (objective.Type != eWardenObjectiveType.Reactor_Shutdown) return;
            if (objective.ChainedPuzzleMidObjective <= 0U) return;

            __instance.m_chainedPuzzleAlignMidObjective = __instance.m_chainedPuzzleAlign;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.Update))]
        private static bool Pre_Update(LG_WardenObjective_Reactor __instance)
        {
            // overwrite Update for eReactorStatus.Shutdown_waitForVerify
            if (__instance.m_currentState.status != eReactorStatus.Shutdown_waitForVerify) return true;

            if (!__instance.m_currentWaveData.HasVerificationTerminal) return true;

            __instance.SetGUIMessage(true, Text.Format(3000U, (object)"<color=orange>" + __instance.m_currentWaveData.VerificationTerminalSerial + "</color>"), ePUIMessageStyle.Warning, false);

            return false;
        }
    }
}
