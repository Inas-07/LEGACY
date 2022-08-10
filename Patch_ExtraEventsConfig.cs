using HarmonyLib;
using Enemies;
using LevelGeneration;
using GameData;
using AIGraph;
using Globals;
using System.Collections;
using LEGACY.Utilities;
using Player;
using BepInEx.IL2CPP.Utils.Collections;
using SNetwork;
namespace LEGACY.Patch
{
    enum EventType
    {
        CloseSecurityDoor_Custom = 100,
        KillEnemiesInDimension_Custom = 101
    }

    [HarmonyPatch]
    class Patch_ExtraEventsConfig
    {
        private static void SetTerminalCommand_Custom(WardenObjectiveEventData eventToTrigger)
        {
            LG_LayerType layer = eventToTrigger.Layer;
            eLocalZoneIndex localIndex = eventToTrigger.LocalIndex;
            eDimensionIndex dimensionIndex = eventToTrigger.DimensionIndex;
            LG_Zone terminalZone = null;
            Builder.CurrentFloor.TryGetZoneByLocalIndex(dimensionIndex, layer, localIndex, out terminalZone);
            if (terminalZone == null)
            {
                Logger.Error("Failed to get terminal in zone {0}, layer {1}, dimension {2}.", localIndex, layer, dimensionIndex);
                return;
            }

            if (terminalZone.TerminalsSpawnedInZone == null)
            {
                Logger.Error("ExtraEventsConfig: terminalZone.TerminalsSpawnedInZone == null");
                return;
            }

            if (terminalZone.TerminalsSpawnedInZone.Count < 1)
            {
                Logger.Error("ExtraEventsConfig: No terminal spawns in the specified zone!");
                return;
            }

            if (eventToTrigger.Count >= terminalZone.TerminalsSpawnedInZone.Count)
            {
                Logger.Error("ExtraEventsConfig: Invalid event.Count: 0 < event.Count < TerminalsSpawnedInZone.Count should suffice.");
                return;
            }

            LG_ComputerTerminal terminal = terminalZone.TerminalsSpawnedInZone[eventToTrigger.Count];
            if (eventToTrigger.Enabled == true)
            {
                terminal.TrySyncSetCommandShow(eventToTrigger.TerminalCommand);
            }
            else
            {
                terminal.TrySyncSetCommandHidden(eventToTrigger.TerminalCommand);
            }
        }

        private static void CloseSecurityDoor_Custom(WardenObjectiveEventData eventToTrigger)
        {
            LG_Zone zone = null;
            if(Builder.CurrentFloor.TryGetZoneByLocalIndex(eventToTrigger.DimensionIndex, eventToTrigger.Layer, eventToTrigger.LocalIndex, out zone) == false || zone == null)
            {
                Logger.Error("CloseSecurityDoor_Custom: Failed to get zone {0}, layer {1}, dimensionIndex {2}", eventToTrigger.LocalIndex, eventToTrigger.Layer, eventToTrigger.DimensionIndex);
                return ;
            }

            LG_SecurityDoor door = null;
            if(Utilities.Utils.TryGetZoneEntranceSecDoor(zone, out door) == false || door == null)
            {
                Logger.Error("CloseSecurityDoor_Custom: failed to get LG_SecurityDoor!");
                return;
            }

            pDoorState currentSyncState1 = door.m_sync.GetCurrentSyncState();
            if (currentSyncState1.status != eDoorStatus.Open && currentSyncState1.status != eDoorStatus.Opening)
                return;
            Logger.Debug("Door Closed!");
            LG_Door_Sync lgDoorSync = door.m_sync.TryCast<LG_Door_Sync>();
            
            if (lgDoorSync == null) return;
            
            pDoorState currentSyncState2 = lgDoorSync.GetCurrentSyncState() with
            {
                status = eDoorStatus.Closed,
                hasBeenOpenedDuringGame = false
            };

            lgDoorSync.m_stateReplicator.State = currentSyncState2;
            LG_Gate gate = door.Gate;
            gate.HasBeenOpenedDuringPlay = false;
            gate.IsTraversable = false;
        }

