using Il2CppSystem.Collections.Generic;
using HarmonyLib;
using GameData;
using LEGACY.Utils;
using SNetwork;
using ChainedPuzzles;
using LevelGeneration;
using Localization;
using GTFO.API;

namespace LEGACY.HardcodedBehaviours
{
    [HarmonyPatch]
    internal class Patch_HardcodedBehaviour_L3E2
    {
        private static bool is_L3E2 = false;

        private static ushort[] Colosseum_WaveEventIDs = null;
        //private static ushort[] Colosseum2_WaveEventIDs = null;

        //private static ChainedPuzzleInstance puzzle1 = null, puzzle2 = null;

        private static System.Collections.Generic.List<ChainedPuzzleInstance> puzzles1 = null, puzzles2 = null;

        private static List<WardenObjectiveEventData> Colosseum1Events = null;
        private static List<WardenObjectiveEventData> Colosseum2Events = null;

        private static uint MainLayerID = 40000u;
        private static uint SecondaryLayoutID = 40001u;

        private static ChainedPuzzleInstance[] DIMENSION_Z0_TERMINAL_CPS = null;

        private static CarryItemPickup_Core[] PowerCells_InDimension = null;

        private static string CustomTextToPrefix = null;
        private static uint CustomTextPID = 3001u;

        enum PuzzleID
        {
            Colosseum1_1 = 40100,
            Colosseum1_2 = 40101,
            Colosseum1_3 = 40102,
            Colosseum1_4 = 40103,

            Colosseum2_1 = 40200,
            Colosseum2_2 = 40201,
            Colosseum2_3 = 40202,
            Colosseum2_4 = 40203
        }

        // survivial wave settings ID
        enum WaveSettings
        {
            Trickle_12_20_ELEVATOR = 240,
            Trickle_6_40_ELEVATOR = 241,
            Trickle_3_60_ELEVATOR = 242,
            Trickle_2_40_ELEVATOR = 243,
            Trickle_6_30_ELEVATOR = 244,
            FINITE_1_ELEVATOR = 245,
            Trickle_1_60_SPAWNPOINT = 246,
            Trickle_8_30_ELEVATOR = 247,
            Trickle_4_15_SPAWNPOINT = 248,
            Trickle_3_20_SPAWNPOINT = 249,
            Trickle_2_30_SPAWNPOINT_MODIFIED = 250,
            Trickle_2_40_SPAWNPOINT = 251,
            Apex_Surge_SPAWNPOINT = 252,
            Trickle_8_30_SPAWNPOINT = 253,
            Trickle_6_30_SPAWNPOINT = 254,
            Trickle_2_30_SPAWNPOINT = 255,
        }

        enum WavePopulation
        {
            STRIKERS_ONLY = 18,
            BULLRUSH_ONLY = 5,
            BULLRUSH_BOSS_ONLY = 15,
            HYBRID_ONLY = 27,
            SHADOWS_ONLY = 7,
            SHADOWS_GIANT_ONLY = 28,
            TANK = 16,
            FLYERS_BIG = 53,
            BASELINE = 1
        }

        private static bool WaveSpawnedSuccessful(ushort[] WaveEventIDs)
        {
            if (WaveEventIDs == null) return false;

            bool succ = true;
            foreach (ushort id in WaveEventIDs)
            {
                if (id == 0)
                {
                    succ = false;
                    break;
                }
            }

            if (!succ)
            {

                string msg = "Critical: Failed to spawn survival wave! IDs: [";
                foreach (ushort id in WaveEventIDs)
                {
                    msg += id + ", ";
                }
                msg.Remove(msg.Length - 2);
                msg += "]";
                Logger.Error(msg);
            }

            return succ;
        }

