using UnityEngine;
using LevelGeneration;
using ChainedPuzzles;
using GameData;
using Il2CppSystem.Collections.Generic;
using Player;

namespace LEGACY.Utils
{
    public static class Helper
    {
        public static LG_WardenObjective_Reactor FindReactor(LG_LayerType layer)
        {
            LG_WardenObjective_Reactor reactor = null;
            foreach (var keyvalue in WardenObjectiveManager.Current.m_wardenObjectiveItem)
            {
                if (keyvalue.Key.Layer != layer)
                    continue;

                reactor = keyvalue.Value.TryCast<LG_WardenObjective_Reactor>();
                if (reactor == null)
                    continue;

                break;
            }

            return reactor;
        }

        public static bool TryGetComponent<T>(this GameObject obj, out T comp)
        {
            comp = obj.GetComponent<T>();
            return comp != null;
        }

        public static bool IsPlayerInLevel(PlayerAgent player)
        {
            return player.Owner.Load<pGameState>().gameState == eGameStateName.InLevel;
        }

        // By chainedpuzzleinstance, it implies that YOU MUST BE IN THE LEVEL, THE LEVEL MUST BE GENERATED
        // We could thus get the command event list by ActiveExpedition
        public static ChainedPuzzleInstance GetChainedPuzzleForCommandOnTerminal(LG_ComputerTerminal terminal, string command)
        {
            uint layoutID = 0u;

            if (terminal.SpawnNode.m_dimension.IsMainDimension)
            {
                switch (terminal.SpawnNode.LayerType)
                {
                    case LG_LayerType.MainLayer: layoutID = RundownManager.ActiveExpedition.LevelLayoutData; break;
                    case LG_LayerType.SecondaryLayer: layoutID = RundownManager.ActiveExpedition.SecondaryLayout; break;
                    case LG_LayerType.ThirdLayer: layoutID = RundownManager.ActiveExpedition.ThirdLayout; break;
                    default: Logger.Error("Unimplemented layer type."); return null;
                }
            }
            else
            {
                layoutID = terminal.SpawnNode.m_dimension.DimensionData.LevelLayoutData;
            }

            // __instance.m_commandEventMap.TryGetValue is unusable. Get around this by getting it from gamedatablock.
            LevelLayoutDataBlock levellayoutData = LevelLayoutDataBlock.GetBlock(layoutID);
            List<CustomTerminalCommand> UniqueCommands = null;

            // CRITICAL: The order of spawning terminals is the same to that of specifying terminalplacementdatas in the datablock!
            List<LG_ComputerTerminal> TerminalsInZone = terminal.SpawnNode.m_zone.TerminalsSpawnedInZone;
            int TerminalDataIndex = TerminalsInZone.IndexOf(terminal);

            ExpeditionZoneData TargetZoneData = null;
            foreach (ExpeditionZoneData zonedata in levellayoutData.Zones)
            {
                if (zonedata.LocalIndex == terminal.SpawnNode.m_zone.LocalIndex)
                {
                    TargetZoneData = zonedata;
                    break;
                }
            }

            if (TargetZoneData == null)
            {
                Logger.Error("Cannot find target zone data.");
                return null;
            }

            if (TargetZoneData.TerminalPlacements.Count != TerminalsInZone.Count)
            {
                Logger.Error("The numbers of terminal placement and spawn, skipped for the zone terminal.");
                return null;
            }

            UniqueCommands = TargetZoneData.TerminalPlacements[TerminalDataIndex].UniqueCommands;

            if (UniqueCommands.Count == 0) return null;

            List<WardenObjectiveEventData> CommandEvent = null;
            TERM_Command CMD = TERM_Command.None;
            foreach (CustomTerminalCommand UniqueCommand in UniqueCommands)
            {
                if (UniqueCommand.Command == command)
                {
                    CommandEvent = UniqueCommand.CommandEvents;
                    string param1, param2; // unused
                    if (terminal.m_command.TryGetCommand(UniqueCommand.Command, out CMD, out param1, out param2) == false)
                    {
                        Logger.Error("Cannot get TERM_COMMAND for command {0} on the specified terminal.");
                    }

                    break;
                }
            }

            if (CommandEvent == null || CMD == TERM_Command.None) return null;
            if (CMD != TERM_Command.UniqueCommand1 
                && CMD != TERM_Command.UniqueCommand2 
                && CMD != TERM_Command.UniqueCommand3 
                && CMD != TERM_Command.UniqueCommand4 
                && CMD != TERM_Command.UniqueCommand5)
            {
                return null;
            }


            ChainedPuzzleInstance result = null;

            for (int eventIndex = 0; eventIndex < CommandEvent.Count; eventIndex++)
            {
                if (CommandEvent[eventIndex].ChainPuzzle == 0) continue;

                if (terminal.TryGetChainPuzzleForCommand(CMD, eventIndex, out result) == true && result != null)
                {
                    break;
                }
            }

            return result;
        }

        public static bool TryGetZoneEntranceSecDoor(LG_Zone zone, out LG_SecurityDoor door)
        {
            if (zone == null)
            {
                door = null;
                return false;
            }
            if (zone.m_sourceGate == null)
            {
                door = null;
                return false;
            }
            if (zone.m_sourceGate.SpawnedDoor == null)
            {
                door = null;
                return false;
            }
            door = zone.m_sourceGate.SpawnedDoor.TryCast<LG_SecurityDoor>();
            return door != null;
        }

        internal static bool isSecDoorToZoneOpened(LG_Zone zone14)
        {
            LG_SecurityDoor door = null;
            if (TryGetZoneEntranceSecDoor(zone14, out door) == false || door == null)
                return false;

            return door.m_sync.GetCurrentSyncState().status == eDoorStatus.Open;
        }

        public static System.Collections.Generic.List<T> cast<T>(Il2CppSystem.Collections.Generic.List<T> list)
        {
            System.Collections.Generic.List<T> res = new();

            foreach(T obj in list)
            {
                res.Add(obj);
            }

            return res;
        }


    }


    public enum MainLayerID 
    {
        L2E1 = 60000,
        L2E2 = 61000,
        L2E3 = 62000,
        L3E2 = 40000,
    }
}

