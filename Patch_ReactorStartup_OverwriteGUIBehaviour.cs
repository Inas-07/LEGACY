using HarmonyLib;
using LevelGeneration;
using GameData;
using System;
using AK;
using Localization;
using UnityEngine;
using LEGACY.Utilities;
namespace LEGACY.Patch
{
    // won't work if there's multiple reactor
    [HarmonyPatch]
    internal class Patch_ReactorStartup_OverwriteGUIBehaviour
    {
        private static bool[] overrideHideGUITimer = null;

        private static float hideTimeThreshold = 43200.0f;

        private static void checkInit(int waveCount)
        {
            if (overrideHideGUITimer != null) return;

            overrideHideGUITimer = new bool[waveCount];
            for (int i = 0; i < overrideHideGUITimer.Length; i++) overrideHideGUITimer[i] = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnBuildDone))]
        private static void Post_OnBuildDone(LG_WardenObjective_Reactor __instance)
        {
            WardenObjectiveDataBlock db = null;

            if(WardenObjectiveManager.Current.TryGetActiveWardenObjectiveData(__instance.SpawnNode.LayerType, out db) == false 
                || db == null)
            {
                Utilities.Logger.Error("Patch_ReactorStartup_OverwriteGUIBehaviour: ");
                Utilities.Logger.Error("Failed to get warden objective datablock");
                return;
            }

            if (db.Type != eWardenObjectiveType.Reactor_Startup) return;

            int waveIndex = 0;
            while (waveIndex < __instance.m_waveCountMax)
            {
                ReactorWaveData currentWave = db.ReactorWaves[waveIndex];
                if (Math.Abs(currentWave.Verify - hideTimeThreshold) < 1.0)
                {
                    checkInit(__instance.m_waveCountMax);
                    overrideHideGUITimer[waveIndex] = true;
                }

                waveIndex++;
            }

            CurrentWaveCount = 0;
            CachedResult_IsInInfiniteWave = false;
        }

        // rewrite the entire method
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.SetGUIMessage))]
        private static bool Pre_SetGUIMessage(LG_WardenObjective_Reactor __instance, bool visible, ref string msg, ePUIMessageStyle style, bool printTimerInText, string timerPrefix, string timerSuffix)
        {
            if (!pReactorState.Equals(__instance.m_currentState.status, eReactorStatus.Startup_waitForVerify)) return true;

            if (!visible && (visible || !__instance.m_reactorGuiVisible))
                return false;
            if (visible)
            {
                if (printTimerInText)
                {
                    int currentWaveIndex = __instance.m_currentWaveCount - 1;
                    if(overrideHideGUITimer != null && overrideHideGUITimer[currentWaveIndex] == true) 
                    {   // hide reactor verification timer.
                        GuiManager.InteractionLayer.SetMessage(msg, style, -1);
                        GuiManager.InteractionLayer.SetMessageTimer(1.0f);
                    }
                    else // original impl.
                    {
                        if (isInInfiniteWave(__instance))
                        {   // hide verification terminal
                            msg = string.Format(Text.Get(3002u), __instance.m_currentWaveCount, __instance.m_waveCountMax);
                        }
                        double num1 = (1.0 - (double)__instance.m_currentWaveProgress) * (double)__instance.m_currentDuration;
                        int num2 = Mathf.FloorToInt((float)(num1 / 60.0));
                        int num3 = Mathf.FloorToInt((float)(num1 - (double)num2 * 60.0));
                        msg = msg + "\n<color=white>" + timerPrefix + " <color=orange>" + num2.ToString("D2") + ":" + num3.ToString("D2") + "</color>" + timerSuffix + "</color>";
                        GuiManager.InteractionLayer.SetMessage(msg, style, -1);
                        GuiManager.InteractionLayer.SetMessageTimer(__instance.m_currentWaveProgress);
                    }
                }
            }
            GuiManager.InteractionLayer.MessageVisible = visible;
            GuiManager.InteractionLayer.MessageTimerVisible = visible;
            __instance.m_reactorGuiVisible = visible;

            return false;
        }

        // Hide Warden Message for Infinite Wave
        private static int CurrentWaveCount = 0;
        private static bool CachedResult_IsInInfiniteWave = false;

        private static bool isInInfiniteWave(LG_WardenObjective_Reactor Reactor)
        {
            //Utilities.Logger.Warning("{0}, {1}", Reactor.m_currentWaveCount, CurrentWaveCount);
            // still in the same wave, use the result lastly evaluated.
            if (Reactor.m_currentWaveCount == CurrentWaveCount) return CachedResult_IsInInfiniteWave;
            CurrentWaveCount = Reactor.m_currentWaveCount;
            CachedResult_IsInInfiniteWave = false;

            ReactorWaveData WaveData = Reactor.m_currentWaveData;
            if (!WaveData.VerifyInOtherZone) return false;

            uint layoutID = 0u;

            if (Reactor.SpawnNode.m_zone.IsMainDimension)
            {
                switch (Reactor.SpawnNode.LayerType)
                {
                    case LG_LayerType.MainLayer: layoutID = RundownManager.ActiveExpedition.LevelLayoutData; break;
                    case LG_LayerType.SecondaryLayer: layoutID = RundownManager.ActiveExpedition.SecondaryLayout; break;
                    case LG_LayerType.ThirdLayer: layoutID = RundownManager.ActiveExpedition.ThirdLayout; break;
                    default: Utilities.Logger.Error("Unimplemented layer type."); return false;
                }
            }
            else
            {
                layoutID = Reactor.SpawnNode.m_zone.Dimension.DimensionData.LevelLayoutData;
            }

            if (layoutID == 0u) return false;
            //Utilities.Logger.Warning("{0}", Reactor.SpawnNode.LayerType);

            LevelLayoutDataBlock layoutDB = null;
            layoutDB = LevelLayoutDataBlock.GetBlock(layoutID);
            if (layoutDB == null) return false;

            eDimensionIndex dimensionIndex = Reactor.SpawnNode.m_zone.DimensionIndex;

            ExpeditionZoneData ZoneForVerification = null;
            foreach (ExpeditionZoneData Zone in layoutDB.Zones)
            {
                if (Zone.LocalIndex == WaveData.ZoneForVerification)
                {
                    ZoneForVerification = Zone;
                    break;
                }
            }

            if (ZoneForVerification == null)
            {
                Utilities.Logger.Error("Did not found zone for verification!");
                return false;
            }
            
            if (ZoneForVerification.TerminalPlacements == null || ZoneForVerification.TerminalPlacements.Count != 1) return false;

            TerminalPlacementData TerminalData = ZoneForVerification.TerminalPlacements[0];
            TerminalStartStateData TerminalState = TerminalData.StartingStateData;
            
            if (!TerminalState.PasswordProtected
                || TerminalState.PasswordPartCount != 1
                || TerminalState.TerminalZoneSelectionDatas == null
                || TerminalState.TerminalZoneSelectionDatas.Count != 1
                || TerminalState.TerminalZoneSelectionDatas[0] == null
                || TerminalState.TerminalZoneSelectionDatas[0].Count != 1
                ) return false;

            if (TerminalState.TerminalZoneSelectionDatas[0][0] != null &&
                TerminalState.TerminalZoneSelectionDatas[0][0].LocalIndex == ZoneForVerification.LocalIndex)
            {
                CachedResult_IsInInfiniteWave = true;
                return true;
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnStateChange))]
        private static bool Pre_HideReactorMessageForInfiniteWave(LG_WardenObjective_Reactor __instance, pReactorState oldState, pReactorState newState)
        {
            if (oldState.status == newState.status) return true;
            if (newState.status != eReactorStatus.Startup_intro
                && newState.status != eReactorStatus.Startup_intense
                && newState.status != eReactorStatus.Startup_waitForVerify) return true;

            if (ForceDisable() || isInInfiniteWave(__instance))
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
                            Utilities.Logger.Error("Patch_ReactorStartup_OverwriteGUIBehaviour: ");
                            Utilities.Logger.Error("Failed to get warden objective datablock");
                            break;
                        }
                        __instance.m_lightCollection.SetMode(db.LightsOnDuringIntro);
                        // R7 migration End
                        
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
                        if (__instance.m_currentWaveCount == 1)
                        {
                            Debug.LogError("Reactor IDLE START");
                            int num1 = (int)__instance.m_sound.Post(EVENTS.REACTOR_POWER_LEVEL_1_LOOP);
                            int num2 = (int)__instance.m_sound.SetRTPCValue(GAME_PARAMETERS.REACTOR_POWER, 0.0f);
                        }
                        else
                        {
                            Debug.LogError("Reactor REACTOR_POWER_DOWN");
                            int num3 = (int)__instance.m_sound.Post(EVENTS.REACTOR_POWER_LEVEL_2_TO_1_TRANSITION);
                            int num4 = (int)__instance.m_sound.SetRTPCValue(GAME_PARAMETERS.REACTOR_POWER, 0.0f);
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
            return RundownManager.ActiveExpedition.LevelLayoutData == (uint)MainLayerID.L3E2;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GS_AfterLevel), nameof(GS_AfterLevel.CleanupAfterExpedition))]
        private static void Post_CleanupAfterExpedition()
        {
            overrideHideGUITimer = null;
        }
    }
}
