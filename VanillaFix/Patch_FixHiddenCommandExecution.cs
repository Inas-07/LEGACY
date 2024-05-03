// NOTE: moved to ExtraObjectiveSetup

//using HarmonyLib;
//using LevelGeneration;
//using Localization;
//namespace LEGACY.VanillaFix
//{
//    [HarmonyPatch]
//    internal static class Patch_FixHiddenCommandExecution
//    {
//        [HarmonyPrefix]
//        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReceiveCommand))]
//        private static void Pre_TerminalInterpreter_ReceiveCommand(LG_ComputerTerminalCommandInterpreter __instance, ref TERM_Command cmd)
//        {
//            switch (cmd)
//            {
//                // on only handle basic command
//                case TERM_Command.Help:
//                case TERM_Command.Commands:
//                case TERM_Command.Cls:
//                case TERM_Command.Exit:
//                //case TERM_Command.Open:
//                //case TERM_Command.Close:
//                //case TERM_Command.EmptyLine:
//                //case TERM_Command.InvalidCommand:
//                //case TERM_Command.DownloadData:
//                case TERM_Command.ViewSecurityLog:
//                case TERM_Command.DisableAlarm:
//                case TERM_Command.Locate:
//                //case TERM_Command.ActivateBeacon:
//                case TERM_Command.ShowList:
//                case TERM_Command.Query:
//                case TERM_Command.Ping:
//                case TERM_Command.ReactorStartup:
//                case TERM_Command.ReactorVerify:
//                case TERM_Command.ReactorShutdown:
//                case TERM_Command.WardenObjectiveSpecialCommand:
//                case TERM_Command.TerminalUplinkConnect:
//                case TERM_Command.TerminalUplinkVerify:
//                case TERM_Command.TerminalUplinkConfirm:
//                case TERM_Command.ListLogs:
//                case TERM_Command.ReadLog:
//                //case TERM_Command.TryUnlockingTerminal:
//                case TERM_Command.WardenObjectiveGatherCommand:
//                case TERM_Command.TerminalCorruptedUplinkConnect:
//                case TERM_Command.TerminalCorruptedUplinkVerify:
//                case TERM_Command.Info:
//                    if (__instance.m_terminal.CommandIsHidden(cmd))
//                    {
//                        cmd = TERM_Command.InvalidCommand;
//                    }
//                    return;
//            }
//        }
//    }
//}
