//using Il2CppSystem.Collections.Generic;
//using HarmonyLib;
//using LevelGeneration;
//using GameData;
//using LEGACY.Utils;
//using ChainedPuzzles;
//using BepInEx.Logging;

//namespace LEGACY.VanillaFix
//{
//    [HarmonyPatch]
//    [HarmonyWrapSafe]
//    internal class Patch_Debug
//    {
//        [HarmonyPrefix]
//        [HarmonyPatch(typeof(LG_TerminalUniqueCommandsSetupJob), nameof(LG_TerminalUniqueCommandsSetupJob.Build))]
//        private static bool Post_Debug(LG_TerminalUniqueCommandsSetupJob __instance, ref bool __result)
//        {
//            __result = true;
//            if (__instance.m_terminalPlacementData != null && __instance.m_terminalPlacementData.UniqueCommands != null)
//            {
//                for (int index1 = 0; index1 < __instance.m_terminalPlacementData.UniqueCommands.Count; ++index1)
//                {
//                    CustomTerminalCommand uniqueCommand = __instance.m_terminalPlacementData.UniqueCommands[index1];
//                    TERM_Command cmd;
//                    if (__instance.m_terminal.m_command.TryGetUniqueCommandSlot(out cmd))
//                    {
//                        LegacyLogger.Warning($"cmd: {cmd}， {uniqueCommand.Command}");
//                        __instance.m_terminal.m_command.AddCommand(cmd, uniqueCommand.Command, uniqueCommand.CommandDesc, uniqueCommand.SpecialCommandRule, uniqueCommand.CommandEvents, uniqueCommand.PostCommandOutputs);
//                        LegacyLogger.Warning("added cmd");
//                        for (int index2 = 0; index2 < uniqueCommand.CommandEvents.Count; ++index2)
//                        {
//                            WardenObjectiveEventData commandEvent = uniqueCommand.CommandEvents[index2];
//                            if (commandEvent.ChainPuzzle != 0U)
//                            {
//                                ChainedPuzzleDataBlock block = GameDataBlockBase<ChainedPuzzleDataBlock>.GetBlock(commandEvent.ChainPuzzle);
//                                if (block != null)
//                                {
//                                    LegacyLogger.Warning($"puzzle instance, cmd: {cmd}, event index: {index2}");
                                    
//                                    // why TF error??????????
//                                    ChainedPuzzleInstance puzzleInstance = ChainedPuzzleManager.CreatePuzzleInstance(block, __instance.m_terminal.SpawnNode.m_area, __instance.m_terminal.m_wardenObjectiveSecurityScanAlign.position, __instance.m_terminal.m_wardenObjectiveSecurityScanAlign, commandEvent.UseStaticBioscanPoints);
//                                    __instance.m_terminal.SetChainPuzzleForCommand(cmd, index2, puzzleInstance);
//                                }
//                            }
//                        }
//                    }
//                    else
//                        LegacyLogger.Error(string.Format("LG_ComputerTerminal: Could not get any more unique command slots for this terminal!! Have you added too many unique commands to this terminal? (Yours: {0}, Max: {1})", __instance.m_terminalPlacementData.UniqueCommands.Count, 5));
//                }
//            }

//            __instance.m_terminal?.m_command.SetupCommandEvents();
            
//            return false;
//        }
//    }
//}