        private static void KillEnemiesInDimension_Custom(WardenObjectiveEventData eventToTrigger)
        {
            if (!SNet.IsMaster) return;
            Dimension dimension = null;
            Dimension.GetDimension(eventToTrigger.DimensionIndex, out dimension);

            for (int index1 = 0; index1 < dimension.Layers.Count; ++index1)
            {
                LG_Layer layer = dimension.Layers[index1];
                for (int index2 = 0; index2 < layer.m_zones.Count; ++index2)
                {
                    LG_Zone zone2 = layer.m_zones[index2];
                    LG_SecurityDoor door;

                    Utils.TryGetZoneEntranceSecDoor(zone2, out door);

                    // limited kill
                    if (index2 == 0 || (door != null && door.m_sync.GetCurrentSyncState().status == eDoorStatus.Open)) // door opened, kill all
                    {
                        for (int index3 = 0; index3 < zone2.m_courseNodes.Count; ++index3)
                        {
                            EnemyAgent[] array = zone2.m_courseNodes[index3].m_enemiesInNode.ToArray();
                            int num2 = 0;
                            for (int index4 = 0; index4 < array.Length; ++index4)
                            {
                                EnemyAgent enemyAgent = array[index4];
                                if (enemyAgent != null && enemyAgent.Damage != null)
                                {
                                    enemyAgent.Damage.MeleeDamage(float.MaxValue, null, UnityEngine.Vector3.zero, UnityEngine.Vector3.up, 0, 1f, 1f, 1f, 1f, false, DamageNoiseLevel.Normal);
                                    ++num2;
                                }
                            }
                        }
                    }
                }
            }

        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WardenObjectiveManager), nameof(WardenObjectiveManager.CheckAndExecuteEventsOnTrigger), new System.Type[] {
            typeof(WardenObjectiveEventData),
            typeof(eWardenObjectiveEventTrigger),
            typeof(bool),
            typeof(float)
        })]
        private static bool Pre_CheckAndExecuteEventsOnTrigger(WardenObjectiveEventData eventToTrigger,
            eWardenObjectiveEventTrigger trigger,
            bool ignoreTrigger = false,
            float currentDuration = 0.0f)
        {
            // custom event
            switch((int)eventToTrigger.Type)
            {
                case (int)EventType.CloseSecurityDoor_Custom:
                case (int)EventType.KillEnemiesInDimension_Custom:
                    CoroutineManager.StartCoroutine(Handle(eventToTrigger, currentDuration).WrapToIl2Cpp(), null);
                    return false;
            }

            // vanilla event modification
            switch (eventToTrigger.Type)
            {
                case eWardenObjectiveEventType.SetTerminalCommand:
                case eWardenObjectiveEventType.DimensionFlashTeam:
                case eWardenObjectiveEventType.DimensionWarpTeam:
                    CoroutineManager.StartCoroutine(Handle(eventToTrigger, currentDuration).WrapToIl2Cpp(), null);
                    return false;
                default: return true;
            }
        }

        private static IEnumerator Handle(WardenObjectiveEventData eventToTrigger, float currentDuration)
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

            switch((int)e.Type)
            {
                case (int)EventType.CloseSecurityDoor_Custom:
                    CloseSecurityDoor_Custom(e);      yield break;
                case (int)EventType.KillEnemiesInDimension_Custom:
                    KillEnemiesInDimension_Custom(e);
                    Logger.Warning("Killed.");
                    yield break;
            }

            switch(e.Type)
            {
                case eWardenObjectiveEventType.SetTerminalCommand:
                    SetTerminalCommand_Custom(e);
                    yield break;
            }
        }
    }
}


