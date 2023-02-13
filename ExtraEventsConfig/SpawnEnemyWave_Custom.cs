using AIGraph;
using AK;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using GameData;
using LEGACY.Utils;
using LevelGeneration;
using Player;
using HarmonyLib;
using SNetwork;
using System.Collections;

namespace LEGACY.ExtraEventsConfig
{

    internal static class SpawnEnemyWave_Custom
    {
        private static System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<ushort>> WaveEventsMap = new();

        internal static void StopSpecifiedWave(WardenObjectiveEventData eventToTrigger, float currentDuration)
        {
            UnityEngine.Coroutine coroutine = CoroutineManager.StartCoroutine(StopWave(eventToTrigger, currentDuration).WrapToIl2Cpp(), null);
            WorldEventManager.m_worldEventEventCoroutines.Add(coroutine);
            //WardenObjectiveManager.m_wardenObjectiveEventCoroutines.Add(coroutine);
        }

        internal static void OnStopAllWave()
        {
            WaveEventsMap.Clear();
        }

        internal static bool SpawnWave(WardenObjectiveEventData eventToTrigger, float currentDuration)
        {
            bool use_vanilla_impl = true;

            if (!string.IsNullOrEmpty(eventToTrigger.WorldEventObjectFilter))
            {
                if (!WaveEventsMap.ContainsKey(eventToTrigger.WorldEventObjectFilter))
                {
                    System.Collections.Generic.List<ushort> eventIDList = new();
                    WaveEventsMap.Add(eventToTrigger.WorldEventObjectFilter, eventIDList);
                    Logger.Debug("Registering Wave(s) with filter {0}", eventToTrigger.WorldEventObjectFilter);

                    // added list here instead of in coroutine to get around with issue caused by concurrency.
                    // the WaveEventsMap will be cleaned OnApplicationQuit and AfterLevel
                }
                use_vanilla_impl = false;
            }

            if (use_vanilla_impl == true)
            {
                SurvivalWaveSettingsDataBlock waveSettingDB = SurvivalWaveSettingsDataBlock.GetBlock(eventToTrigger.EnemyWaveData.WaveSettings);
                if (waveSettingDB.m_overrideWaveSpawnType == true &&
                    (waveSettingDB.m_survivalWaveSpawnType == SurvivalWaveSpawnType.InSuppliedCourseNodeZone
                    || waveSettingDB.m_survivalWaveSpawnType == SurvivalWaveSpawnType.InSuppliedCourseNode))
                {
                    use_vanilla_impl = false;
                }
            }

            if (!use_vanilla_impl)
            {
                UnityEngine.Coroutine coroutine = CoroutineManager.StartCoroutine(SpawnWave(eventToTrigger, currentDuration, 0).WrapToIl2Cpp(), null);
                WorldEventManager.m_worldEventEventCoroutines.Add(coroutine);
            }
            //WardenObjectiveManager.m_wardenObjectiveEventCoroutines.Add(coroutine);

            return use_vanilla_impl;
        }

        private static IEnumerator SpawnWave(WardenObjectiveEventData eventToTrigger, float currentDuration, int ph = 0) // place holder int, unused.
        {
            WardenObjectiveEventData e = eventToTrigger;

            float delay = UnityEngine.Mathf.Max(e.Delay - currentDuration, 0f);
            if (delay > 0f)
            {
                yield return new UnityEngine.WaitForSeconds(delay);
            }

            WardenObjectiveManager.DisplayWardenIntel(e.Layer, e.WardenIntel);
            if (e.DialogueID > 0u)
            {
                PlayerDialogManager.WantToStartDialog(e.DialogueID, -1, false, false);
            }
            if (e.SoundID > 0u)
            {
                WardenObjectiveManager.Current.m_sound.Post(e.SoundID, true);
                var line = e.SoundSubtitle.ToString();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    GuiManager.PlayerLayer.ShowMultiLineSubtitle(line);
                }
            }

            // spawn code rewrite
            PlayerAgent localPlayer = PlayerManager.GetLocalPlayerAgent();
            if (!SNet.IsMaster || localPlayer == null)
                yield break;

