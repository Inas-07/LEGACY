using Il2CppSystem.Collections.Generic;
using HarmonyLib;
using GameData;
using LEGACY.Utilities;
using SNetwork;
using ChainedPuzzles;
using LevelGeneration;
using Localization;
namespace LEGACY.Hardcoded_Behaviour
{
    [HarmonyPatch]
    internal class Patch_L3E2_HardcodedBehaviour
    {
        private static bool is_L3E2 = false;

        private static ushort[] Colosseum1_WaveEventIDs = null;
        private static ushort[] Colosseum2_WaveEventIDs = null;

        //private static ChainedPuzzleInstance puzzle1 = null, puzzle2 = null;

        private static System.Collections.Generic.List<ChainedPuzzleInstance> puzzles1 = null, puzzles2 = null;

        private static List<WardenObjectiveEventData> Colosseum1Events = null;
        private static List<WardenObjectiveEventData> Colosseum2Events = null;

        private static uint MainLayerID = 40000u;
        private static uint SecondaryLayoutID = 40001u;

        private static int DimensionWarpCount = 0;
        private static ChainedPuzzleInstance DIMENSION_Z0_CP = null;

        private static CarryItemPickup_Core[] PowerCells_InDimension = null;

        private static string CustomTextToPrefix = null;
        private static uint CustomTextPID = 3001u;

        enum PuzzleID
        {
            Colosseum1_1 = 40100,
            Colosseum1_2 = 40101,
            Colosseum1_3 = 40102,
            Colosseum1_4 = 40103,
            Colosseum2_1 = 40201,
            Colosseum2_2 = 40202,
            Colosseum2_3 = 40203,
            Colosseum2_4 = 40204
        }

        // survivial wave settings ID
        enum WaveSettings
        {
            Trickle_12_20_ELEVATOR = 152,
            Trickle_6_40_ELEVATOR = 153,
            Trickle_3_60_ELEVATOR = 154,
            Trickle_2_40_ELEVATOR = 155,
            Trickle_6_30_ELEVATOR = 156,
            FINITE_1_ELEVATOR = 157,
            Trickle_1_60_SPAWNPOINT = 158,
            Trickle_8_30_ELEVATOR = 159,
            Trickle_4_15_SPAWNPOINT = 160,
            Trickle_3_20_SPAWNPOINT = 161,
            Trickle_2_30_ELEVATOR = 162,
            Trickle_2_40_SPAWNPOINT = 163,
            Apex_Surge_SPAWNPOINT = 164,
            Trickle_8_30_SPAWNPOINT = 165,
            Trickle_6_30_SPAWNPOINT = 166,
            Trickle_2_30_SPAWNPOINT = 167,
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

