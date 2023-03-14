using HarmonyLib;
using Enemies;
using LevelGeneration;
using GameData;
using System.Collections;
using LEGACY.Utils;
using Player;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using SNetwork;
using AK;
using Agents;
using AIGraph;

namespace LEGACY.ExtraEventsConfig
{
    enum EventType
    {
        CloseSecurityDoor_Custom = 100,
        KillEnemiesInDimension_Custom,
        SetTimerTitle_Custom,
        ToggleEnableDisableAllTerminalsInZone_Custom,
        ToggleEnableDisableTerminalInZone_Custom,
        KillEnemiesInZone_Custom,
        StopSpecifiedEnemyWave,
        AlertEnemiesInZone,
        AlertEnemiesInArea,

        Reactor_CompleteCurrentWave = 150,
        TP_WarpTeamsToArea = 160,

        SpawnEnemy_Hibernate = 170,

        ChainedPuzzle_AddReqItem = 200,
        ChainedPuzzle_RemoveReqItem 
    }

    [HarmonyPatch]
    class Patch_ExtraEventsConfig
    {
        private static void SpawnEnemy_Hibernate(WardenObjectiveEventData e)
        {
            if (!SNet.IsMaster) return;

            AgentMode mode = AgentMode.Hibernate; 

            switch(e.EnemyID)
            {
                case 20:
                case 40:
                case 41:
                    mode = AgentMode.Scout; break;
            }

            LG_Zone zone;
            AIG_CourseNode node = null;
            bool usingOnPosition = false;
            if (!Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out zone) || zone == null)
            {
                Logger.Error($"SpawnEnemy_Hibernate: cannot find zone {e.LocalIndex}, {e.Layer}, {e.DimensionIndex}");
                return;
            }

            System.Collections.Generic.List<UnityEngine.Vector3> spawnPositions = new();
            if (e.Position != UnityEngine.Vector3.zero && e.Count == 1)
            {
                usingOnPosition = true;
                spawnPositions.Add(e.Position);
            }
            else
            {
                switch (e.WorldEventObjectFilter.ToUpperInvariant())
                {
                    case "RANDOM":
                    case "RANDOM_AREA":
                    case "":
                        node = null;
                        break;
                    default:
                        if (e.WorldEventObjectFilter.Contains("AREA_") && "AREA_".Length + 1 == e.WorldEventObjectFilter.Length)
                        {
                            int areaIndex = e.WorldEventObjectFilter["AREA_".Length] - 'A';
                            if (areaIndex < 0 || areaIndex >= zone.m_areas.Count)
                            {
                                Logger.Error($"SpawnEnemy_Hibernate: invalid WorldEventObjectFilter - didn't find {e.WorldEventObjectFilter}");
                                return;
                            }

                            node = zone.m_areas[areaIndex].m_courseNode;
                        }
                        else
                        {
                            Logger.Error("SpawnEnemy_Hibernate: invalid format for WorldEventObjectFilter, should be one of RANDOM, \"\", AREA_{area letter}");
                            return;
                        }
                        break;
                }

                if(node == null)
                {
                    int areaIndex = Builder.SessionSeedRandom.Range(0, zone.m_areas.Count); 
                    Logger.Debug($"areaIndex: {areaIndex}");

                    // a single enemy group falls into one single zone.
                    node = zone.m_areas[areaIndex].m_courseNode;
                }

                for (int c = 0; c < e.Count; c++)
                {
                    spawnPositions.Add(node.GetRandomPositionInside());
                }
            }