        private static bool SetupColosseumsPreparation()
        {
            // ------------------------------------------------------
            //   Colosseum Events Setup
            // ------------------------------------------------------
            LevelLayoutDataBlock db = LevelLayoutDataBlock.GetBlock(SecondaryLayoutID);

            List<CustomTerminalCommand> unusedCommands = db.Zones[8].TerminalPlacements[0].UniqueCommands;

            Colosseum1Events = unusedCommands[0].CommandEvents;
            Colosseum2Events = unusedCommands[1].CommandEvents;
            // ------------------------------------------------------
            //   Colosseum Events Setup END
            // ------------------------------------------------------

            // ------------------------------------------------------
            //   Colosseums preparation setup
            // ------------------------------------------------------
            TerminalPlacementData term_placement1 = db.Zones[3].TerminalPlacements[0];

            List<WardenObjectiveEventData> CommandEvent_1 = term_placement1.UniqueCommands[0].CommandEvents;

            LG_Zone zone123 = null, zone129 = null;
            Builder.CurrentFloor.TryGetZoneByLocalIndex(eDimensionIndex.Reality, LG_LayerType.SecondaryLayer, eLocalZoneIndex.Zone_3, out zone123);
            Builder.CurrentFloor.TryGetZoneByLocalIndex(eDimensionIndex.Reality, LG_LayerType.SecondaryLayer, eLocalZoneIndex.Zone_9, out zone129);

            if (zone123 == null || zone129 == null)
            {
                Logger.Error("Builder failed to get the 2 zones");
                return false;
            }

            LG_ComputerTerminal terminal1 = zone123.TerminalsSpawnedInZone[0], terminal2 = zone129.TerminalsSpawnedInZone[0];
            TERM_Command CMD1 = TERM_Command.None;
            string param1, param2;
            terminal1.m_command.TryGetCommand(term_placement1.UniqueCommands[0].Command, out CMD1, out param1, out param2);

            if (CMD1 == TERM_Command.None)
            {
                Logger.Error("Failed to get TERM_Command");
                return false;
            }

            ChainedPuzzleInstance CP_1_1 = null;
            for (int eventIndex = 0; eventIndex < CommandEvent_1.Count; eventIndex++)
            {
                terminal1.TryGetChainPuzzleForCommand(CMD1, eventIndex, out CP_1_1);

                if (CP_1_1 != null) break;
            }

            if (CP_1_1 == null)
            {
                Logger.Error("Failed to get colosseum 1 chained puzzle instance.");
                return false;
            }

            //  create all puzzle instances
            puzzles1 = new System.Collections.Generic.List<ChainedPuzzleInstance>();
            LG_Area puzzles1_sourceArea = CP_1_1.m_sourceArea;
            UnityEngine.Transform puzzles1_Parent = terminal1.m_wardenObjectiveSecurityScanAlign;
            ChainedPuzzleInstance CP_1_2 = ChainedPuzzleManager.CreatePuzzleInstance(ChainedPuzzleDataBlock.GetBlock((uint)PuzzleID.Colosseum1_2), puzzles1_sourceArea, puzzles1_Parent.position, puzzles1_Parent);
            ChainedPuzzleInstance CP_1_3 = ChainedPuzzleManager.CreatePuzzleInstance(ChainedPuzzleDataBlock.GetBlock((uint)PuzzleID.Colosseum1_3), puzzles1_sourceArea, puzzles1_Parent.position, puzzles1_Parent);
            ChainedPuzzleInstance CP_1_4 = ChainedPuzzleManager.CreatePuzzleInstance(ChainedPuzzleDataBlock.GetBlock((uint)PuzzleID.Colosseum1_4), puzzles1_sourceArea, puzzles1_Parent.position, puzzles1_Parent);
            puzzles1.Add(CP_1_1);
            puzzles1.Add(CP_1_2);
            puzzles1.Add(CP_1_3);
            puzzles1.Add(CP_1_4);

            ChainedPuzzleInstance CP_2_1 = ChainedPuzzleManager.CreatePuzzleInstance(ChainedPuzzleDataBlock.GetBlock((uint)PuzzleID.Colosseum2_1), terminal2.SpawnNode.m_area, terminal2.m_wardenObjectiveSecurityScanAlign.position, terminal2.m_wardenObjectiveSecurityScanAlign);
            puzzles2 = new System.Collections.Generic.List<ChainedPuzzleInstance>();
            LG_Area puzzles2_sourceArea = CP_2_1.m_sourceArea;
            UnityEngine.Transform puzzles2_Parent = terminal2.m_wardenObjectiveSecurityScanAlign;
            ChainedPuzzleInstance CP_2_2 = ChainedPuzzleManager.CreatePuzzleInstance(ChainedPuzzleDataBlock.GetBlock((uint)PuzzleID.Colosseum2_2), puzzles2_sourceArea, puzzles2_Parent.position, puzzles2_Parent);
            ChainedPuzzleInstance CP_2_3 = ChainedPuzzleManager.CreatePuzzleInstance(ChainedPuzzleDataBlock.GetBlock((uint)PuzzleID.Colosseum2_3), puzzles2_sourceArea, puzzles2_Parent.position, puzzles2_Parent);
            ChainedPuzzleInstance CP_2_4 = ChainedPuzzleManager.CreatePuzzleInstance(ChainedPuzzleDataBlock.GetBlock((uint)PuzzleID.Colosseum2_4), puzzles2_sourceArea, puzzles2_Parent.position, puzzles2_Parent);
            puzzles2.Add(CP_2_1);
            puzzles2.Add(CP_2_2);
            puzzles2.Add(CP_2_3);
            puzzles2.Add(CP_2_4);

            // ------------------------------------------------------
            //          Colosseums preparation setup END
            // ------------------------------------------------------
            return true;
        }