//case eWardenObjectiveEventType.DimensionFlashTeam:
//case eWardenObjectiveEventType.DimensionWarpTeam:
//    // cannot put this event to other places because of `Duration`
//    PlayerAgent localPlayer = PlayerManager.GetLocalPlayerAgent();
//    bool success = false;
//    if (localPlayer != null)
//    {
//        eDimensionIndex flashFromDimensionIndex = localPlayer.DimensionIndex;
//        Dimension flashToDimension;
//        if (Dimension.GetDimension(e.DimensionIndex, out flashToDimension))
//        {
//            Il2CppSystem.ValueTuple<UnityEngine.Vector3, UnityEngine.Vector3> warpPoint1;
//            if (GameStateManager.CurrentStateName == eGameStateName.InLevel && flashToDimension.GetValidDimensionWarpPoint(localPlayer, false, out warpPoint1) && localPlayer.TryWarpTo(e.DimensionIndex, warpPoint1.Item1, warpPoint1.Item2, true))
//                success = true;
//            Dimension flashFromDimension;
//            if (success && Dimension.GetDimension(flashFromDimensionIndex, out flashFromDimension))
//            {
//                if (e.Duration > 0.0)
//                    yield return new UnityEngine.WaitForSeconds(e.Duration);
//                if (GameStateManager.CurrentStateName == eGameStateName.InLevel)
//                {
//                    Il2CppSystem.ValueTuple<UnityEngine.Vector3, UnityEngine.Vector3> warpPoint2;

//                    if (e.Type == eWardenObjectiveEventType.DimensionFlashTeam && flashFromDimension.GetValidDimensionWarpPoint(localPlayer, false, out warpPoint2))
//                        localPlayer.TryWarpTo(flashFromDimensionIndex, warpPoint2.Item1/*position*/, warpPoint2.Item2/*lookDirection*/, true);
//                    if (e.ClearDimension && SNet.IsMaster)
//                    {
//                        Dimension dimension = e.Type == eWardenObjectiveEventType.DimensionFlashTeam ? flashToDimension : flashFromDimension;
//                        for (int index1 = 0; index1 < dimension.Layers.Count; ++index1)
//                        {
//                            LG_Layer layer = dimension.Layers[index1];
//                            for (int index2 = 0; index2 < layer.m_zones.Count; ++index2)
//                            {
//                                LG_Zone zone2 = layer.m_zones[index2];
//                                LG_SecurityDoor door;

//                                Utilities.Utils.TryGetZoneEntranceSecDoor(zone2, out door);

//                                // limited kill
//                                if (door == null // failed to get the door, use vanilla impl. anyway
//                                    || door.m_sync.GetCurrentSyncState().status == eDoorStatus.Open) // door opened, kill all
//                                {
//                                    for (int index3 = 0; index3 < zone2.m_courseNodes.Count; ++index3)
//                                    {
//                                        EnemyAgent[] array = zone2.m_courseNodes[index3].m_enemiesInNode.ToArray();
//                                        int num2 = 0;
//                                        for (int index4 = 0; index4 < array.Length; ++index4)
//                                        {
//                                            EnemyAgent enemyAgent = array[index4];
//                                            if (enemyAgent != null && enemyAgent.Damage != null)
//                                            {
//                                                enemyAgent.Damage.MeleeDamage(float.MaxValue, null, UnityEngine.Vector3.zero, UnityEngine.Vector3.up, 0, 1f, 1f, 1f, 1f, false, DamageNoiseLevel.Normal);
//                                                ++num2;
//                                            }
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//            flashFromDimension = null;
//        }
//        flashToDimension = null;
//    }
//    if (!success)
//    {
//        UnityEngine.Debug.LogError(string.Format("DimensionFlashTeam event tried to warp player {0} to {1} but failed!", localPlayer != null ? localPlayer.PlayerName : "Null", e.DimensionIndex));
//    }
//    break;