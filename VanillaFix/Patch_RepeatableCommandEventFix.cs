using Il2CppSystem.Collections.Generic;
using HarmonyLib;
using LevelGeneration;
using GameData;
using LEGACY.Utils;
using ChainedPuzzles;

namespace LEGACY.VanillaFix
{
    [HarmonyPatch]
    [HarmonyWrapSafe]
    internal class Patch_RepeatableCommandEventFix
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.SetupCommandEvents))]
        private static void Post_ResetupChainedPuzzleAfterExecution(LG_ComputerTerminalCommandInterpreter __instance)
        {
            uint layoutID = 0u;

            if (__instance.m_terminal.ConnectedReactor != null) return;
            if (__instance.m_terminal.SpawnNode.m_dimension.IsMainDimension)
            {
                switch (__instance.m_terminal.SpawnNode.LayerType)
                {
                    case LG_LayerType.MainLayer: layoutID = RundownManager.ActiveExpedition.LevelLayoutData; break;
                    case LG_LayerType.SecondaryLayer: layoutID = RundownManager.ActiveExpedition.SecondaryLayout; break;
                    case LG_LayerType.ThirdLayer: layoutID = RundownManager.ActiveExpedition.ThirdLayout; break;
                    default: LegacyLogger.Error("Unimplemented layer type."); return;
                }
            }
            else
            {
                layoutID = __instance.m_terminal.SpawnNode.m_dimension.DimensionData.LevelLayoutData;
            }

            // __instance.m_commandEventMap.TryGetValue is unusable. Get around this by getting it from gamedatablock.
            LevelLayoutDataBlock levellayoutData = LevelLayoutDataBlock.GetBlock(layoutID);
            List<CustomTerminalCommand> UniqueCommands = null;

            // CRITICAL: The order of spawning terminals is the same to that of specifying terminalplacementdatas in the datablock!
            List<LG_ComputerTerminal> TerminalsInZone = __instance.m_terminal.SpawnNode.m_zone.TerminalsSpawnedInZone;
            int TerminalDataIndex = TerminalsInZone.IndexOf(__instance.m_terminal);

            ExpeditionZoneData TargetZoneData = null;
            foreach (ExpeditionZoneData zonedata in levellayoutData.Zones)
            {
                if (zonedata.LocalIndex == __instance.m_terminal.SpawnNode.m_zone.LocalIndex)
                {
                    TargetZoneData = zonedata;
                    break;
                }
            }

            if (TargetZoneData == null)
            {
                LegacyLogger.Error("Cannot find target zone data.");
                return;
            }

            if(TerminalDataIndex >= TargetZoneData.TerminalPlacements.Count)
            {
                LegacyLogger.Debug("RepeatableCommand: TerminalDataIndex >= TargetZoneData.TerminalPlacements.Count: found sec-door terminal, skipping");
                return;
            }

            UniqueCommands = TargetZoneData.TerminalPlacements[TerminalDataIndex].UniqueCommands;

            if (UniqueCommands.Count == 0) return;

            // Fix all ChainedPuzzle on this terminal
            foreach (CustomTerminalCommand UniqueCommand in UniqueCommands)
            {
                if (UniqueCommand.SpecialCommandRule != TERM_CommandRule.Normal) continue;

                TERM_Command CMD;
                string param1, param2; // unused
                if (__instance.TryGetCommand(UniqueCommand.Command, out CMD, out param1, out param2) == false)
                    continue;

                if (CMD != TERM_Command.UniqueCommand1 && CMD != TERM_Command.UniqueCommand2 && CMD != TERM_Command.UniqueCommand3 && CMD != TERM_Command.UniqueCommand4 && CMD != TERM_Command.UniqueCommand5)
                    continue;

                ChainedPuzzleInstance OldCPInstance = null;
                List<WardenObjectiveEventData> CommandEvents = UniqueCommand.CommandEvents;

                int eventIndex;
                for (eventIndex = 0; eventIndex < CommandEvents.Count; eventIndex++)
                {
                    if (CommandEvents[eventIndex].ChainPuzzle == 0) continue;

                    if (__instance.m_terminal.TryGetChainPuzzleForCommand(CMD, eventIndex, out OldCPInstance) == true && OldCPInstance != null)
                    {
                        break;
                    }
                }

                if (OldCPInstance == null) continue;

                OldCPInstance.OnPuzzleSolved += new System.Action(() =>
                {
                    // TODO: fix this
                    ChainedPuzzleInstance newCPInstance = ChainedPuzzleManager.CreatePuzzleInstance(OldCPInstance.Data, OldCPInstance.m_sourceArea, __instance.m_terminal.m_wardenObjectiveSecurityScanAlign.position, __instance.m_terminal.m_wardenObjectiveSecurityScanAlign, CommandEvents[eventIndex].UseStaticBioscanPoints);

                    Il2CppSystem.ValueTuple<TERM_Command, int> valueTuple = null;

                    foreach (var entry in __instance.m_terminal.m_commandToChainPuzzleMap.entries)
                    {
                        ChainedPuzzleInstance CPInstance = entry.value;
                        if (CPInstance.m_sourceArea == OldCPInstance.m_sourceArea && CPInstance.m_parent == OldCPInstance.m_parent)
                        {
                            valueTuple = entry.key;
                            break;
                        }
                    }

                    if (valueTuple != null)
                    {
                        __instance.m_terminal.m_commandToChainPuzzleMap.Remove(valueTuple);
                        __instance.m_terminal.SetChainPuzzleForCommand(CMD, eventIndex, newCPInstance);

                        newCPInstance.OnPuzzleSolved = OldCPInstance.OnPuzzleSolved;
                    }
                    else
                    {
                        LegacyLogger.Error("value tuple is null!");
                    }
                });
            }
        }
    }
}
