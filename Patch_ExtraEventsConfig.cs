using HarmonyLib;
using Enemies;
using LevelGeneration;
using GameData;
using AIGraph;
using Globals;
using System.Collections.Generic;
using LEGACY.Utilities;

namespace LEGACY.Patch
{
    enum EventType
    {
        CloseSecurityDoor_Custom = 100,
    }

    [HarmonyPatch]
    class Patch_ExtraEventsConfig
    {
        private static bool SetTerminalCommand_Custom(WardenObjectiveEventData eventToTrigger, eWardenObjectiveEventTrigger trigger)
        {
            LG_LayerType layer = eventToTrigger.Layer;
            eLocalZoneIndex localIndex = eventToTrigger.LocalIndex;
            eDimensionIndex dimensionIndex = eventToTrigger.DimensionIndex;
            LG_Zone terminalZone = null;
            Builder.CurrentFloor.TryGetZoneByLocalIndex(dimensionIndex, layer, localIndex, out terminalZone);
            if (terminalZone == null)
            {
                Logger.Error("Failed to get terminal in zone {0}, layer {1}, dimension {2}.", localIndex, layer, dimensionIndex);
                return true;
            }

            if (terminalZone.TerminalsSpawnedInZone == null)
            {
                Logger.Error("ExtraEventsConfig: terminalZone.TerminalsSpawnedInZone == null");
                return true;
            }

            if (terminalZone.TerminalsSpawnedInZone.Count < 1)
            {
                Logger.Error("ExtraEventsConfig: No terminal spawns in the specified zone!");
                return true;
            }

            if (eventToTrigger.Count >= terminalZone.TerminalsSpawnedInZone.Count)
            {
                Logger.Error("ExtraEventsConfig: Invalid event.Count: 0 < event.Count < TerminalsSpawnedInZone.Count should suffice.");
                return true;
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

            WardenObjectiveManager.DisplayWardenIntel(eventToTrigger.Layer, eventToTrigger.WardenIntel);
            Logger.Warning("Succeed setting terminal command visibility!");
            return false;
        }

        private static void CloseSecurityDoor_Custom(WardenObjectiveEventData eventToTrigger, eWardenObjectiveEventTrigger trigger)
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

            WardenObjectiveManager.DisplayWardenIntel(eventToTrigger.Layer, eventToTrigger.WardenIntel);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WardenObjectiveManager), nameof(WardenObjectiveManager.CheckAndExecuteEventsOnTrigger), new System.Type[] {
            typeof(WardenObjectiveEventData),
            typeof(eWardenObjectiveEventTrigger),
            typeof(bool),
            typeof(float)
        })]
        private static bool Pre_CheckAndExecuteEventsOnTrigger(WardenObjectiveManager __instance,
            WardenObjectiveEventData eventToTrigger,
            eWardenObjectiveEventTrigger trigger,
            bool ignoreTrigger = false,
            float currentDuration = 0.0f)
        {
            switch((int)eventToTrigger.Type)
            {
                case (int)eWardenObjectiveEventType.SetTerminalCommand:
                    return SetTerminalCommand_Custom(eventToTrigger, trigger);

                case (int)EventType.CloseSecurityDoor_Custom:
                    if (SNetwork.SNet.IsMaster)
                    {
                        CloseSecurityDoor_Custom(eventToTrigger, trigger);
                    }
                    return false;

                default: return true;
            }
        }
    }
}