            // scout:
            if (mode == AgentMode.Scout)
            {
                eEnemyGroupType groupType = (eEnemyGroupType)3;
                eEnemyRoleDifficulty difficulty = 0;

                switch (e.EnemyID)
                {
                    case 20:
                        difficulty = 0; break;
                    case 40:
                        difficulty = (eEnemyRoleDifficulty)14; break;
                    case 41:
                        difficulty = (eEnemyRoleDifficulty)3; break;
                    case 23:
                        difficulty = (eEnemyRoleDifficulty)6; break;
                    case 48:
                        difficulty = (eEnemyRoleDifficulty)13; break;
                    default:
                        Logger.Error($"Undefined scout, enemy ID {e.EnemyID}");
                        break;
                }

                EnemyGroupRandomizer r = null;
                if (!EnemySpawnManager.TryCreateEnemyGroupRandomizer(groupType, difficulty, out r) || r == null)
                {
                    Logger.Error("EnemySpawnManager.TryCreateEnemyGroupRandomizer false");
                    return;
                }

                EnemyGroupDataBlock randomGroup = r.GetRandomGroup(Builder.SessionSeedRandom.Value());
                float popPoints = randomGroup.MaxScore * Builder.SessionSeedRandom.Range(1f, 1.2f);

                foreach (var position in spawnPositions)
                {
                    var scoutSpawnData = EnemyGroup.GetSpawnData(position, node, EnemyGroupType.Hibernating,
                    eEnemyGroupSpawnType.RandomInArea, randomGroup.persistentID, popPoints) with
                    {
                        respawn = false
                    };

                    EnemyGroup.Spawn(scoutSpawnData);
                }
            }

            else
            {
                foreach (var position in spawnPositions)
                {
                    UnityEngine.Quaternion rotation = UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(EnemyGroup.s_randomRot2D.x, 0.0f, EnemyGroup.s_randomRot2D.y), UnityEngine.Vector3.up);
                    EnemyAllocator.Current.SpawnEnemy(e.EnemyID, node, mode, position, rotation);
                }
            }