        private static bool SetupDimensionWarpReqItem()
        {
            LG_Zone dim_Z12 = null;

            if (Builder.CurrentFloor.TryGetZoneByLocalIndex(eDimensionIndex.Dimension_1, LG_LayerType.MainLayer, eLocalZoneIndex.Zone_0, out dim_Z12) == false || dim_Z12 == null || dim_Z12.TerminalsSpawnedInZone.Count <= 0)
            {
                Logger.Warning("Cound not get dimension z12 or the terminal in zone! Omitted Item Requirement for the scan");
                return false;
            }

            if (dim_Z12.TerminalsSpawnedInZone.Count != 1)
            {
                Logger.Warning("Multiple terminal found in dim_z12, which is not as designed. Omitted Item Requirement for the scan");
                return false;
            }

            DIMENSION_Z0_TERMINAL_CPS = new ChainedPuzzleInstance[3] { null, null, null };
            DIMENSION_Z0_TERMINAL_CPS[0] = Helper.GetChainedPuzzleForCommandOnTerminal(dim_Z12.TerminalsSpawnedInZone[0], "RESTORE_MATTER_WAVE_PHASE_1");
            DIMENSION_Z0_TERMINAL_CPS[1] = Helper.GetChainedPuzzleForCommandOnTerminal(dim_Z12.TerminalsSpawnedInZone[0], "RESTORE_MATTER_WAVE_PHASE_2");
            DIMENSION_Z0_TERMINAL_CPS[2] = Helper.GetChainedPuzzleForCommandOnTerminal(dim_Z12.TerminalsSpawnedInZone[0], "RESTORE_MATTER_WAVE_PHASE_3");

            if (DIMENSION_Z0_TERMINAL_CPS[0] == null || DIMENSION_Z0_TERMINAL_CPS[1] == null || DIMENSION_Z0_TERMINAL_CPS[2] == null)
            {
                Logger.Error("Could not get chained puzzles for commands on terminal in dim_z12! This should not happed! Omitting Item Requirement!");
                Logger.Error("0 == {0}", DIMENSION_Z0_TERMINAL_CPS[0]);
                Logger.Error("1 == {0}", DIMENSION_Z0_TERMINAL_CPS[1]);
                Logger.Error("2 == {0}", DIMENSION_Z0_TERMINAL_CPS[2]);
                return false;
            }

            PowerCells_InDimension = new CarryItemPickup_Core[3] { null, null, null };

            foreach (CarryItemPickup_Core cell in ProgressionObjective_GeneratorCell.m_allCellsInLevel)
            {
                if (cell.SpawnNode.m_dimension.DimensionIndex != eDimensionIndex.Dimension_1) continue;

                if (cell.SpawnNode.m_zone.LocalIndex == eLocalZoneIndex.Zone_4)
                {
                    PowerCells_InDimension[0] = cell;
                }
                else if (cell.SpawnNode.m_zone.LocalIndex == eLocalZoneIndex.Zone_2)
                {
                    PowerCells_InDimension[1] = cell;
                }
                else if (cell.SpawnNode.m_zone.LocalIndex == eLocalZoneIndex.Zone_3)
                {
                    PowerCells_InDimension[2] = cell;
                }
            }

            DIMENSION_Z0_TERMINAL_CPS[0].AddRequiredItems(new iWardenObjectiveItem[1] { new iWardenObjectiveItem(PowerCells_InDimension[0].Pointer) });
            DIMENSION_Z0_TERMINAL_CPS[1].AddRequiredItems(new iWardenObjectiveItem[1] { new iWardenObjectiveItem(PowerCells_InDimension[1].Pointer) });
            DIMENSION_Z0_TERMINAL_CPS[2].AddRequiredItems(new iWardenObjectiveItem[1] { new iWardenObjectiveItem(PowerCells_InDimension[2].Pointer) });

            return true;
        }