            var waveData = eventToTrigger.EnemyWaveData;

            if ((waveData.WaveSettings > 0U && waveData.WavePopulation > 0U && (currentDuration == 0.0 || waveData.SpawnDelay >= currentDuration)) == false)
                yield break;

            SurvivalWaveSettingsDataBlock waveSettingDB = SurvivalWaveSettingsDataBlock.GetBlock(waveData.WaveSettings);

            bool abort_spawn = false;
            AIG_CourseNode spawnNode = localPlayer.CourseNode;
            SurvivalWaveSpawnType spawnType = SurvivalWaveSpawnType.InRelationToClosestAlivePlayer;

            if (waveSettingDB.m_overrideWaveSpawnType == true)
            {
                if (waveSettingDB.m_survivalWaveSpawnType == SurvivalWaveSpawnType.InSuppliedCourseNodeZone
                    || waveSettingDB.m_survivalWaveSpawnType == SurvivalWaveSpawnType.InSuppliedCourseNode)
                {
                    LG_Zone specified_zone = null;
                    Builder.CurrentFloor.TryGetZoneByLocalIndex(eventToTrigger.DimensionIndex, eventToTrigger.Layer, eventToTrigger.LocalIndex, out specified_zone);
                    if (specified_zone == null)
                    {
                        Logger.Error("SpawnSurvialWave_InSuppliedCourseNodeZone - Failed to find LG_Zone.");
                        Logger.Error("DimensionIndex: {0}, Layer: {1}, LocalIndex: {2}", eventToTrigger.DimensionIndex, eventToTrigger.Layer, eventToTrigger.LocalIndex);
                        yield break;
                    }

                    spawnNode = specified_zone.m_courseNodes[0];
                    spawnType = SurvivalWaveSpawnType.InSuppliedCourseNodeZone;

                    if (waveSettingDB.m_survivalWaveSpawnType == SurvivalWaveSpawnType.InSuppliedCourseNode)
                    {
                        if (e.Count < specified_zone.m_courseNodes.Count)
                        {
                            spawnNode = specified_zone.m_courseNodes[e.Count];
                            spawnType = SurvivalWaveSpawnType.InSuppliedCourseNode;
                        }
                        else
                        {
                            Logger.Error("Spawn InSuppliedCourseNode but zone {0}-{1} does not exist! Falling back to InSuppliedCourseNodeZone", specified_zone.Alias, 'A' + e.Count);
                        }
                    }

                    Logger.Debug("Starting wave with spawn type InSuppliedCourseNodeZone / InSuppliedCourseNode!");
                    Logger.Debug("DimensionIndex: {0}, Layer: {1}, LocalIndex: {2}", eventToTrigger.DimensionIndex, eventToTrigger.Layer, eventToTrigger.LocalIndex);
                }
            }

            if (abort_spawn) yield break;

            UnityEngine.Coroutine coroutine = CoroutineManager.StartCoroutine(TriggerEnemyWaveDataAndRegisterWaveEventID(waveData, spawnNode, spawnType, currentDuration, eventToTrigger.WorldEventObjectFilter).WrapToIl2Cpp(), null);
            WardenObjectiveManager.m_wardenObjectiveWaveCoroutines.Add(coroutine);
        }

