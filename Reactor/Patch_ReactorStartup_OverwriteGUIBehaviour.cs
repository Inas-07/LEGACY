using HarmonyLib;
using LevelGeneration;
using GameData;
using AK;
using UnityEngine;
using LEGACY.Utils;
using System.Collections.Generic;

namespace LEGACY.Reactor
{
    // won't work if there's multiple reactor
    [HarmonyPatch]
    internal class Patch_ReactorStartup_OverwriteGUIBehaviour
    {
        private static HashSet<uint> ForceDisableLevels = new();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnStateChange))]
        private static bool Pre_HideReactorMessageForInfiniteWave(LG_WardenObjective_Reactor __instance, pReactorState oldState, pReactorState newState)
        {
            if (oldState.status == newState.status) return true;
            if (newState.status != eReactorStatus.Startup_intro
                && newState.status != eReactorStatus.Startup_intense
                && newState.status != eReactorStatus.Startup_waitForVerify) return true;

            if (ForceDisable())
            {
                if (oldState.stateCount != newState.stateCount)
                    __instance.OnStateCountUpdate(newState.stateCount);
                if (oldState.stateProgress != newState.stateProgress)
                    __instance.OnStateProgressUpdate(newState.stateProgress);
                __instance.ReadyForVerification = false;

                // original implementation, with calls to warden intel display commented out
                switch (newState.status)
                {
                    case eReactorStatus.Startup_intro:
                        // R7 migration 
                        WardenObjectiveDataBlock db = null;

                        if (WardenObjectiveManager.Current.TryGetActiveWardenObjectiveData(__instance.SpawnNode.LayerType, out db) == false
                            || db == null)
                        {
                            LegacyLogger.Error("Patch_ReactorStartup_OverwriteGUIBehaviour: ");
                            LegacyLogger.Error("Failed to get warden objective datablock");
                            break;
                        }
                        __instance.m_lightCollection.SetMode(db.LightsOnDuringIntro);


                        // R6 impl.
                        //__instance.m_lightCollection.SetMode(WardenObjectiveManager.ActiveWardenObjective(__instance.SpawnNode.LayerType).LightsOnDuringIntro);

                        __instance.m_lightCollection.ResetUpdateValues(true);
                        __instance.lcReset = true;
                        __instance.m_lightsBlinking = false;
                        __instance.m_spawnEnemies = false;
                        __instance.m_progressUpdateEnabled = true;
                        __instance.m_alarmCountdownPlayed = false;
                        __instance.m_currentDuration = !newState.verifyFailed ? __instance.m_currentWaveData.Warmup : __instance.m_currentWaveData.WarmupFail;
                        WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(__instance.m_currentWaveData.Events, eWardenObjectiveEventTrigger.OnStart, false, 0.0f);
                        //Utils.CheckAndExecuteEventsOnTrigger(__instance.m_currentWaveData.Events, eWardenObjectiveEventTrigger.OnStart, false, 0.0f);
                        if (__instance.m_currentWaveCount == 1)
                        {
                            Debug.LogError("Reactor IDLE START");
                            __instance.m_sound.Post(EVENTS.REACTOR_POWER_LEVEL_1_LOOP);
                            __instance.m_sound.SetRTPCValue(GAME_PARAMETERS.REACTOR_POWER, 0.0f);
                        }
                        else
                        {
                            Debug.LogError("Reactor REACTOR_POWER_DOWN");
                            __instance.m_sound.Post(EVENTS.REACTOR_POWER_LEVEL_2_TO_1_TRANSITION);
                            __instance.m_sound.SetRTPCValue(GAME_PARAMETERS.REACTOR_POWER, 0.0f);
                        }
                        //if (newState.verifyFailed)
                        //{
                        //    GuiManager.PlayerLayer.m_wardenIntel.ShowSubObjectiveMessage("", Text.Get(1075U));
                        //    break;
                        //}
                        //GuiManager.PlayerLayer.m_wardenIntel.ShowSubObjectiveMessage("", Text.Get(1076U));
                        break;
                    case eReactorStatus.Startup_intense:
                        __instance.m_lightCollection.ResetUpdateValues(true);
                        __instance.lcReset = true;
                        __instance.m_spawnEnemies = true;
                        __instance.m_currentEnemyWaveIndex = 0;
                        __instance.m_alarmCountdownPlayed = false;
                        __instance.m_progressUpdateEnabled = true;
                        __instance.m_currentDuration = __instance.m_currentWaveData.Wave;
                        __instance.m_sound.Post(EVENTS.REACTOR_POWER_LEVEL_1_TO_3_TRANSITION);
                        //GuiManager.PlayerLayer.m_wardenIntel.ShowSubObjectiveMessage("", Text.Get(1077U));
                        break;
                    case eReactorStatus.Startup_waitForVerify:
                        __instance.m_lightCollection.ResetUpdateValues(false);
                        __instance.lcReset = true;
                        __instance.m_spawnEnemies = false;
                        __instance.m_progressUpdateEnabled = true;
                        __instance.ReadyForVerification = true;
                        Debug.Log("Wait for verify! newState.verifyFailed? " + newState.verifyFailed.ToString());
                        __instance.m_currentDuration = !newState.verifyFailed ? __instance.m_currentWaveData.Verify : __instance.m_currentWaveData.VerifyFail;
                        WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(__instance.m_currentWaveData.Events, eWardenObjectiveEventTrigger.OnMid, false, 0.0f);
                        //Utils.CheckAndExecuteEventsOnTrigger(__instance.m_currentWaveData.Events, eWardenObjectiveEventTrigger.OnMid, false, 0.0f);
                        //GuiManager.PlayerLayer.m_wardenIntel.ShowSubObjectiveMessage("", Text.Get(1078U));
                        break;

                }
                __instance.m_currentState = newState;
                return false;
            }

            return true;
        }

        private static bool ForceDisable() => ForceDisableLevels.Contains(RundownManager.ActiveExpedition.LevelLayoutData);
        
        static Patch_ReactorStartup_OverwriteGUIBehaviour()
        {
            var block = LevelLayoutDataBlock.GetBlock("Legacy_L3E2_L1");
            if(block != null)
            {
                ForceDisableLevels.Add(block.persistentID);
            }

            block = LevelLayoutDataBlock.GetBlock("Legacy_L1E1_L1");
            if(block != null)
            {
               ForceDisableLevels.Add(block.persistentID);
            }
        }
    }
}
