using LevelGeneration;
using GameData;
using LEGACY.Utils;
using AK;
using Player;
using System.Text;
using ExtraObjectiveSetup.Instances;
using LEGACY.LegacyOverride.ExtraExpeditionSettings;
using EOSExt.Reactor.Managers;

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
                    LG_WardenObjective_Reactor reactor = ReactorInstanceManager.FindVanillaReactor(layer); // Find vanilla reactor terminal
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

        // === unused events, and thus not fully implemented ===
        private static void ToggleBioTrackerState(WardenObjectiveEventData e)
        {
            ExpeditionSettingsManager.Current.ToggleBioTrackerState(e.Enabled);
            LegacyLogger.Debug($"ToggleBioTrackerState: Enabled ? - {e.Enabled}");
        }

        private static void WarpTeamsToArea(WardenObjectiveEventData e)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

            // with area unspecified, warp players to random area in zone
            PlayerAgent localPlayer = PlayerManager.GetLocalPlayerAgent();

            if (localPlayer == null)
            {
                LegacyLogger.Error("WarpTeamsToArea: Cannot get local player agent!");
                return;
            }

            eDimensionIndex flashFromDimensionIndex = localPlayer.DimensionIndex;
            Dimension flashToDimension;
            if (Dimension.GetDimension(e.DimensionIndex, out flashToDimension) == false || flashToDimension == null)
            {
                LegacyLogger.Error("WarpTeamsToArea: Cannot find dimension to warp to!");
                return;
            }

            LG_Zone warpToZone;
            if (!Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out warpToZone) || warpToZone == null)
            {
                LegacyLogger.Error($"WarpTeamsToArea: Cannot find target zone! {e.LocalIndex}, {e.Layer}, {e.DimensionIndex}");
                return;
            }

            int areaIndex = e.Count;
            if (areaIndex < 0 || areaIndex >= warpToZone.m_areas.Count)
            {
                LegacyLogger.Warning($"WarpTeamsToArea: invalid area index {areaIndex}, defaulting to first area");
                areaIndex = 0;
            }

            LG_Area warpToArea = warpToZone.m_areas[areaIndex];

            UnityEngine.Vector3 warpToPosition = warpToArea.m_courseNode.GetRandomPositionInside();

            localPlayer.TryWarpTo(e.DimensionIndex, warpToPosition, UnityEngine.Random.onUnitSphere, true);
            LegacyLogger.Debug($"WarpTeamsToArea: warpped to {e.LocalIndex}{'A' + areaIndex}, {e.Layer}, {e.DimensionIndex}");
        }

    }
}