        private static IEnumerator TriggerEnemyWaveDataAndRegisterWaveEventID(
          GenericEnemyWaveData data,
          AIG_CourseNode spawnNode,
          SurvivalWaveSpawnType waveSpawnType,
          float currentDuration = 0.0f,
          string worldEventObjectFilter = "")
        {
            UnityEngine.Debug.Log("WardenObjectiveManager.TriggerEnemyWaveData Data: " + data + " spawnNode: " + spawnNode + " waveSpawnType: " + waveSpawnType);
            yield return new UnityEngine.WaitForSeconds(data.SpawnDelay - currentDuration);
            if (spawnNode != null)
            {
                ushort eventID;
                if (SNet.IsMaster && Mastermind.Current.TriggerSurvivalWave(spawnNode, data.WaveSettings, data.WavePopulation, out eventID, waveSpawnType, 2f, data.AreaDistance)) // areaDistance here has no effect. 
                {
                    UnityEngine.Debug.Log("WardenObjectiveManager.TriggerEnemyWaveData - Enemy wave spawned (" + waveSpawnType + ") with eventID " + eventID);
                    if (!string.IsNullOrEmpty(worldEventObjectFilter))
                    {
                        if (WaveEventsMap.ContainsKey(worldEventObjectFilter))
                        {
                            System.Collections.Generic.List<ushort> eventIDList;
                            WaveEventsMap.TryGetValue(worldEventObjectFilter, out eventIDList);
                            eventIDList.Add(eventID);
                        }
                        else
                        {
                            Logger.Error("We should have instanitiated a List before call to coroutine, WTF???");
                            yield break;
                        }

                        Logger.Debug("Registered wave with filter {0}", worldEventObjectFilter);
                    }
                }
                else
                    UnityEngine.Debug.Log("WardenObjectiveManager.TriggerEnemyWaveData : WARNING : Failed spawning enemy wave");
            }

            if (!string.IsNullOrEmpty(data.IntelMessage))
                GuiManager.PlayerLayer.m_wardenIntel.ShowSubObjectiveMessage("", data.IntelMessage);

            if (spawnNode != null)
            {
                if (data.TriggerAlarm)
                {
                    // Not pretty sure if 'this' can be replaced by WardenObjectiveManager.Current
                    int num1 = (int)WardenObjectiveManager.Current.m_sound.UpdatePosition(spawnNode.Position);
                    int num2 = (int)WardenObjectiveManager.Current.m_sound.Post(EVENTS.APEX_PUZZLE_START_ALARM);
                    UnityEngine.Debug.Log("WardenObjectiveManager.TriggerEnemyWaveData - Alarm");
                }
                yield return new UnityEngine.WaitForSeconds(2f);
            }
            else
                UnityEngine.Debug.LogError("WardenobjectiveManager.TriggerEnemyWaveData got NO SPAWNNODE for the enemy wave!");
        }

        private static IEnumerator StopWave(WardenObjectiveEventData eventToTrigger, float currentDuration)
        {
            WardenObjectiveEventData e = eventToTrigger;

            float delay = UnityEngine.Mathf.Max(e.Delay - currentDuration, 0f);
            if (delay > 0f)
            {
                yield return new UnityEngine.WaitForSeconds(delay);
            }

            WardenObjectiveManager.DisplayWardenIntel(e.Layer, e.WardenIntel);
            if (e.DialogueID > 0u)
            {
                PlayerDialogManager.WantToStartDialog(e.DialogueID, -1, false, false);
            }
            if (e.SoundID > 0u)
            {
                WardenObjectiveManager.Current.m_sound.Post(e.SoundID, true);
                var line = e.SoundSubtitle.ToString();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    GuiManager.PlayerLayer.ShowMultiLineSubtitle(line);
                }
            }

            if (!SNet.IsMaster) yield break;

            if (string.IsNullOrEmpty(e.WorldEventObjectFilter))
            {
                Logger.Error("WorldEventObjectFilter is empty. Aborted stop wave event.");
                yield break;
            }

            if (!WaveEventsMap.ContainsKey(e.WorldEventObjectFilter))
            {
                Logger.Error("Wave Filter {0} is unregistered, cannot stop wave.", e.WorldEventObjectFilter);
                yield break;
            }

            WaveEventsMap.TryGetValue(e.WorldEventObjectFilter, out var eventIDList);
            WaveEventsMap.Remove(e.WorldEventObjectFilter);

            Mastermind.MastermindEvent masterMindEvent_StopWave;
            foreach (ushort eventID in eventIDList)
            {
                if (Mastermind.Current.TryGetEvent(eventID, out masterMindEvent_StopWave))
                {
                    masterMindEvent_StopWave.StopEvent();
                }
            }

            Logger.Debug("Wave(s) with filter {0} stopped.", e.WorldEventObjectFilter);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GS_AfterLevel), nameof(GS_AfterLevel.CleanupAfterExpedition))]
        internal static void CleanupAfterExpedition()
        {
            WaveEventsMap.Clear();
        }
    }
}