        private static void ActivateDuoScans()
        {
            if (puzzles1[1].IsSolved && puzzles2[0].IsSolved)
            {
                Logger.Log("puzzles1 Trio and puzzles2 Solo solved. Activating Duo scans for both");
            }
            else
            {
                Logger.Error("Either puzzles1 Trio or puzzles2 Solo is not solved but Duo scans are gonna activated!");
                Logger.Error("Will proceed Duo Scan activations.");
                Logger.Error("puzzles1[1].IsSolved ? {0}. puzzles2[0].IsSolved ? {1}", puzzles1[1].IsSolved, puzzles2[0].IsSolved);
            }

            puzzles1[2].AttemptInteract(eChainedPuzzleInteraction.Activate);
            puzzles2[1].AttemptInteract(eChainedPuzzleInteraction.Activate);

            // Descend fog
            WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum1Events, eWardenObjectiveEventTrigger.OnMid, false);

            if (SNet.IsMaster)
            {
                if (Colosseum_WaveEventIDs == null)
                {
                    Logger.Error("Wave Event ID not stored!");
                    return;
                }
                StopSpecifiedWaves(Colosseum_WaveEventIDs);

                Colosseum_WaveEventIDs = new ushort[3] { 0, 0, 0 };
                // colosseum 1 waves
                Colosseum_WaveEventIDs[0] = StartSurvivalWave(puzzles1[2], (uint)WaveSettings.Trickle_6_30_ELEVATOR, (uint)WavePopulation.BULLRUSH_ONLY, SurvivalWaveSpawnType.FromElevatorDirection);
                Colosseum_WaveEventIDs[1] = StartSurvivalWave(puzzles1[2], (uint)WaveSettings.FINITE_1_ELEVATOR, (uint)WavePopulation.TANK, SurvivalWaveSpawnType.FromElevatorDirection);
                // colosseum 2 waves
                Colosseum_WaveEventIDs[2] = StartSurvivalWave(puzzles2[1], (uint)WaveSettings.Trickle_6_30_SPAWNPOINT, (uint)WavePopulation.BULLRUSH_ONLY, SurvivalWaveSpawnType.OnSpawnPoints);

                if (!WaveSpawnedSuccessful(Colosseum_WaveEventIDs)) return;
            }
        }

        private static void ActivateSoloTrioScan()
        {
            if (puzzles1[2].IsSolved && puzzles2[1].IsSolved)
            {
                Logger.Log("puzzles1 & puzzles2 Duo solved. Activating Trio-Solo scans");
            }
            else
            {
                Logger.Error("Either puzzles1 Duo or puzzles2 Duo is not solved but Solo-Trio scan is gonna activated!");
                Logger.Error("Will proceed Solo-Trio Scan activations.");
                Logger.Error("puzzles1[2].IsSolved ? {0}. puzzles2[1].IsSolved ? {1}", puzzles1[2].IsSolved, puzzles2[1].IsSolved);
            }

            puzzles1[3].AttemptInteract(eChainedPuzzleInteraction.Activate);
            puzzles2[2].AttemptInteract(eChainedPuzzleInteraction.Activate);

            WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum1Events, eWardenObjectiveEventTrigger.OnEnd, false);
            //Utils.CheckAndExecuteEventsOnTrigger(Colosseum1Events, eWardenObjectiveEventTrigger.OnEnd, false);

