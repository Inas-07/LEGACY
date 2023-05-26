using Agents;
using AIGraph;
using Enemies;
using GameData;
using LEGACY.LegacyOverride;
using LEGACY.Utils;
using LevelGeneration;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using BepInEx.Unity.IL2CPP.Utils.Collections;

namespace LEGACY.ExtraEvents
{
    internal static class SpawnHibernate
    {
        internal static void Debug_OutputLevelHibernateSpawnEvent(WardenObjectiveEventData e)
        {
            LegacyLogger.Warning("Debug_OutputLevelHibernateSpawnEvent: This event involves IO operation. Do not use this event on released rundown!");

            string dirPath = Path.Combine(LegacyOverrideManagers.LEGACY_CONFIG_PATH, "LevelHibernateSpawnEvents");

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            string levelName = $"{RundownManager.ActiveExpedition.Descriptive.Prefix} {RundownManager.ActiveExpedition.Descriptive.PublicName}";
            string levelDir = Path.Combine(dirPath, levelName);
            if (!Directory.Exists(levelDir))
            {
                Directory.CreateDirectory(levelDir);
            }

            List<SpawnHibernateEnemiesEvent> events = new();
            foreach (var zone in Builder.CurrentFloor.allZones)
            {
                var map = ZoneEnemiesInfo(zone);
                foreach (var enemyID in map.Keys)
                {
                    SpawnHibernateEnemiesEvent _e = new()
                    {
                        DimensionIndex = zone.DimensionIndex,
                        Layer = zone.m_layer.m_type,
                        LocalIndex = zone.LocalIndex,
                        EnemyID = enemyID,
                        Count = (int)map[enemyID]
                    };

                    events.Add(_e);
                }

                if (events.Count == 0) continue;

                var file = File.CreateText(Path.Combine(levelDir, $"Zone_{(int)zone.LocalIndex}, {zone.Layer.m_type}, {zone.DimensionIndex}.json"));
                file.WriteLine(Json.Serialize(events));
                file.Flush();
                file.Close();

                events.Clear();
            }
        }

        internal static void Debug_LevelEnemiesInfo(WardenObjectiveEventData e)
        {
            StringBuilder s = new();
            s.AppendLine();
            foreach (var zone in Builder.CurrentFloor.allZones)
            {
                s.AppendLine($"Zone {zone.LocalIndex}, {zone.Layer.m_type}, {zone.DimensionIndex}:");

                var map = ZoneEnemiesInfo(zone);
                foreach (var enemyID in map.Keys)
                {
                    var db = GameDataBlockBase<EnemyDataBlock>.GetBlock(enemyID);
                    s.AppendLine($"{db.name}, Num: {map[enemyID]}, ID: {enemyID}");
                }

                s.AppendLine();
            }

            LegacyLogger.Debug(s.ToString());
        }

        internal static void Debug_ZoneEnemiesInfo(WardenObjectiveEventData e)
        {
            LG_Zone zone;
            if (!Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out zone) || zone == null)
            {
                LegacyLogger.Error($"Debug_ZoneEnemiesInfo: cannot find zone {e.LocalIndex}, {e.Layer}, {e.DimensionIndex}");
                return;
            }

            var map = ZoneEnemiesInfo(zone);

            StringBuilder s = new();
            foreach (var enemyID in map.Keys)
            {
                var db = GameDataBlockBase<EnemyDataBlock>.GetBlock(enemyID);
                s.AppendLine($"{db.name}, Num: {map[enemyID]}, ID: {enemyID}");
            }

            LegacyLogger.Debug(s.ToString());
        }

        public static Dictionary<uint, uint> ZoneEnemiesInfo(LG_Zone zone)
        {
            Dictionary<uint, uint> map = new();

            foreach (var node in zone.m_courseNodes)
            {
                foreach (var enemy in node.m_enemiesInNode)
                {
                    if (map.ContainsKey(enemy.EnemyData.persistentID))
                    {
                        map[enemy.EnemyData.persistentID]++;
                    }
                    else
                    {
                        map.Add(enemy.EnemyData.persistentID, 1);
                    }
                }
            }

            return map;
        }