            foreach (ushort id in WaveEventIDs)
            {
                if (id == 0) return false;
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Mastermind), nameof(Mastermind.OnBuilderDone))]
        private static void Post_MastermindOnBuilderDone(Mastermind __instance)
        {
            is_L3E2 = RundownManager.ActiveExpedition.LevelLayoutData == MainLayerID;

            if (!is_L3E2) return;

            Logger.Log("In Legacy L3-E2, Setting up custom level behaviour!");

            // ---------------------------
            //   Colosseum Events Setup
            // ---------------------------
            LevelLayoutDataBlock db = LevelLayoutDataBlock.GetBlock(SecondaryLayoutID);

            List<CustomTerminalCommand> unusedCommands = db.Zones[8].TerminalPlacements[0].UniqueCommands;

            Colosseum1Events = unusedCommands[0].CommandEvents;
            Colosseum2Events = unusedCommands[1].CommandEvents;

            // ---------------------------
            //   Dimension Warp Required Item setup
            // ---------------------------
            DimensionWarpCount = 0;

            LG_Zone dim_Z12 = null;

            if (Builder.CurrentFloor.TryGetZoneByLocalIndex(eDimensionIndex.Dimension_1, LG_LayerType.MainLayer, eLocalZoneIndex.Zone_0, out dim_Z12) == false || dim_Z12 == null || dim_Z12.TerminalsSpawnedInZone.Count <= 0)
            {
                Logger.Warning("Cound not get dimension z12 or the terminal in zone! Omitted Item Requirement for the scan");
                return;
            }

            if (dim_Z12.TerminalsSpawnedInZone.Count != 1)
            {
                Logger.Warning("Multiple terminal found in dim_z12, which is not as designed. Omitted Item Requirement for the scan");
                return;
            }

            DIMENSION_Z0_CP = Utils.GetChainedPuzzleForCommandOnTerminal(dim_Z12.TerminalsSpawnedInZone[0], "RESTORE_MATTER_WAVE");
            if (DIMENSION_Z0_CP == null)
            {
                Logger.Error("Could not get chained puzzle for command RESTORE_MATTER_WAVE on terminal in dim_z12! Omitted Item Requirement for the scan");
                return;
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

            DIMENSION_Z0_CP.AddRequiredItems(new iWardenObjectiveItem[1] { new iWardenObjectiveItem(PowerCells_InDimension[0].Pointer) });
            DIMENSION_Z0_CP.OnPuzzleSolved += new System.Action(() => {
                DimensionWarpCount++;
                if (DimensionWarpCount < PowerCells_InDimension.Length)
                {
                    ChainedPuzzleInstance CP = Utils.GetChainedPuzzleForCommandOnTerminal(dim_Z12.TerminalsSpawnedInZone[0], "RESTORE_MATTER_WAVE");
                    CP.AddRequiredItems(new iWardenObjectiveItem[1] { new iWardenObjectiveItem(PowerCells_InDimension[DimensionWarpCount].Pointer) });
                }
            });

            // setting up colosseum (again :) 
            TerminalPlacementData term_placement1 = db.Zones[3].TerminalPlacements[0];
            TerminalPlacementData term_placement2 = db.Zones[9].TerminalPlacements[0];

            List<WardenObjectiveEventData> CommandEvent_1 = term_placement1.UniqueCommands[0].CommandEvents;
            List<WardenObjectiveEventData> CommandEvent_2 = term_placement2.UniqueCommands[0].CommandEvents;

            LG_Zone zone123 = null, zone129 = null;
            Builder.CurrentFloor.TryGetZoneByLocalIndex(eDimensionIndex.Reality, LG_LayerType.SecondaryLayer, eLocalZoneIndex.Zone_3, out zone123);
            Builder.CurrentFloor.TryGetZoneByLocalIndex(eDimensionIndex.Reality, LG_LayerType.SecondaryLayer, eLocalZoneIndex.Zone_9, out zone129);

            if (zone123 == null || zone129 == null)
            {
                Logger.Error("Builder failed to get the 2 zones");
                return;
            }

            LG_ComputerTerminal terminal1 = zone123.TerminalsSpawnedInZone[0], terminal2 = zone129.TerminalsSpawnedInZone[0];
            TERM_Command CMD1 = TERM_Command.None, CMD2 = TERM_Command.None;
            string param1, param2;
            terminal1.m_command.TryGetCommand(term_placement1.UniqueCommands[0].Command, out CMD1, out param1, out param2);
            terminal2.m_command.TryGetCommand(term_placement2.UniqueCommands[0].Command, out CMD2, out param1, out param2);

            if (CMD1 == TERM_Command.None || CMD2 == TERM_Command.None)
            {
                Logger.Error("Failed to get TERM_Command");
                return;
            }

            ChainedPuzzleInstance CP_1 = null, CP_2 = null;
            for (int eventIndex = 0; eventIndex < CommandEvent_1.Count; eventIndex++)
            {
                terminal1.TryGetChainPuzzleForCommand(CMD1, eventIndex, out CP_1);
                terminal2.TryGetChainPuzzleForCommand(CMD2, eventIndex, out CP_2);

                if (CP_1 != null && CP_2 != null) break;
            }

            if (CP_1 == null || CP_2 == null)
            {
                Logger.Error("Failed to get colosseum chained puzzle instance.");
                return;
            }

            puzzles1 = new System.Collections.Generic.List<ChainedPuzzleInstance>();
            puzzles2 = new System.Collections.Generic.List<ChainedPuzzleInstance>();

            puzzles1.Add(CP_1);
            puzzles2.Add(CP_2);

            puzzles1[0].OnPuzzleSolved += new System.Action(() => {
                if (SNet.IsMaster)
                {
                    if (Colosseum1_WaveEventIDs == null)
                    {
                        Logger.Error("Wave Event ID not stored!");
                        return;
                    }
                    StopSpecifiedWaves(Colosseum1_WaveEventIDs);

                    Colosseum1_WaveEventIDs = new ushort[2] { 0, 0 };
                    Colosseum1_WaveEventIDs[0] = StartSurvivalWave(puzzles1, (uint)WaveSettings.Trickle_12_20_ELEVATOR, (uint)WavePopulation.SHADOWS_ONLY, SurvivalWaveSpawnType.FromElevatorDirection);
                    Colosseum1_WaveEventIDs[1] = StartSurvivalWave(puzzles1, (uint)WaveSettings.Trickle_2_40_ELEVATOR, (uint)WavePopulation.SHADOWS_GIANT_ONLY, SurvivalWaveSpawnType.FromElevatorDirection);

                    if (!WaveSpawnedSuccessful(Colosseum1_WaveEventIDs))
                    {
                        Logger.Error("Critical: Failed to spawn survival wave! PuzzleIndex: {2}, ID1: {0}, ID2: {1}", Colosseum1_WaveEventIDs[0], Colosseum1_WaveEventIDs[1], index);
                        return;
                    }

                    Logger.Log("Oops! Spawning waves for 2nd puzzle!");
                }

                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum1Events, eWardenObjectiveEventTrigger.OnStart, false);
                Logger.Log("Oops! Executing events for 2nd puzzle!");
            });

            // TODO: try to divide a scans of 4 puzzles into 4 separated puzzle instance.
            /*
             * Handling colosseum 1
            */
            iChainedPuzzleCore p1_1 = CP_1.GetPuzzle(0);
            iChainedPuzzleCore p1_2 = CP_1.GetPuzzle(1);
            iChainedPuzzleCore p1_3 = CP_1.GetPuzzle(2);
            iChainedPuzzleCore p1_4 = CP_1.GetPuzzle(3);

            p1_1.add_OnPuzzleDone(new System.Action<int>((index) => {
                if (SNet.IsMaster)
                {
                    if (Colosseum1_WaveEventIDs == null)
                    {
                        Logger.Error("Wave Event ID not stored!");
                        return;
                    }
                    StopSpecifiedWaves(Colosseum1_WaveEventIDs);

                    Colosseum1_WaveEventIDs = new ushort[2] { 0, 0 };
                    Colosseum1_WaveEventIDs[0] = StartSurvivalWave(puzzles1, (uint)WaveSettings.Trickle_12_20_ELEVATOR, (uint)WavePopulation.SHADOWS_ONLY, SurvivalWaveSpawnType.FromElevatorDirection);
                    Colosseum1_WaveEventIDs[1] = StartSurvivalWave(puzzles1, (uint)WaveSettings.Trickle_2_40_ELEVATOR, (uint)WavePopulation.SHADOWS_GIANT_ONLY, SurvivalWaveSpawnType.FromElevatorDirection);

                    if (!WaveSpawnedSuccessful(Colosseum1_WaveEventIDs))
                    {
                        Logger.Error("Critical: Failed to spawn survival wave! PuzzleIndex: {2}, ID1: {0}, ID2: {1}", Colosseum1_WaveEventIDs[0], Colosseum1_WaveEventIDs[1], index);
                        return;
                    }

                    Logger.Log("Oops! Spawning waves for 2nd puzzle!");
                }

                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum1Events, eWardenObjectiveEventTrigger.OnStart, false);
                Logger.Log("Oops! Executing events for 2nd puzzle!");
            }));
            p1_2.add_OnPuzzleDone(new System.Action<int>((index) => {
                if (SNet.IsMaster)
                {
                    if (Colosseum1_WaveEventIDs == null)
                    {
                        Logger.Error("Wave Event ID not stored!");
                        return;
                    }
                    StopSpecifiedWaves(Colosseum1_WaveEventIDs);

                    Colosseum1_WaveEventIDs = new ushort[2] { 0, 0 };
                    Colosseum1_WaveEventIDs[0] = StartSurvivalWave(puzzles1, (uint)WaveSettings.Trickle_6_30_ELEVATOR, (uint)WavePopulation.BULLRUSH_ONLY, SurvivalWaveSpawnType.FromElevatorDirection);
                    Colosseum1_WaveEventIDs[1] = StartSurvivalWave(puzzles1, (uint)WaveSettings.FINITE_1_ELEVATOR, (uint)WavePopulation.TANK, SurvivalWaveSpawnType.FromElevatorDirection);
                    //Colosseum1_WaveEventIDs[2] = StartSurvivalWave(puzzle1, (uint)WaveSettings.Trickle_1_60_SPAWNPOINT, (uint)WavePopulation.FLYERS_BIG, SurvivalWaveSpawnType.OnSpawnPoints);

                    if (!WaveSpawnedSuccessful(Colosseum1_WaveEventIDs))
                    {
                        Logger.Error("Critical: Failed to spawn survival wave! PuzzleIndex: {3}, ID1: {0}, ID2: {1}", Colosseum1_WaveEventIDs[0], Colosseum1_WaveEventIDs[1], index);
                        return;
                    }

                    Logger.Log("Oops! Spawning waves for 3rd puzzle!");
                }

                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum1Events, eWardenObjectiveEventTrigger.OnMid, false);
                Logger.Log("Oops! Executing events for 3rd puzzle!");
            }));
            p1_3.add_OnPuzzleDone(new System.Action<int>((index) => {
                if (SNet.IsMaster)
                {
                    if (Colosseum1_WaveEventIDs == null)
                    {
                        Logger.Error("Wave Event ID not stored!");
                        return;
                    }
                    StopSpecifiedWaves(Colosseum1_WaveEventIDs);

                    Colosseum1_WaveEventIDs = new ushort[1] { 0 };
                    Colosseum1_WaveEventIDs[0] = StartSurvivalWave(puzzles1, (uint)WaveSettings.Trickle_8_30_ELEVATOR, (uint)WavePopulation.BASELINE, SurvivalWaveSpawnType.FromElevatorDirection);

                    if (!WaveSpawnedSuccessful(Colosseum1_WaveEventIDs))
                    {
                        Logger.Error("Critical: Failed to spawn survival wave! PuzzleIndex: {1}, ID1: {0}", Colosseum1_WaveEventIDs[0], index);
                        return;
                    }

                    Logger.Log("Oops! Spawning waves for 4th puzzle!");
                }

                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum1Events, eWardenObjectiveEventTrigger.OnEnd, false);
                Logger.Log("Oops! Executing events for 4th puzzle!");
            }));
            p1_4.add_OnPuzzleDone(new System.Action<int>((index) => {
                if (SNet.IsMaster)
                {
                    if (Colosseum1_WaveEventIDs == null)
                    {
                        Logger.Error("Wave Event ID not stored!");
                        return;
                    }
                    StopSpecifiedWaves(Colosseum1_WaveEventIDs);
                    Colosseum1_WaveEventIDs = null;
                    Logger.Log("Colosseum 1 ended successfully!");
                }
            }));

            /*
             * Handling colosseum 2
            */
            iChainedPuzzleCore p2_1 = CP_2.GetPuzzle(0);
            iChainedPuzzleCore p2_2 = CP_2.GetPuzzle(1);
            iChainedPuzzleCore p2_3 = CP_2.GetPuzzle(2);
            iChainedPuzzleCore p2_4 = CP_2.GetPuzzle(3);
            p2_1.add_OnPuzzleDone(new System.Action<int>((index) => {
                if (SNet.IsMaster)
                {
                    if (Colosseum2_WaveEventIDs == null)
                    {
                        Logger.Error("Wave Event ID not stored!");
                        return;
                    }
                    StopSpecifiedWaves(Colosseum2_WaveEventIDs);

                    // doesn't new another ushort array.
                    Colosseum2_WaveEventIDs[0] = 0;

                    Colosseum2_WaveEventIDs[0] = StartSurvivalWave(puzzles2, (uint)WaveSettings.Trickle_6_30_SPAWNPOINT, (uint)WavePopulation.BULLRUSH_ONLY, SurvivalWaveSpawnType.OnSpawnPoints);
                    if(!WaveSpawnedSuccessful(Colosseum2_WaveEventIDs))
                    {
                        Logger.Error("Critical: Failed to spawn survival wave! PuzzleIndex: {4}, ID1: {0}", Colosseum2_WaveEventIDs[0], index);
                        return;
                    }
                    Logger.Log("Oops! Spawning waves for 2ND puzzle!");
                }

                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum2Events, eWardenObjectiveEventTrigger.OnStart, false);
                Logger.Log("Oops! Executing events for 2nd puzzle!");
            }));
            p2_2.add_OnPuzzleDone(new System.Action<int>((index) => {
                if (SNet.IsMaster)
                {
                    if(Colosseum2_WaveEventIDs == null)
                    {
                        Logger.Error("Wave Event ID not stored!");
                        return;
                    }
                    StopSpecifiedWaves(Colosseum2_WaveEventIDs);

                    Colosseum2_WaveEventIDs = new ushort[2] { 0, 0 };
                    Colosseum2_WaveEventIDs[0] = StartSurvivalWave(puzzles2, (uint)WaveSettings.Trickle_2_30_SPAWNPOINT, (uint)WavePopulation.HYBRID_ONLY, SurvivalWaveSpawnType.OnSpawnPoints);
                    Colosseum2_WaveEventIDs[1] = StartSurvivalWave(puzzles2, (uint)WaveSettings.Trickle_2_30_SPAWNPOINT, (uint)WavePopulation.BULLRUSH_BOSS_ONLY, SurvivalWaveSpawnType.OnSpawnPoints);
                    
                    if (!WaveSpawnedSuccessful(Colosseum2_WaveEventIDs))
                    {
                        Logger.Error("Critical: Failed to spawn survival wave! PuzzleIndex: {4}, ID1: {0}, ID2: {1}, ID3: {2}, ID4: {3}", Colosseum2_WaveEventIDs[0], Colosseum2_WaveEventIDs[1], Colosseum2_WaveEventIDs[2], Colosseum2_WaveEventIDs[3], index);
                        return;
                    }

                    Logger.Log("Oops! Spawning waves for 3rd puzzle!");
                }

                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum2Events, eWardenObjectiveEventTrigger.OnMid, false);
                Logger.Log("Oops! Executing events for 3rd puzzle!");
            }));
            p2_3.add_OnPuzzleDone(new System.Action<int>((index) =>
            {
                if (SNet.IsMaster)
                {
                    if (Colosseum2_WaveEventIDs == null)
                    {
                        Logger.Error("Wave Event ID not stored!");
                        return;
                    }
                    StopSpecifiedWaves(Colosseum2_WaveEventIDs);

                    Colosseum2_WaveEventIDs = new ushort[1] { 0 };
                    Colosseum2_WaveEventIDs[0] = StartSurvivalWave(puzzles2, (uint)WaveSettings.Apex_Surge_SPAWNPOINT, (uint)WavePopulation.BASELINE, SurvivalWaveSpawnType.OnSpawnPoints);

                    if (!WaveSpawnedSuccessful(Colosseum2_WaveEventIDs))
                    {
                        Logger.Error("Critical: Failed to spawn survival wave! PuzzleIndex: {1}, ID1: {0}", Colosseum2_WaveEventIDs[0], index);
                        return;
                    }

                    Logger.Log("Oops! Spawning waves for 4th puzzle!");
                }

                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum2Events, eWardenObjectiveEventTrigger.OnEnd, false);
                Logger.Log("Oops! Executing events for 4th puzzle!");
            }));
            p2_4.add_OnPuzzleDone(new System.Action<int>((index) => {
                if (SNet.IsMaster)
                {
                    if (Colosseum2_WaveEventIDs == null)
                    {
                        Logger.Error("Wave Event ID not stored!");
                        return;
                    }
                    StopSpecifiedWaves(Colosseum2_WaveEventIDs);
                    Colosseum2_WaveEventIDs = null;
                    Logger.Log("Colosseum 2 ended successfully!");
                }
            }));
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
            
            if(zoneData == null)
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
        [HarmonyPatch(typeof(ChainedPuzzleInstance), nameof(ChainedPuzzleInstance.AttemptInteract), new System.Type[] {typeof(eChainedPuzzleInteraction)})]
        private static void Post_ChainedPuzzleInstance_AttemptInteract(ChainedPuzzleInstance __instance, eChainedPuzzleInteraction interaction)
        {
            if (!eChainedPuzzleInteraction.Equals(interaction, eChainedPuzzleInteraction.Activate)) return;

            switch (__instance.Data.persistentID)
            {
                case (uint)PuzzleID.Colosseum1_1:
                    puzzles1 = __instance;

                    if (SNet.IsMaster)
                    {
                        Colosseum1_WaveEventIDs = new ushort[3] { 0, 0, 0 };

                        Colosseum1_WaveEventIDs[0] = StartSurvivalWave(__instance, (uint)WaveSettings.Trickle_12_20_ELEVATOR, (uint)WavePopulation.STRIKERS_ONLY, SurvivalWaveSpawnType.FromElevatorDirection);
                        Colosseum1_WaveEventIDs[1] = StartSurvivalWave(__instance, (uint)WaveSettings.Trickle_6_40_ELEVATOR, (uint)WavePopulation.BULLRUSH_ONLY, SurvivalWaveSpawnType.FromElevatorDirection);
                        Colosseum1_WaveEventIDs[2] = StartSurvivalWave(__instance, (uint)WaveSettings.Trickle_3_60_ELEVATOR, (uint)WavePopulation.HYBRID_ONLY, SurvivalWaveSpawnType.FromElevatorDirection);

                        if (!WaveSpawnedSuccessful(Colosseum1_WaveEventIDs))
                        {
                            Logger.Error("Critical: Failed to spawn survival wave! PuzzleIndex: 0, ID1: {0}, ID2: {1}, ID3: {2}", Colosseum1_WaveEventIDs[0], Colosseum1_WaveEventIDs[1], Colosseum1_WaveEventIDs[2]);
                            return;
                        }

                        Logger.Log("Oops! Spawning waves for 1st puzzle!");
                    }

                    WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum1Events, eWardenObjectiveEventTrigger.None, false);
                    Logger.Log("Oops! Executing events for 1st puzzle!");
                    break;
                case (uint)PuzzleID.Colosseum2: 
                    puzzles2 = __instance;
                    if (SNet.IsMaster)
                    {
                        Colosseum2_WaveEventIDs = new ushort[1] { 0 };

                        Colosseum2_WaveEventIDs[0] = StartSurvivalWave(puzzles2, (uint)WaveSettings.Trickle_8_30_SPAWNPOINT, (uint)WavePopulation.STRIKERS_ONLY, SurvivalWaveSpawnType.OnSpawnPoints);
                        
                        if (!WaveSpawnedSuccessful(Colosseum2_WaveEventIDs))
                        {
                            Logger.Error("Critical: Failed to spawn survival wave! PuzzleIndex: 0, ID1: {0}", Colosseum1_WaveEventIDs[0]);
                            return;
                        }
                        
                        Logger.Log("Oops! Spawning waves for 1st puzzle!");
                    }

                    WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(Colosseum2Events, eWardenObjectiveEventTrigger.None, false);
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GS_AfterLevel), nameof(GS_AfterLevel.CleanupAfterExpedition))]
        private static void Post_CleanupAfterExpedition()
        {
            Colosseum1_WaveEventIDs = Colosseum2_WaveEventIDs = null;
            puzzles1 = puzzles2 = null;
            Colosseum1Events = Colosseum2Events = null;
            DIMENSION_Z0_CP = null;
            PowerCells_InDimension = null;
            CustomTextToPrefix = null;
        }
    }
}