            if (SNet.IsMaster)
            {
                if (Colosseum_WaveEventIDs == null)
                {
                    Logger.Error("Wave Event ID not stored!");
                    return;
                }
                StopSpecifiedWaves(Colosseum_WaveEventIDs);

                Colosseum_WaveEventIDs = new ushort[3] { 0, 0, 0 };
                // Colosseum 1 waves
                Colosseum_WaveEventIDs[0] = StartSurvivalWave(puzzles1[3], (uint)WaveSettings.Trickle_8_30_ELEVATOR, (uint)WavePopulation.BASELINE, SurvivalWaveSpawnType.FromElevatorDirection);
                // Colosseum 2 waves
                Colosseum_WaveEventIDs[0] = StartSurvivalWave(puzzles2[2], (uint)WaveSettings.Trickle_2_30_SPAWNPOINT, (uint)WavePopulation.HYBRID_ONLY, SurvivalWaveSpawnType.OnSpawnPoints);
                Colosseum_WaveEventIDs[1] = StartSurvivalWave(puzzles2[2], (uint)WaveSettings.Trickle_2_30_SPAWNPOINT, (uint)WavePopulation.BULLRUSH_BOSS_ONLY, SurvivalWaveSpawnType.OnSpawnPoints);

                if (!WaveSpawnedSuccessful(Colosseum_WaveEventIDs)) return;
            }
        }

        private static void ActivateLastFullTeamScan()
        {
            if (puzzles1[3].IsSolved && puzzles2[2].IsSolved)
            {
                Logger.Log("puzzles1 Solo and Puzzle2 Trio solved. Activating last Full team scans");
            }
            else
            {
                Logger.Error("Either puzzles1 Solo or puzzles2 Trio is not solved but Full-team scan is gonna activated!");
                Logger.Error("Will proceed Full team Scan activations.");
                Logger.Error("puzzles1[3].IsSolved ? {0}. puzzles2[2].IsSolved ? {1}", puzzles1[3].IsSolved, puzzles2[2].IsSolved);
            }

            puzzles2[3].AttemptInteract(eChainedPuzzleInteraction.Activate);

            if (SNet.IsMaster)
            {
                if (Colosseum_WaveEventIDs == null)
                {
                    Logger.Error("Wave Event ID not stored!");
                    return;
                }
                StopSpecifiedWaves(Colosseum_WaveEventIDs);

                Colosseum_WaveEventIDs = new ushort[1] { 0 };
                Colosseum_WaveEventIDs[0] = StartSurvivalWave(puzzles2[3], (uint)WaveSettings.Apex_Surge_SPAWNPOINT, (uint)WavePopulation.BASELINE, SurvivalWaveSpawnType.OnSpawnPoints);

                if (!WaveSpawnedSuccessful(Colosseum_WaveEventIDs)) return;
            }
        }

        private static void OnBuildDone()
        {
            is_L3E2 = RundownManager.ActiveExpedition.LevelLayoutData == MainLayerID;
            if (!is_L3E2) return;
            Logger.Log("In Legacy L3-E2, Setting up custom level behaviour!");
            if (SetupColosseumsPreparation() == false) return;
            if (SetupDimensionWarpReqItem() == false) return;


            // ----------------------------------------------------
            //      OnPuzzleSolved: puzzles1 full team config
            // ----------------------------------------------------
            puzzles1[0].add_OnPuzzleSolved(new System.Action(() =>
            {
                Logger.Log("Puzzles1 Full Team Solved: Activating puzzles1 Trio and puzzles2 Solo.");
                puzzles1[1].AttemptInteract(eChainedPuzzleInteraction.Activate);
                puzzles2[0].AttemptInteract(eChainedPuzzleInteraction.Activate);

                Logger.Log("Fog descending for Puzzle1 Trio");

                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum1Events, eWardenObjectiveEventTrigger.OnStart, false);

                if (SNet.IsMaster)
                {
                    if (Colosseum_WaveEventIDs == null)
                    {
                        Logger.Error("Wave Event ID not stored!");
                        return;
                    }
                    StopSpecifiedWaves(Colosseum_WaveEventIDs);

                    Colosseum_WaveEventIDs = new ushort[3] { 0, 0, 0 };
                    // colosseum 1 waves
                    Colosseum_WaveEventIDs[0] = StartSurvivalWave(puzzles1[0], (uint)WaveSettings.Trickle_12_20_ELEVATOR, (uint)WavePopulation.SHADOWS_ONLY, SurvivalWaveSpawnType.FromElevatorDirection);
                    Colosseum_WaveEventIDs[1] = StartSurvivalWave(puzzles1[0], (uint)WaveSettings.Trickle_2_40_ELEVATOR, (uint)WavePopulation.SHADOWS_GIANT_ONLY, SurvivalWaveSpawnType.FromElevatorDirection);
                    // colosseum 2 waves
                    Colosseum_WaveEventIDs[2] = StartSurvivalWave(puzzles2[0], (uint)WaveSettings.Trickle_8_30_SPAWNPOINT, (uint)WavePopulation.STRIKERS_ONLY, SurvivalWaveSpawnType.OnSpawnPoints);
                    if (!WaveSpawnedSuccessful(Colosseum_WaveEventIDs))
                    {
                        return;
                    }
                }
            }));

            // ----------------------------------------------------
            //      OnPuzzleSolved: puzzles1 Trio and puzzles2 Solo 
            // ----------------------------------------------------
            puzzles1[1].OnPuzzleSolved += new System.Action(() =>
            {
                if (!SNet.IsMaster)
                {
                    puzzles1[1].m_stateReplicator.SetStateUnsynced(new pChainedPuzzleState() { status = eChainedPuzzleStatus.Solved });
                }
                if (!puzzles2[0].IsSolved)
                {
                    Logger.Log("Puzzle1 Trio Solved: Puzzles2 Solo scan is not finished.");
                    Logger.Log("Duo scans will be activated after puzzles2 Solo scan completed.");
                }
                else
                {
                    ActivateDuoScans();
                }
            });
            puzzles2[0].OnPuzzleSolved += new System.Action(() =>
            {
                // Elevate fog. 
                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum2Events, eWardenObjectiveEventTrigger.OnStart, false);

                if (!SNet.IsMaster)
                {
                    puzzles2[0].m_stateReplicator.SetStateUnsynced(new pChainedPuzzleState() { status = eChainedPuzzleStatus.Solved });
                }

                if (!puzzles1[1].IsSolved)
                {
                    Logger.Log("Puzzle2 Solo Solved: puzzle1 Trio scan is not finished.");
                    Logger.Log("Duo scans will be activated after puzzles1 Trio scan completed.");
                }
                else
                {
                    // WARNING: potential fog elevation bug.
                    // Solution: added delay to 3rd, 4th fog descending events
                    ActivateDuoScans();
                }
            });

            // ----------------------------------------------------
            //      OnPuzzleSolved: puzzles1 Duo and puzzles2 Duo
            // ----------------------------------------------------
            puzzles1[2].OnPuzzleSolved += new System.Action(() =>
            {
                if (!SNet.IsMaster)
                {
                    puzzles1[2].m_stateReplicator.SetStateUnsynced(new pChainedPuzzleState() { status = eChainedPuzzleStatus.Solved });
                }

                if (!puzzles2[1].IsSolved)
                {
                    Logger.Log("Puzzle1 Duo Solved: Puzzles2 Duo scan is not finished.");
                    Logger.Log("Puzzle1 Solo and Puzzle2 Trio will be activated after Puzzles2 Duo scan completed.");
                }
                else
                {
                    ActivateSoloTrioScan();
                }
            });
            puzzles2[1].OnPuzzleSolved += new System.Action(() =>
            {
                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum2Events, eWardenObjectiveEventTrigger.OnMid, false);
                //Utils.CheckAndExecuteEventsOnTrigger(Colosseum2Events, eWardenObjectiveEventTrigger.OnMid, false);

                if (!SNet.IsMaster)
                {
                    puzzles2[1].m_stateReplicator.SetStateUnsynced(new pChainedPuzzleState() { status = eChainedPuzzleStatus.Solved });
                }

                if (!puzzles1[2].IsSolved)
                {
                    Logger.Log("Puzzle2 Duo Solved: Puzzles1 Duo scan is not finished.");
                    Logger.Log("Puzzle1 Solo and Puzzle2 Trio will be activated after Puzzles1 Duo scan completed.");
                }
                else
                {
                    ActivateSoloTrioScan();
                }
            });

            // ----------------------------------------------------
            //      OnPuzzleSolved: puzzles1 Solo and puzzles2 Trio
            // ----------------------------------------------------
            puzzles1[3].OnPuzzleSolved += new System.Action(() =>
            {
                if (!SNet.IsMaster)
                {
                    puzzles1[3].m_stateReplicator.SetStateUnsynced(new pChainedPuzzleState() { status = eChainedPuzzleStatus.Solved });
                }

                if (!puzzles2[2].IsSolved)
                {
                    Logger.Log("Puzzle1 Solo Solved: Puzzles2 Trio scan is not finished.");
                    Logger.Log("Puzzle2 Full Team will be activated after Puzzles2 Trio scan completed.");
                }
                else
                {
                    ActivateLastFullTeamScan();
                }
            });
            puzzles2[2].OnPuzzleSolved += new System.Action(() =>
            {
                if (!SNet.IsMaster)
                {
                    puzzles2[2].m_stateReplicator.SetStateUnsynced(new pChainedPuzzleState() { status = eChainedPuzzleStatus.Solved });
                }

                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum2Events, eWardenObjectiveEventTrigger.OnEnd, false);
                //Utils.CheckAndExecuteEventsOnTrigger(Colosseum2Events, eWardenObjectiveEventTrigger.OnEnd, false);

                if (!puzzles1[3].IsSolved)
                {
                    Logger.Log("Puzzle2 Trio Solved: Puzzles1 Solo scan is not finished.");
                    Logger.Log("Puzzle2 Full Team will be activated after Puzzles1 Solo scan completed.");
                }
                else
                {
                    ActivateLastFullTeamScan();
                }
            });

            // ----------------------------------------------------
            //      OnPuzzleSolved: puzzles2 full team config
            // ----------------------------------------------------
            puzzles2[3].OnPuzzleSolved += new System.Action(() =>
            {
                // colosseum 2 events. Currently there's no event.
                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum2Events, eWardenObjectiveEventTrigger.None, false);
                //Utils.CheckAndExecuteEventsOnTrigger(Colosseum2Events, eWardenObjectiveEventTrigger.None, false);

                if (SNet.IsMaster)
                {
                    if (Colosseum_WaveEventIDs == null)
                    {
                        Logger.Error("Wave Event ID not stored!");
                        return;
                    }

                    StopSpecifiedWaves(Colosseum_WaveEventIDs);
                    Colosseum_WaveEventIDs = null;
                }
            });

        }

        // ---------------------------
        // Custom SecDoor Interaction Text Setup
        // ---------------------------
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_SecurityDoor_Locks), nameof(LG_SecurityDoor_Locks.OnDoorState))]
        private static void Post_Customize_SecDoor_Interaction_Text(pDoorState state, LG_SecurityDoor_Locks __instance)
        {
            // additional evaluation if it's in L3E2
            if (RundownManager.ActiveExpedition.LevelLayoutData != MainLayerID) return;

            if (state.status != eDoorStatus.Unlocked && state.status != eDoorStatus.Closed_LockedWithChainedPuzzle) return;

            LG_SecurityDoor door = __instance.m_door;

            ExpeditionZoneData zoneData = door.LinkedToZoneData;

            if (zoneData == null)
            {
                Logger.Warning("door.LinkedToZoneData == null");
                return;
            }

            //Logger.Warning("LinkToZoneData.LocalIndex: {0}", zoneData.LocalIndex);

            if (CustomTextToPrefix == null)
            {
                CustomTextToPrefix = Text.Get(CustomTextPID);
            }

            if (zoneData.EventsOnBossDeath == null || zoneData.EventsOnBossDeath.Count <= 0) return;

            Interact_Timed intOpenDoor = __instance.m_intOpenDoor;
            intOpenDoor.InteractionMessage = CustomTextToPrefix + "\n" + intOpenDoor.InteractionMessage;
            //Logger.Warning("Added");
        }

        // puzzle starts event
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChainedPuzzleInstance), nameof(ChainedPuzzleInstance.AttemptInteract), new System.Type[] { typeof(eChainedPuzzleInteraction) })]
        private static void Post_ChainedPuzzleInstance_AttemptInteract(ChainedPuzzleInstance __instance, eChainedPuzzleInteraction interaction)
        {
            if (interaction != eChainedPuzzleInteraction.Activate) return;

            switch (__instance.Data.persistentID)
            {
                case (uint)PuzzleID.Colosseum1_1:
                    WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum1Events, eWardenObjectiveEventTrigger.None, false);

                    if (SNet.IsMaster)
                    {
                        Colosseum_WaveEventIDs = new ushort[3] { 0, 0, 0 };

                        Colosseum_WaveEventIDs[0] = StartSurvivalWave(__instance, (uint)WaveSettings.Trickle_12_20_ELEVATOR, (uint)WavePopulation.STRIKERS_ONLY, SurvivalWaveSpawnType.FromElevatorDirection);
                        Colosseum_WaveEventIDs[1] = StartSurvivalWave(__instance, (uint)WaveSettings.Trickle_6_40_ELEVATOR, (uint)WavePopulation.BULLRUSH_ONLY, SurvivalWaveSpawnType.FromElevatorDirection);
                        Colosseum_WaveEventIDs[2] = StartSurvivalWave(__instance, (uint)WaveSettings.Trickle_3_60_ELEVATOR, (uint)WavePopulation.HYBRID_ONLY, SurvivalWaveSpawnType.FromElevatorDirection);

                        if (!WaveSpawnedSuccessful(Colosseum_WaveEventIDs))
                        {
                            return;
                        }
                    }
                    break;
                default: return;
            }
        }

        private static ushort StartSurvivalWave(ChainedPuzzleInstance __instance, uint SurvivalWaveSettingsID, uint SurvivalWavePopulationID, SurvivalWaveSpawnType spawnType)
        {
            ushort eventID = 0;

            if (!SNet.IsMaster) return 0;

            if (Mastermind.Current.TriggerSurvivalWave(__instance.m_sourceArea.m_courseNode, SurvivalWaveSettingsID, SurvivalWavePopulationID, out eventID, spawnType) == false)
            {
                Logger.Error("Critical: Failed to spawn survival wave. Settings: {0}, Population: {1}", SurvivalWaveSettingsID, SurvivalWavePopulationID);
                return 0;
            }

            return eventID;
        }

        private static void StopSpecifiedWaves(ushort[] WaveEventIDs)
        {
            if (WaveEventIDs == null) return;
            foreach (ushort WaveEventID in WaveEventIDs)
            {
                Mastermind.MastermindEvent masterMindEvent_StopWave;
                if (Mastermind.Current.TryGetEvent(WaveEventID, out masterMindEvent_StopWave))
                {
                    masterMindEvent_StopWave.StopEvent();
                }
            }
        }

        private static void CleanupAfterExpedition()
        {
            if (!is_L3E2) return;
            Colosseum_WaveEventIDs = null;
            if (puzzles1 != null)
            {
                puzzles1.Clear();
                puzzles2.Clear();
            }
            is_L3E2 = false;
            puzzles1 = puzzles2 = null;
            Colosseum1Events = Colosseum2Events = null;
            DIMENSION_Z0_TERMINAL_CPS = null;
            PowerCells_InDimension = null;
            CustomTextToPrefix = null;

            Logger.Warning("L3E2 cleanup");
        }

        static Patch_HardcodedBehaviour_L3E2()
        {
            LevelAPI.OnBuildDone += OnBuildDone;
            LevelAPI.OnLevelCleanup += CleanupAfterExpedition;
        }
    }
}