        private static System.Collections.IEnumerator Spawn(WardenObjectiveEventData e)
        {
            LG_Zone zone;
            if (!Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out zone) || zone == null)
            {
                LegacyLogger.Error($"SpawnEnemy_Hibernate: cannot find zone {e.LocalIndex}, {e.Layer}, {e.DimensionIndex}");
                yield break;
            }

            string worldEventObjectFilter = e.WorldEventObjectFilter.ToUpperInvariant();

            // SPAWN ON POSITION
            if (e.Position != UnityEngine.Vector3.zero)
            {
                LegacyLogger.Debug($"SpawnEnemy_Hibernate: using SpawnOnPosition, will only spawn 1 enemy (spawning scout is not supported).\nYou'll have to specify the correct area as well");

                if (!worldEventObjectFilter.Contains("AREA_") || worldEventObjectFilter.Length != "AREA_".Length + 1)
                {
                    LegacyLogger.Error($"SpawnEnemy_Hibernate: invalid WorldEventObjectFilter {e.WorldEventObjectFilter}");
                    yield break;
                }

                int areaIndex = worldEventObjectFilter[worldEventObjectFilter.Length - 1] - 'A';
                if (areaIndex < 0 || areaIndex >= zone.m_areas.Count)
                {
                    LegacyLogger.Error($"SpawnEnemy_Hibernate: invalid WorldEventObjectFilter - didn't find AREA_{worldEventObjectFilter[worldEventObjectFilter.Length - 1]}");
                    yield break;
                }

                UnityEngine.Quaternion rotation = UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(EnemyGroup.s_randomRot2D.x, 0.0f, EnemyGroup.s_randomRot2D.y), UnityEngine.Vector3.up);
                EnemyAllocator.Current.SpawnEnemy(e.EnemyID, zone.m_areas[areaIndex].m_courseNode, AgentMode.Hibernate, e.Position, rotation);
            
                LegacyLogger.Debug($"SpawnEnemy_Hibernate: spawned {e.Count} enemy/enemies on position ({e.Position.x}, {e.Position.y}, {e.Position.z}), in zone_{e.LocalIndex}, {e.Layer}, {e.DimensionIndex}");