            Logger.Debug($"SpawnEnemy_Hibernate: spawned {e.Count} enemy/enemies " + (usingOnPosition ? $"on position ({e.Position.x}, {e.Position.y}, {e.Position.z})" : $"in zone {e.LocalIndex}, {e.Layer}, {e.DimensionIndex}"));
        }

        // specifying e.DimensionIndex is necessary!
        private static void WarpTeamsToArea(WardenObjectiveEventData e)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

            // with area unspecified, warp players to random area in zone
            PlayerAgent localPlayer = PlayerManager.GetLocalPlayerAgent();

            if (localPlayer == null)
            {
                Logger.Error("WarpTeamsToArea: Cannot get local player agent!");
                return;
            }

            eDimensionIndex flashFromDimensionIndex = localPlayer.DimensionIndex;
            Dimension flashToDimension;
            if (Dimension.GetDimension(e.DimensionIndex, out flashToDimension) == false || flashToDimension == null)
            {
                Logger.Error("WarpTeamsToArea: Cannot find dimension to warp to!");
                return;
            }

            LG_Zone warpToZone;
            if (!Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out warpToZone) || warpToZone == null)
            {
                Logger.Error($"WarpTeamsToArea: Cannot find target zone! {e.LocalIndex}, {e.Layer}, {e.DimensionIndex}");
                return;
            }

            int areaIndex = e.Count;
            if (areaIndex < 0 || areaIndex >= warpToZone.m_areas.Count)
            {
                Logger.Warning($"WarpTeamsToArea: invalid area index {areaIndex}, defaulting to first area");
                areaIndex = 0;
            }

            LG_Area warpToArea = warpToZone.m_areas[areaIndex];

            UnityEngine.Vector3 warpToPosition = warpToArea.m_courseNode.GetRandomPositionInside();

            localPlayer.TryWarpTo(e.DimensionIndex, warpToPosition, UnityEngine.Random.onUnitSphere, true);
            Logger.Debug($"WarpTeamsToArea: warpped to {e.LocalIndex}{'A' + areaIndex}, {e.Layer}, {e.DimensionIndex}");
        }

        internal static void CompleteCurrentReactorWave(WardenObjectiveEventData e)
        {
            if (!SNet.IsMaster) return;

            WardenObjectiveDataBlock data;
            if (!WardenObjectiveManager.Current.TryGetActiveWardenObjectiveData(e.Layer, out data) || data == null)
            {
                Logger.Error("CompleteCurrentReactorWave: Cannot get WardenObjectiveDataBlock");
                return;
            }
            if (data.Type != eWardenObjectiveType.Reactor_Startup)
            {
                Logger.Error($"CompleteCurrentReactorWave: {e.Layer} is not ReactorStartup. CompleteCurrentReactorWave is invalid.");
                return;
            }

            LG_WardenObjective_Reactor reactor = Helper.FindReactor(e.Layer);

            if (reactor == null)
            {
                Logger.Error($"CompleteCurrentReactorWave: Cannot find reactor in {e.Layer}.");
                return;
            }

            switch (reactor.m_currentState.status)
            {
                case eReactorStatus.Inactive_Idle:
                    reactor.AttemptInteract(eReactorInteraction.Initiate_startup);
                    reactor.m_terminal.TrySyncSetCommandHidden(TERM_Command.ReactorStartup);
                    break;
                case eReactorStatus.Startup_complete:
                    Logger.Error($"CompleteCurrentReactorWave: Startup already completed for {e.Layer} reactor");
                    break;
                case eReactorStatus.Active_Idle:
                case eReactorStatus.Startup_intro:
                case eReactorStatus.Startup_intense:
                case eReactorStatus.Startup_waitForVerify:
                    if (reactor.m_currentWaveCount == reactor.m_waveCountMax)
                        reactor.AttemptInteract(eReactorInteraction.Finish_startup);
                    else
                        reactor.AttemptInteract(eReactorInteraction.Verify_startup);
                    break;
            }

            Logger.Debug($"CompleteCurrentReactorWave: Current reactor wave for {e.Layer} completed");
        }

        private static void AlertEnemies(WardenObjectiveEventData e, bool AlertAllAreas = false)
        {
            LG_Zone zone = null;
            if (Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out zone) == false)
            {
                Logger.Error("AlertEnemies: Zone is Missing?");
                return;
            }

            if (e.Count >= zone.m_areas.Count)
            {
                Logger.Error("event.Count >= zone.areas.Count! Falling back to AlertAllAreas!");
                AlertAllAreas = true;
            }

            if (AlertAllAreas)
            {
                foreach (var node in zone.m_courseNodes)
                {
                    if (node.m_enemiesInNode.Count <= 0) continue;

                    UnityEngine.Vector3 position = node.m_enemiesInNode[0].Position;
                    NoiseManager.MakeNoise(new NM_NoiseData
                    {
                        noiseMaker = null,
                        position = position,
                        radiusMin = 50f,
                        radiusMax = 120f,
                        yScale = 1f,
                        node = node,
                        type = NM_NoiseType.InstaDetect,
                        includeToNeightbourAreas = true,
                        raycastFirstNode = false
                    });
                }
            }
            else
            {
                var node = zone.m_courseNodes[e.Count];
                if (node.m_enemiesInNode.Count <= 0) return;

                UnityEngine.Vector3 position = node.m_enemiesInNode[0].Position;
                NoiseManager.MakeNoise(new NM_NoiseData
                {
                    noiseMaker = null,
                    position = position,
                    radiusMin = 50f,
                    radiusMax = 120f,
                    yScale = 1f,
                    node = node,
                    type = NM_NoiseType.InstaDetect,
                    includeToNeightbourAreas = true,
                    raycastFirstNode = false
                });
            }
        }

        private static void SetTerminalCommand_Custom(WardenObjectiveEventData eventToTrigger)
        {
            LG_LayerType layer = eventToTrigger.Layer;
            eLocalZoneIndex localIndex = eventToTrigger.LocalIndex;
            eDimensionIndex dimensionIndex = eventToTrigger.DimensionIndex;
            LG_ComputerTerminal terminal;

            switch (eventToTrigger.TerminalCommand)
            {
                case TERM_Command.ReactorStartup:
                case TERM_Command.ReactorShutdown:
                case TERM_Command.ReactorVerify:
                    LG_WardenObjective_Reactor reactor = Helper.FindReactor(layer);
                    if (reactor == null)
                    {
                        Logger.Error($"SetTerminalCommand_Custom: Cannot find reactor for {layer}, won't change {eventToTrigger.TerminalCommand} visibility");
                        return;
                    }

                    terminal = reactor.m_terminal;
                    break;

                    // todo: add more warden objective command manipulation.

                default:
                    LG_Zone terminalZone = null;

                    Builder.CurrentFloor.TryGetZoneByLocalIndex(dimensionIndex, layer, localIndex, out terminalZone);
                    if (terminalZone == null)
                    {
                        Logger.Error("SetTerminalCommand_Custom: Failed to get terminal in zone {0}, layer {1}, dimension {2}.", localIndex, layer, dimensionIndex);
                        return;
                    }

                    if (terminalZone.TerminalsSpawnedInZone == null)
                    {
                        Logger.Error("SetTerminalCommand_Custom: terminalZone.TerminalsSpawnedInZone == null");
                        return;
                    }

                    if (terminalZone.TerminalsSpawnedInZone.Count < 1)
                    {
                        Logger.Error("SetTerminalCommand_Custom: No terminal spawns in the specified zone!");
                        return;
                    }

                    if (eventToTrigger.Count >= terminalZone.TerminalsSpawnedInZone.Count)
                    {
                        Logger.Error("SetTerminalCommand_Custom: Invalid event.Count: 0 <= event.Count < TerminalsSpawnedInZone.Count should suffice.");
                        return;
                    }

                    terminal = terminalZone.TerminalsSpawnedInZone[eventToTrigger.Count];
                    break;
            }

            if(terminal == null)
            {
                Logger.Error("SetTerminalCommand_Custom: null temrinal");
                return;
            }

            if (eventToTrigger.Enabled == true)
            {
                terminal.TrySyncSetCommandShow(eventToTrigger.TerminalCommand);
            }
            else
            {
                terminal.TrySyncSetCommandHidden(eventToTrigger.TerminalCommand);
            }

            Logger.Debug($"SetTerminalCommand_Custom: Terminal_{terminal.m_serialNumber}, Zone_{terminal.SpawnNode.m_zone.LocalIndex}, {terminal.SpawnNode.LayerType}, {terminal.SpawnNode.m_dimension.DimensionIndex}");
        }

        private static bool CloseSecurityDoor_Custom(WardenObjectiveEventData eventToTrigger)
        {
            LG_Zone zone = null;
            if (Builder.CurrentFloor.TryGetZoneByLocalIndex(eventToTrigger.DimensionIndex, eventToTrigger.Layer, eventToTrigger.LocalIndex, out zone) == false || zone == null)
            {
                Logger.Error("CloseSecurityDoor_Custom: Failed to get zone {0}, layer {1}, dimensionIndex {2}", eventToTrigger.LocalIndex, eventToTrigger.Layer, eventToTrigger.DimensionIndex);
                return false;
            }

            LG_SecurityDoor door = null;
            if (Utils.Helper.TryGetZoneEntranceSecDoor(zone, out door) == false || door == null)
            {
                Logger.Error("CloseSecurityDoor_Custom: failed to get LG_SecurityDoor!");
                return false;
            }

            pDoorState currentSyncState1 = door.m_sync.GetCurrentSyncState();
            if (currentSyncState1.status != eDoorStatus.Open && currentSyncState1.status != eDoorStatus.Opening)
                return false;
            Logger.Debug("Door Closed!");
            LG_Door_Sync lgDoorSync = door.m_sync.TryCast<LG_Door_Sync>();

            if (lgDoorSync == null) return false;

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

            return true;
        }

        private static void KillEnemiesInZone(LG_Zone zone)
        {
            if (zone == null) return;

            for (int i = 0; i < zone.m_courseNodes.Count; ++i)
            {
                EnemyAgent[] array = zone.m_courseNodes[i].m_enemiesInNode.ToArray();
                for (int j = 0; j < array.Length; ++j)
                {
                    EnemyAgent enemyAgent = array[j];
                    if (enemyAgent != null && enemyAgent.Damage != null)
                    {
                        enemyAgent.Damage.MeleeDamage(float.MaxValue, null, UnityEngine.Vector3.zero, UnityEngine.Vector3.up, 0, 1f, 1f, 1f, 1f, false, DamageNoiseLevel.Normal);
                    }
                }
            }
        }

        private static void KillEnemiesInDimension_Custom(WardenObjectiveEventData eventToTrigger)
        {
            if (!SNet.IsMaster) return;
            Dimension dimension = null;
            Dimension.GetDimension(eventToTrigger.DimensionIndex, out dimension);

            for (int index1 = 0; index1 < dimension.Layers.Count; ++index1)
            {
                LG_Layer layer = dimension.Layers[index1];
                for (int index2 = 0; index2 < layer.m_zones.Count; ++index2)
                {
                    LG_Zone zone2 = layer.m_zones[index2];
                    LG_SecurityDoor door;

                    Utils.Helper.TryGetZoneEntranceSecDoor(zone2, out door);

                    // limited kill
                    if (index2 == 0 || door != null && door.m_sync.GetCurrentSyncState().status == eDoorStatus.Open) // door opened, kill all
                    {
                        KillEnemiesInZone(zone2);
                    }
                }
            }
        }

        private static void ToggleEnableDisableTerminal(LG_ComputerTerminal terminal, bool Enabled)
        {
            if (terminal == null) return;

            if (Enabled == false)
            {
                terminal.m_command.ClearOutputQueueAndScreenBuffer();
                //terminal.TrySyncSetCommandHidden(TERM_Command.MAX_COUNT);
            }
            else
            {
                terminal.m_command.AddInitialTerminalOutput();
                //terminal.TrySyncSetCommandShow(TERM_Command.MAX_COUNT);
            }

            terminal.transform.FindChild("Interaction").gameObject.active = Enabled;
            UnityEngine.Transform child = terminal.transform.FindChild("Graphics/kit_ElectronicsTerminalConsole/Display");
            if (child != null)
            {
                child.gameObject.active = Enabled;
            }
        }

        private static void ToggleEnableDisableAllTerminalsInZone_Custom(WardenObjectiveEventData eventToTrigger)
        {
            WardenObjectiveEventData e = eventToTrigger;

            LG_Zone zone = null;
            Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out zone);
            if (zone == null)
            {
                Logger.Error("ToggleEnableDisableAllTerminalsInZone_Custom - Failed to find LG_Zone.");
                Logger.Error("DimensionIndex: {0}, Layer: {1}, LocalIndex: {2}", eventToTrigger.DimensionIndex, eventToTrigger.Layer, eventToTrigger.LocalIndex);
                return;
            }

            foreach (LG_ComputerTerminal terminalInZone in zone.TerminalsSpawnedInZone)
            {
                ToggleEnableDisableTerminal(terminalInZone, e.Enabled);
            }
        }

        private static void ToggleEnableDisableTerminalInZone_Custom(WardenObjectiveEventData eventToTrigger)
        {
            WardenObjectiveEventData e = eventToTrigger;

            if (e.Count < 0)
            {
                Logger.Error("ToggleEnableDisableTerminalInZone_Custom - Count < 0");
                return;
            }

            LG_Zone zone = null;
            Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out zone);
            if (zone == null)
            {
                Logger.Error("ToggleEnableDisableTerminalInZone_Custom - Failed to find LG_Zone.");
                Logger.Error("DimensionIndex: {0}, Layer: {1}, LocalIndex: {2}", eventToTrigger.DimensionIndex, eventToTrigger.Layer, eventToTrigger.LocalIndex);
                return;
            }

            if (e.Count >= zone.TerminalsSpawnedInZone.Count)
            {
                Logger.Error("ToggleEnableDisableTerminalInZone_Custom - Count >= Spawned terminal count");
                return;
            }

            ToggleEnableDisableTerminal(zone.TerminalsSpawnedInZone[e.Count], e.Enabled);
        }

        private static void KillEnemiesInZone_Custom(WardenObjectiveEventData eventToTrigger)
        {
            if (!SNet.IsMaster) return;

            WardenObjectiveEventData e = eventToTrigger;

            LG_Zone zone = null;
            Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out zone);
            if (zone == null)
            {
                Logger.Error("KillEnemiesInZone_Custom - Failed to find LG_Zone.");
                Logger.Error("DimensionIndex: {0}, Layer: {1}, LocalIndex: {2}", eventToTrigger.DimensionIndex, eventToTrigger.Layer, eventToTrigger.LocalIndex);
                return;
            }

            KillEnemiesInZone(zone);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WardenObjectiveManager), nameof(WardenObjectiveManager.CheckAndExecuteEventsOnTrigger), new System.Type[] {
            typeof(WardenObjectiveEventData),
            typeof(eWardenObjectiveEventTrigger),
            typeof(bool),
            typeof(float)
        })]
        private static bool Pre_CheckAndExecuteEventsOnTrigger(WardenObjectiveEventData eventToTrigger,
            eWardenObjectiveEventTrigger trigger,
            bool ignoreTrigger,
            float currentDuration)
        {
            if (eventToTrigger == null || !ignoreTrigger && eventToTrigger.Trigger != trigger || currentDuration != 0.0 && eventToTrigger.Delay <= currentDuration)
                return true;

            // specified condition and condition unsatisfied
            if (eventToTrigger.Condition.ConditionIndex >= 0
                && WorldEventManager.GetCondition(eventToTrigger.Condition.ConditionIndex) != eventToTrigger.Condition.IsTrue)
            {
                return true;
            }

            UnityEngine.Coroutine coroutine = null;

            // custom event
            switch ((int)eventToTrigger.Type)
            {
                case (int)EventType.CloseSecurityDoor_Custom:
                case (int)EventType.KillEnemiesInDimension_Custom:
                case (int)EventType.SetTimerTitle_Custom:
                case (int)EventType.ToggleEnableDisableAllTerminalsInZone_Custom:
                case (int)EventType.ToggleEnableDisableTerminalInZone_Custom:
                case (int)EventType.KillEnemiesInZone_Custom:
                case (int)EventType.AlertEnemiesInZone:
                case (int)EventType.AlertEnemiesInArea:
                case (int)EventType.TP_WarpTeamsToArea:
                case (int)EventType.SpawnEnemy_Hibernate:
                case (int)EventType.Reactor_CompleteCurrentWave:
                    coroutine = CoroutineManager.StartCoroutine(Handle(eventToTrigger, currentDuration).WrapToIl2Cpp(), null);
                    WorldEventManager.m_worldEventEventCoroutines.Add(coroutine);
                    return false;
                case (int)EventType.StopSpecifiedEnemyWave:
                    SpawnSurvivalWave_Custom.StopSpecifiedWave(eventToTrigger, currentDuration);
                    return false;
                case (int)EventType.ChainedPuzzle_AddReqItem:
                    coroutine = CoroutineManager.StartCoroutine(ChainedPuzzle_Custom.AddReqItem(eventToTrigger, currentDuration).WrapToIl2Cpp(), null);
                    WorldEventManager.m_worldEventEventCoroutines.Add(coroutine);
                    return false;
                case (int)EventType.ChainedPuzzle_RemoveReqItem:
                    coroutine = CoroutineManager.StartCoroutine(ChainedPuzzle_Custom.RemoveReqItem(eventToTrigger, currentDuration).WrapToIl2Cpp(), null);
                    WorldEventManager.m_worldEventEventCoroutines.Add(coroutine);
                    return false;
            }

            // vanilla event modification
            switch (eventToTrigger.Type)
            {
                case eWardenObjectiveEventType.SetTerminalCommand:
                    coroutine = CoroutineManager.StartCoroutine(Handle(eventToTrigger, currentDuration).WrapToIl2Cpp(), null);
                    WorldEventManager.m_worldEventEventCoroutines.Add(coroutine);
                    //WardenObjectiveManager.m_wardenObjectiveEventCoroutines.Add(coroutine);
                    return false;
                case eWardenObjectiveEventType.SpawnEnemyWave:
                    bool use_vanilla_impl = SpawnSurvivalWave_Custom.SpawnWave(eventToTrigger, currentDuration);
                    return use_vanilla_impl;
                case eWardenObjectiveEventType.StopEnemyWaves:
                    SpawnSurvivalWave_Custom.OnStopAllWave();
                    return true;
                case eWardenObjectiveEventType.ActivateChainedPuzzle:
                    coroutine = CoroutineManager.StartCoroutine(ChainedPuzzle_Custom.ActivateChainedPuzzle(eventToTrigger, currentDuration).WrapToIl2Cpp(), null);
                    WorldEventManager.m_worldEventEventCoroutines.Add(coroutine);
                    return false;
                default: return true;
            }
        }

        private static IEnumerator Handle(WardenObjectiveEventData eventToTrigger, float currentDuration)
        {
            WardenObjectiveEventData e = eventToTrigger;

            float delay = UnityEngine.Mathf.Max(e.Delay - currentDuration, 0f);
            if (delay > 0f)
            {
                yield return new UnityEngine.WaitForSeconds(delay);
            }

            WardenObjectiveManager.DisplayWardenIntel(e.Layer, e.WardenIntel);
            if (e.DialogueID > 0u)
            {
                PlayerDialogManager.WantToStartDialog(e.DialogueID, -1, false, false);
            }
            if (e.SoundID > 0u)
            {
                WardenObjectiveManager.Current.m_sound.Post(e.SoundID, true);
                var line = e.SoundSubtitle.ToString();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    GuiManager.PlayerLayer.ShowMultiLineSubtitle(line);
                }
            }

            switch ((int)e.Type)
            {
                case (int)EventType.CloseSecurityDoor_Custom:
                    bool close_success = CloseSecurityDoor_Custom(e);
                    if (close_success == false)
                    {
                        break;
                    }

                    LG_Zone zone = null;
                    Builder.CurrentFloor.TryGetZoneByLocalIndex(eventToTrigger.DimensionIndex, eventToTrigger.Layer, eventToTrigger.LocalIndex, out zone);
                    if (zone != null && e.ClearDimension)
                    {
                        yield return new UnityEngine.WaitForSeconds(5.0f);
                        KillEnemiesInZone(zone);
                    }
                    break;
                case (int)EventType.KillEnemiesInDimension_Custom:
                    KillEnemiesInDimension_Custom(e); break;
                case (int)EventType.ToggleEnableDisableAllTerminalsInZone_Custom:
                    ToggleEnableDisableAllTerminalsInZone_Custom(e); break;
                case (int)EventType.ToggleEnableDisableTerminalInZone_Custom:
                    ToggleEnableDisableTerminalInZone_Custom(e); break;
                case (int)EventType.KillEnemiesInZone_Custom:
                    KillEnemiesInZone_Custom(e); break;
                case (int)EventType.AlertEnemiesInZone:
                case (int)EventType.AlertEnemiesInArea:
                    AlertEnemies(e, (uint)e.Type == (uint)EventType.AlertEnemiesInZone); break;
                case (int)EventType.Reactor_CompleteCurrentWave:
                    CompleteCurrentReactorWave(e); break;
                case (int)EventType.SpawnEnemy_Hibernate:
                    SpawnEnemy_Hibernate(e); break;
                case (int)EventType.TP_WarpTeamsToArea:
                    WarpTeamsToArea(e); break;
                case (int)EventType.SetTimerTitle_Custom:
                    {
                        float duration = e.Duration;

                        // set title
                        if (duration <= 0.0) // no idea why this fked up
                        {
                            // disable title
                            if (e.CustomSubObjectiveHeader.ToString().Length == 0)
                            {
                                GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(false);
                            }
                            // enable title
                            else
                            {
                                GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(true);
                                GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerTitle(e.CustomSubObjectiveHeader.ToString());
                                GuiManager.PlayerLayer.m_objectiveTimer.SetTimerTextEnabled(false);
                            }

                            break;
                        }

                        // count down
                        else
                        {
                            GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(true, true);
                            GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerTitle(e.CustomSubObjectiveHeader.ToString());
                            GuiManager.PlayerLayer.m_objectiveTimer.SetTimerTextEnabled(true);

                            UnityEngine.Color color;
                            if (UnityEngine.ColorUtility.TryParseHtmlString(e.CustomSubObjective.ToString(), out color) == false)
                            {
                                color.r = color.g = color.b = 255.0f;
                            }

                            var time = 0.0f;
                            while (time <= duration)
                            {
                                if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
                                {
                                    break;
                                }

                                GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerText(duration - time, duration, color);
                                time += UnityEngine.Time.deltaTime;
                                yield return null;
                            }

                            GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(false, true);

                            break;
                        }
                    }
            }

            switch (e.Type)
            {
                case eWardenObjectiveEventType.SetTerminalCommand:
                    SetTerminalCommand_Custom(e); break;
            }
        }
    }
}