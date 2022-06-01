//using Il2CppSystem.Collections.Generic; // dont use System.Generic.Collections
//using HarmonyLib;
//using LevelGeneration;
//using GameData;
//using SNetwork;
//using LEGACY.Utilities;

//namespace LEGACY.Patch
//{
//    /*
//     Every time an powercell is inserted, LG_PowerGeneratorCluster.Setup() will be invoked.
//     May turn to that later. But what about multi-layer support?
//     */

//    [HarmonyPatch]
//    internal class Patch_CentralGenCluster
//    {
//        private static HashSet<LG_PowerGenerator_Core> genSet = null;
//        private static int[] last_break_indices = null;
//        private static WardenObjectiveDataBlock[] dbs = null;
//        private static bool skippedNextCall = true;

//        private static void checkInit()
//        {
//            if (genSet != null) return;

//            genSet = new HashSet<LG_PowerGenerator_Core>();
//            last_break_indices = new int[] { 0, 0, 0 };
//            dbs = new WardenObjectiveDataBlock[] { null, null, null };
//            skippedNextCall = SNet.IsMaster ? false : true;

//            Logger.Debug("Patch_CentralGenCluster initialized");
//        }

//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(LG_PowerGenerator_Core), nameof(LG_PowerGenerator_Core.SetupAsCentralGeneratorClusterObjective))]
//        private static void Post_SetupAsCentralGeneratorClusterObjective(LG_PowerGenerator_Core __instance)
//        {
//            if (__instance == null) return;

//            checkInit();

//            genSet.Add(__instance);

//            LG_LayerType layer = __instance.SpawnNode.LayerType;

//            WardenObjectiveDataBlock db = dbs[(int)layer];

//            if (db == null)
//            {
//                if (!WardenObjectiveManager.TryGetWardenObjectiveDataForLayer(layer, out db))
//                {
//                    Logger.Error("Cannot get WardenObjectiveData for the GC, layer: {0}", layer);
//                    return;
//                }

//                dbs[(int)layer] = db;
//                Logger.Debug("Register DB in {0}, EventsOnActivate count: {1}", layer, db.EventsOnActivate.Count);

//                // dont use default impl.
//                if (db.EventsOnActivate.Count == 0 || db.OnActivateOnSolveItem == true)
//                {
//                    last_break_indices[(int)layer] = -1;
//                    if (db.EventsOnActivate.Count == 0)
//                    {
//                        Logger.Debug("Patch_CentralGenCluster: No EventsOnActivate Found for GC, skipped. Layer: {0}", layer);
//                    }
//                    else
//                    {
//                        Logger.Debug("Patch_CentralGenCluster: OnActivateOnSolveItem == true, will use vanilla implmentation. Skipped. Layer: {0}", layer);
//                    }
//                    return;
//                }
//            }

//            Logger.Debug("Registered generator in {0} for the layer generator-cluster.", layer.ToString());
//        }

//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(LG_PowerGenerator_Core), nameof(LG_PowerGenerator_Core.OnStateChange))]
//        private static void Post_OnStateChange(LG_PowerGenerator_Core __instance)
//        {
//            if (genSet == null) return;

//            if (__instance == null)
//            {
//                Logger.Error("In Post_OnStateChange, argument __instance == null!");
//                return;
//            }

//            if (skippedNextCall == true)
//            {
//                skippedNextCall = false;
//                Logger.Log("client will execute events 1 times ahead than host. skipped once.");
//                return;
//            };


//            if (!genSet.Contains(__instance))
//            {
//                //Logger.Debug("Not target generator, skipped");
//                return;
//            }

//            LG_LayerType layer = __instance.SpawnNode.LayerType;

//            int last_break_index = last_break_indices[(int)layer];

//            // no events in events on activate. skipped
//            if (last_break_index < 0)
//            {
//                //Logger.Debug("No/No More events in events on activate, skipped");
//                return;
//            }

//            WardenObjectiveDataBlock db = dbs[(int)layer];

//            if (db == null)
//            {
//                Logger.Error("FATAL: No registered WardenObjectiveData for the GC!!");
//                return;
//            }

//            // dont use default impl.
//            //if (db.OnActivateOnSolveItem == true) return;

//            int new_break_index = WardenObjectiveManager.CheckAndExecuteEventsWithBreaks(layer,
//                db.EventsOnActivate, eWardenObjectiveEventTrigger.None,
//                last_break_index, true);

//            last_break_indices[(int)layer] = db.EventsOnActivate.Count == new_break_index ? -1 : new_break_index;

//            if (SNet.IsMaster == false)
//            {
//                skippedNextCall = true;
//            }

//            Logger.Debug("Layer {0} GC EventsOnActivate execution ended, new break_index = {1}", layer.ToString(), new_break_index);
//        }


//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(GS_AfterLevel), nameof(GS_AfterLevel.CleanupAfterExpedition))]
//        private static void Post_CleanupAfterExpedition()
//        {
//            if (genSet != null)
//            {
//                genSet = null;
//                last_break_indices = null;
//                dbs = null;
//                Logger.Debug("Clean up completed");
//            }
//        }

//    }
//}