                yield break;
            }

            // SPAWN UNDER GIVEN RULE
            else
            {
                float CompleteSpawnInSeconds = e.Duration != 0.0f ? e.Duration : 2.0f;
                float SpawnInterval = CompleteSpawnInSeconds / e.Count;

                AgentMode mode = AgentMode.Hibernate;

                switch (e.EnemyID)
                {
                    case 20:
                    case 40:
                    case 41:
                    case 23:
                    case 48:
                        mode = AgentMode.Scout; break;
                }

                Random rand = new Random();
                string candidateAreas = string.Empty;

                for (int SpawnCount = 0; SpawnCount < e.Count; SpawnCount++)
                {
                    AIG_CourseNode node = null;
                    UnityEngine.Vector3 position = UnityEngine.Vector3.zero;

                    switch (worldEventObjectFilter)
                    {
                        case "RANDOM":
                        case "RANDOM_AREA":
                        case "":
                            for (int c = 0; c < e.Count; c++)
                            {
                                // most time-consuming part
                                // Luckily we are now in an coroutine
                                node = zone.m_areas[rand.Next(zone.m_areas.Count)].m_courseNode;
                                position = node.GetRandomPositionInside();
                            }

                            break;
                        case "AREA_WEIGHTED":
                            WeightedAreaSelector selector = WeightedAreaSelector.Get(zone);
                            // most time-consuming part
                            // Luckily we are now in an coroutine
                            node = selector.GetRandom().m_courseNode;
                            position = node.GetRandomPositionInside();
                            break;

                        default:
                            if(string.IsNullOrEmpty(candidateAreas))
                            {
                                if (worldEventObjectFilter.Contains("AREA_") && worldEventObjectFilter.Length - "AREA_".Length > 0)
                                {
                                    candidateAreas = worldEventObjectFilter.Substring("AREA_".Length);
                                    foreach(char c in candidateAreas)
                                    {
                                        int _areaIndex = c - 'A';
                                        if (_areaIndex < 0 || _areaIndex >= zone.m_areas.Count)
                                        {
                                            LegacyLogger.Error($"SpawnEnemy_Hibernate: invalid WorldEventObjectFilter - didn't find AREA_{_areaIndex}");
                                            yield break;
                                        }
                                    }
                                }
                                else
                                {
                                    LegacyLogger.Error("SpawnEnemy_Hibernate: invalid format for WorldEventObjectFilter, should be one of RANDOM, \"\", AREA_{area letters}");
                                    yield break;
                                }
                            }


                            char selectedArea = candidateAreas[rand.Next(candidateAreas.Length)];
                            int areaIndex = selectedArea - 'A';
                            node = zone.m_areas[areaIndex].m_courseNode;
                            position = node.GetRandomPositionInside();
                            break;
                    }

                    if(UnityEngine.Vector3.Equals(position, UnityEngine.Vector3.zero))
                    {
                        LegacyLogger.Error("SpawnEnemy_Hibernate: got zero position vector, aborting all spawn!");
                        yield break;
                    }

                    // scout:
                    if (mode != AgentMode.Scout)
                    {
                        UnityEngine.Quaternion rotation = UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(EnemyGroup.s_randomRot2D.x, 0.0f, EnemyGroup.s_randomRot2D.y), UnityEngine.Vector3.up);
                        EnemyAllocator.Current.SpawnEnemy(e.EnemyID, node, mode, position, rotation);
                    }

                    else
                    {
                        SpawnScout(e.EnemyID, node, position);
                    }

                    yield return new UnityEngine.WaitForSeconds(SpawnInterval);
                }

                LegacyLogger.Debug($"SpawnEnemy_Hibernate: spawned {e.Count} enemy/enemies in {CompleteSpawnInSeconds}s in zone {e.LocalIndex}, {e.Layer}, {e.DimensionIndex}");
                yield break;
            }
        }

        internal static void SpawnEnemy_Hibernate(WardenObjectiveEventData e)
        {
            if (!SNet.IsMaster) return;

            UnityEngine.Coroutine coroutine = CoroutineManager.StartCoroutine(Spawn(e).WrapToIl2Cpp(), null);
            WorldEventManager.m_worldEventEventCoroutines.Add(coroutine);
        }

        private static void SpawnScout(uint scoutID, AIG_CourseNode node, UnityEngine.Vector3 position)
        {
            eEnemyGroupType groupType = (eEnemyGroupType)3;
            eEnemyRoleDifficulty difficulty = 0;

            switch (scoutID)
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
                    LegacyLogger.Error($"Undefined scout, enemy ID {scoutID}");
                    break;
            }

            EnemyGroupRandomizer r = null;
            if (!EnemySpawnManager.TryCreateEnemyGroupRandomizer(groupType, difficulty, out r) || r == null)
            {
                LegacyLogger.Error("EnemySpawnManager.TryCreateEnemyGroupRandomizer false");
                return;
            }

            EnemyGroupDataBlock randomGroup = r.GetRandomGroup(Builder.SessionSeedRandom.Value());
            float popPoints = randomGroup.MaxScore * Builder.SessionSeedRandom.Range(1f, 1.2f);

            var scoutSpawnData = EnemyGroup.GetSpawnData(position, node, EnemyGroupType.Hibernating,
            eEnemyGroupSpawnType.RandomInArea, randomGroup.persistentID, popPoints) with
            {
                respawn = false
            };

            EnemyGroup.Spawn(scoutSpawnData);
        }
    }
}
