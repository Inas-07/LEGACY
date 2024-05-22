using LevelGeneration;
using GameData;
using LEGACY.Utils;
using AK;
using System.Text;
using ExtraObjectiveSetup.Instances;
using EOSExt.Reactor.Managers;
using LEGACY.LegacyOverride.EventScan;
using LEGACY.LegacyOverride.FogBeacon;
using FloLib.Infos;
using SNetwork;
using Player;
using LEGACY.LegacyOverride.ExpeditionSuccessPage;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static void CloseSecurityDoor(WardenObjectiveEventData e)
        {
            LG_Zone zone = null;
            if (Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out zone) == false || zone == null)
            {
                LegacyLogger.Error("CloseSecurityDoor_Custom: Failed to get zone {0}, layer {1}, dimensionIndex {2}", e.LocalIndex, e.Layer, e.DimensionIndex);
                return;
            }

            LG_SecurityDoor door = null;
            if (Helper.TryGetZoneEntranceSecDoor(zone, out door) == false || door == null)
            {
                LegacyLogger.Error("CloseSecurityDoor_Custom: failed to get LG_SecurityDoor!");
                return;
            }

            pDoorState currentSyncState1 = door.m_sync.GetCurrentSyncState();
            if (currentSyncState1.status != eDoorStatus.Open && currentSyncState1.status != eDoorStatus.Opening)
                return;
            LegacyLogger.Debug("Door Closed!");
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

            if (door.ActiveEnemyWaveData != null && door.ActiveEnemyWaveData.HasActiveEnemyWave)
            {
                door.m_sound.Post(EVENTS.MONSTER_RUCKUS_FROM_BEHIND_SECURITY_DOOR_LOOP_START);
            }

            return;
        }

        private static void SetTerminalCommand_Custom(WardenObjectiveEventData e)
        {
            LG_LayerType layer = e.Layer;
            eLocalZoneIndex localIndex = e.LocalIndex;
            eDimensionIndex dimensionIndex = e.DimensionIndex;
            LG_ComputerTerminal terminal;

            switch (e.TerminalCommand)
            {
                case TERM_Command.ReactorStartup:
                case TERM_Command.ReactorShutdown:
                case TERM_Command.ReactorVerify:
                    LG_WardenObjective_Reactor reactor = ReactorInstanceManager.FindVanillaReactor(layer, e.Count); // Find vanilla reactor terminal
                    if (reactor == null)
                    {
                        // find EOSExt.Reactor terminal
                        terminal = Helper.FindTerminal(dimensionIndex, layer, localIndex, e.Count);
                        if(terminal == null)
                        {
                            LegacyLogger.Error($"SetTerminalCommand_Custom: Cannot find reactor for {layer} or instance index ({(dimensionIndex, layer, localIndex, e.Count)})");
                            return;
                        }
                    }
                    else
                    {
                        terminal = reactor.m_terminal; // found vanilla reactor terminal
                    }

                    break;

                default:
                    terminal = Helper.FindTerminal(e.DimensionIndex, e.Layer, e.LocalIndex, e.Count);
                    break;
            }

            if(terminal == null)
            {
                LegacyLogger.Error("SetTerminalCommand_Custom: temrinal not found");
                return;
            }

            if (e.Enabled == true)
            {
                terminal.TrySyncSetCommandShow(e.TerminalCommand);
            }
            else
            {
                terminal.TrySyncSetCommandHidden(e.TerminalCommand);
            }

            LegacyLogger.Debug($"SetTerminalCommand_Custom: Terminal_{terminal.m_serialNumber}, {e.TerminalCommand}");
        }

        private static void ShowTerminalInfoInZone(WardenObjectiveEventData e)
        {
            LG_LayerType layer = e.Layer;
            eLocalZoneIndex localIndex = e.LocalIndex;
            eDimensionIndex dimensionIndex = e.DimensionIndex;

            var terminalsInZone = TerminalInstanceManager.Current.GetInstancesInZone(dimensionIndex, layer, localIndex);
            StringBuilder s = new();
            s.AppendLine();

            for (int index = 0; index < terminalsInZone.Count; index++)
            {
                var t = terminalsInZone[index];
                s.AppendLine($"{t.PublicName}, instance index: {index}");
            }

            LegacyLogger.Debug(s.ToString());
        }

        private static void ToggleEventScanState(WardenObjectiveEventData e)
        {
            EventScanManager.Current.ToggleEventScanState(e.WorldEventObjectFilter, e.Enabled);
        }

        private static void ToggleLevelSpawnedFogBeaconState(WardenObjectiveEventData e)
        {
            LevelSpawnedFogBeaconManager.Current.ToggleLSFBState(e.WorldEventObjectFilter, e.Enabled);
        }
        
        private static void SaveCheckpoint(WardenObjectiveEventData e)
        {
            CheckpointManager.StoreCheckpoint(LocalPlayer.GetEyePosition());
            SNet.Capture.CaptureGameState(eBufferType.Checkpoint);
        }

        private static void SetSuccessPageCustomization(WardenObjectiveEventData e)
        {
            SuccessPageCustomizationManager.Current.ApplyCustomization(e.WorldEventObjectFilter);
        }

        private static void ToggleCamaraShake(WardenObjectiveEventData e)
        {

        }
    }
}