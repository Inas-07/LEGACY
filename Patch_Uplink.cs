//using HarmonyLib;
//using LevelGeneration;
//using GameData;
//using LEGACY.Utilities;

//namespace LEGACY.Patch
//{
//    [HarmonyPatch]
//    internal class Patch_Uplink
//    {
//        private static int[] last_break_indices = null;
//        private static WardenObjectiveDataBlock[] dbs = null;

//        private static void checkInit()
//        {
//            if (dbs != null) return;

//            dbs = new WardenObjectiveDataBlock[] { null, null, null };
//            last_break_indices = new int[] { 0, 0, 0 };

//            Logger.Debug("Patch_Uplink initialized");
//        }

//        private static void registerLayerEventsOnActivate(LG_ComputerTerminal __instance)
//        {
//            checkInit();

//            LG_LayerType layer = __instance.SpawnNode.LayerType;

//            WardenObjectiveDataBlock db = dbs[(int)layer];

//            if (db != null) return; // multiple uplink terminals may exist in 1 layer.

//            if (!WardenObjectiveManager.TryGetWardenObjectiveDataForLayer(layer, out db))
//            {
//                Logger.Error("Cannot get WardenObjectiveData for the uplink-terminal, layer: {0}", layer);
//                return;
//            }

//            if (db.Type != eWardenObjectiveType.TerminalUplink && db.Type != eWardenObjectiveType.CorruptedTerminalUplink)
//            {
//                Logger.Debug("{0}: How can you set up a non-uplink terminal in the uplink setup method?", layer.ToString());
//            }

//            dbs[(int)layer] = db;
//            if (db.EventsOnActivate.Count == 0 || db.OnActivateOnSolveItem == true)
//            {
//                last_break_indices[(int)layer] = -1;
//                if(db.EventsOnActivate.Count == 0)
//                {
//                    Logger.Debug("Patch_Uplink: No EventsOnActivate Found for uplink terminal, skipped. Layer: {0}", layer);
//                }
//                else
//                {
//                    Logger.Debug("Patch_Uplink: OnActivateOnSolveItem == true, will use vanilla implmentation. Skipped. Layer: {0}", layer);
//                }
//                return;
//            }

//            Logger.Debug("Registered Uplink EventsOnActivate for the {0} uplink.", layer);
//        }

//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(LG_ComputerTerminal), "SetupAsWardenObjectiveCorruptedTerminalUplink")]
//        private static void Post_SetupAsWardenObjectiveCorruptedTerminalUplink(LG_ComputerTerminal __instance) // do initialization
//        {
//            registerLayerEventsOnActivate(__instance);
//        }

//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(LG_ComputerTerminal), "SetupAsWardenObjectiveTerminalUplink")]
//        private static void Post_LG_ComputerTerminal(LG_ComputerTerminal __instance) // do initialization
//        {
//            registerLayerEventsOnActivate(__instance);
//        }

//        /*
//         You must guarantee that simultaneous uplink connection is logically impossible
//         */
//        [HarmonyPrefix]
//        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), "TerminalUplinkVerify")]
//        private static void Pre_TerminalUplinkVerify(string param1, LG_ComputerTerminalCommandInterpreter __instance)
//        {
//            if (__instance == null) return;

//            // check if it is correct verification
//            Logger.Debug("Input code: {0}. Correct: {1}", param1, __instance.m_terminal.UplinkPuzzle.CurrentRound.CorrectCode);

//            if (!__instance.m_terminal.UplinkPuzzle.CurrentRound.CorrectCode.Equals(param1)) return;

//            Logger.Debug("Verification Code correct. Invoking EventsOnActivate.");

//            LG_LayerType layer = __instance.m_terminal.SpawnNode.LayerType;

//            int last_break_index = last_break_indices[(int)layer];

//            if (last_break_index < 0)
//            {
//                //Logger.Debug("No/No More events in events on activate, skipped");
//                return;
//            }

//            WardenObjectiveDataBlock db = dbs[(int)layer];

//            if (db == null)
//            {
//                Logger.Error("-- FATAL ERROR: UPLINK DB IS NULL! ---");
//                return;
//            }

//            int new_break_index = WardenObjectiveManager.CheckAndExecuteEventsWithBreaks(layer,
//                db.EventsOnActivate, eWardenObjectiveEventTrigger.None,
//                last_break_index, true);

//            last_break_indices[(int)layer] = db.EventsOnActivate.Count == new_break_index ? -1 : new_break_index;

//            Logger.Debug("Layer {0} Uplink EventsOnActivate execution ended, new break_index = {1}", layer.ToString(), new_break_index);
//        }

//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(GS_AfterLevel), "CleanupAfterExpedition")]
//        private static void Post_CleanupAfterExpedition()
//        {
//            if (dbs != null)
//            {
//                last_break_indices = null;
//                dbs = null;
//                Logger.Debug("Patch_Uplink clean up completed");
//            }
//        }
//    }
//}
