using HarmonyLib;
using LevelGeneration;
using GameData;
using System;
using AK;
using Localization;
using UnityEngine;
using LEGACY.Utils;
using GTFO.API;
using System.Collections.Generic;

namespace LEGACY.Reactor
{
    // won't work if there's multiple reactor
    [HarmonyPatch]
    internal class Patch_ReactorStartup_OverwriteGUIBehaviour
    {
        //private static bool[] overrideHideGUITimer = null;
        private static WardenObjectiveDataBlock[] dbs = new WardenObjectiveDataBlock[3] { null, null, null };

        private const float hideTimeThreshold = 43200.0f;

        private static HashSet<uint> ForceDisableLevels = new();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnBuildDone))]
        private static void Post_OnBuildDone(LG_WardenObjective_Reactor __instance)
        {
            WardenObjectiveDataBlock db = null;
            if (WardenObjectiveManager.Current.TryGetActiveWardenObjectiveData(__instance.SpawnNode.LayerType, out db) == false
                || db == null)
            {
                LegacyLogger.Error("Patch_ReactorStartup_OverwriteGUIBehaviour: ");
                LegacyLogger.Error("Failed to get warden objective datablock");
                return;
            }

            if (db.Type != eWardenObjectiveType.Reactor_Startup) return;

            if (dbs[(int)__instance.SpawnNode.LayerType] != null)
            {
                LegacyLogger.Error($"ReactorStartup_OverwriteGUIBehaviour: multiple reactor startup objective definition found for layer {__instance.SpawnNode.LayerType}. Nonsense!");
            }

            dbs[(int)__instance.SpawnNode.LayerType] = db;
        }

        //// rewrite the entire method
        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.SetGUIMessage))]
        //private static bool Pre_SetGUIMessage(LG_WardenObjective_Reactor __instance, bool visible, ref string msg, ePUIMessageStyle style, bool printTimerInText, string timerPrefix, string timerSuffix)
        //{
        //    if (dbs[(int)__instance.SpawnNode.LayerType] == null || !Equals(__instance.m_currentState.status, eReactorStatus.Startup_waitForVerify)) return true;

        //    if (!visible && (visible || !__instance.m_reactorGuiVisible))
        //        return false;
        //    if (visible)
        //    {
        //        if (printTimerInText)
        //        {
        //            int currentWaveIndex = __instance.m_currentWaveCount - 1;
        //            if (dbs[(int)__instance.SpawnNode.LayerType].ReactorWaves[currentWaveIndex].Verify >= hideTimeThreshold)
        //            {   // hide reactor verification timer.
        //                GuiManager.InteractionLayer.SetMessage(msg, style, -1);
        //                GuiManager.InteractionLayer.SetMessageTimer(1.0f);
        //            }
        //            else // original impl.
        //            {
        //                double num1 = (1.0 - (double)__instance.m_currentWaveProgress) * (double)__instance.m_currentDuration;
        //                int num2 = Mathf.FloorToInt((float)(num1 / 60.0));
        //                int num3 = Mathf.FloorToInt((float)(num1 - num2 * 60.0));
        //                msg = msg + "\n<color=white>" + timerPrefix + " <color=orange>" + num2.ToString("D2") + ":" + num3.ToString("D2") + "</color>" + timerSuffix + "</color>";
        //                GuiManager.InteractionLayer.SetMessage(msg, style, -1);
        //                GuiManager.InteractionLayer.SetMessageTimer(__instance.m_currentWaveProgress);
        //            }
        //        }
        //    }
        //    GuiManager.InteractionLayer.MessageVisible = visible;
        //    GuiManager.InteractionLayer.MessageTimerVisible = visible;
        //    __instance.m_reactorGuiVisible = visible;

        //    return false;
        //}

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
                            Utils.LegacyLogger.Error("Patch_ReactorStartup_OverwriteGUIBehaviour: ");
                            Utils.LegacyLogger.Error("Failed to get warden objective datablock");
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
                        int num = (int)__instance.m_sound.Post(EVENTS.REACTOR_POWER_LEVEL_1_TO_3_TRANSITION);
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

        private static bool ForceDisable()
        {
            return ForceDisableLevels.Contains(RundownManager.ActiveExpedition.LevelLayoutData);
        }

        static Patch_ReactorStartup_OverwriteGUIBehaviour()
        {
            LevelAPI.OnLevelCleanup += Clear;
            LevelLayoutDataBlock block = LevelLayoutDataBlock.GetBlock("Legacy_L3E2_L1");
            ForceDisableLevels.Add(block.persistentID);

            block = LevelLayoutDataBlock.GetBlock("Legacy_L1E1_L1");
            ForceDisableLevels.Add(block.persistentID);
        }

        private static void Clear()
        {
            for (int i = 0; i < 3; i++) dbs[i] = null;
        }
    }
}
