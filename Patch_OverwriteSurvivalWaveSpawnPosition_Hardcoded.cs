using Il2CppSystem.Collections.Generic; // dont use System.Generic.Collections
using HarmonyLib;
using LevelGeneration;
using GameData;
using LEGACY.Utilities;
using AIGraph;
using Player;
using GTFO.API;

namespace LEGACY.Hardcoded_Behaviour
{
    [HarmonyPatch]
    internal class Patch_OverwriteSurvivalWaveSpawnPosition_Hardcoded
    {
        private static uint currentMainLayerUID = 0u;

        private static void OnBuildDone()
        {
            currentMainLayerUID = RundownManager.ActiveExpedition.LevelLayoutData;
            Logger.Warning("OnBuildDone Patch_OverwriteSurvivalWaveSpawnPosition_Hardcoded");
        }

        private static eDimensionIndex GetCurrentDimensionIndex()
        {
            if(PlayerManager.PlayerAgentsInLevel.Count <= 0)
            {
                throw new System.Exception("? You don't have any player agent in level? How could that happen?");
            }

            return PlayerManager.PlayerAgentsInLevel[0].DimensionIndex;
        }

        // what about dimension index?
        // DimensionIndex is incomparable! Since all players must be in the same dimension
        private static void GetMinLayerAndLocalIndex(out LG_LayerType MinLayer, out eLocalZoneIndex MinLocalIndex)
        {
            MinLayer = LG_LayerType.ThirdLayer;
            MinLocalIndex = eLocalZoneIndex.Zone_20;

            foreach(PlayerAgent player in PlayerManager.PlayerAgentsInLevel)
            {
                if (!Utils.isPlayerInLevel(player)) continue;

                //SNetwork.SNet_Player player2
                if(MinLayer > player.m_courseNode.LayerType)
                {
                    MinLayer = player.m_courseNode.LayerType;
                    MinLocalIndex = eLocalZoneIndex.Zone_20;
                }

                if(MinLocalIndex >= player.m_courseNode.m_zone.LocalIndex)
                {
                    MinLocalIndex = player.m_courseNode.m_zone.LocalIndex;
                }
            }
        }

        private static void GetMaxLayerAndLocalIndex(out LG_LayerType MaxLayer, out eLocalZoneIndex MaxLocalIndex)
        {
            MaxLayer = LG_LayerType.MainLayer;
            MaxLocalIndex = eLocalZoneIndex.Zone_0;

            foreach(PlayerAgent player in PlayerManager.PlayerAgentsInLevel)
            {
                if (!Utils.isPlayerInLevel(player)) continue;

                //SNetwork.SNet_Player player2
                if(MaxLayer < player.m_courseNode.LayerType)
                {
                    MaxLayer = player.m_courseNode.LayerType;
                    MaxLocalIndex = eLocalZoneIndex.Zone_0;
                }

                if(MaxLocalIndex < player.m_courseNode.m_zone.LocalIndex)
                {
                    MaxLocalIndex = player.m_courseNode.m_zone.LocalIndex;
                }
            }
        }

        // return negative value if error occurred.
        // return LG_Zone.Count if there's no agent in the specified zone.
        private static int GetMinAreaIndex(LG_Zone zone)
        {
            if (zone == null) return -1;

            LG_LayerType Layer = zone.m_layer.m_type;
            eLocalZoneIndex LocalIndex = zone.LocalIndex;

            eDimensionIndex dimensionIndex = GetCurrentDimensionIndex();

            int minAreaIndex = zone.m_areas.Count;

            foreach (PlayerAgent player in PlayerManager.PlayerAgentsInLevel)
            {
                if (player.m_courseNode.LayerType == Layer && player.m_courseNode.m_zone.LocalIndex == LocalIndex)
                {
                    int areaIndex = 0;
                    List<LG_Area> areas = zone.m_areas;
                    while (areaIndex < areas.Count)
                    {
                        if (areas[areaIndex].gameObject.GetInstanceID() == player.m_courseNode.m_area.gameObject.GetInstanceID())
                        {
                            if (minAreaIndex > areaIndex) minAreaIndex = areaIndex;
                            break;
                        }
                        areaIndex++;
                    }
                }
            }

            return minAreaIndex;
        }

        // Note: no longer required since I can now overwrite scout wave in EventsOnPortalWarp via Patch_EventsOnZoneScoutScream
        private static SurvivalWave.ScoredSpawnPoint L2E1_GetScoredSpawnPoint_FromElevator_Overwrite()
        {
            eLocalZoneIndex minLocalIndex;// = eLocalZoneIndex.Zone_20;
            LG_LayerType minLayerType;// = LG_LayerType.SecondaryLayer;

            GetMinLayerAndLocalIndex(out minLayerType, out minLocalIndex);
            if (minLayerType != LG_LayerType.MainLayer) return null;
            AIG_CourseNode spawnNode = null;
            switch (minLocalIndex)
            {
                case eLocalZoneIndex.Zone_0:
                case eLocalZoneIndex.Zone_1:
                    spawnNode = Builder.GetElevatorArea().m_courseNode;
                    break;
                case eLocalZoneIndex.Zone_2:
                    spawnNode = Builder.GetElevatorZone().m_areas[1].m_courseNode;
                    break;
                default: return null;
            }

            return spawnNode == null ? null : new SurvivalWave.ScoredSpawnPoint(){
                courseNode = spawnNode
            };
        }

        // Note: no longer required since I can now overwrite scout wave in EventsOnPortalWarp via Patch_EventsOnZoneScoutScream
        private static SurvivalWave.ScoredSpawnPoint L3E1_GetScoredSpawnPoint_FromElevator_Overwrite()
        {
            Logger.Warning("L3E1 hardcoded spawn-overwrite method not updated. Remember to update it according to new layout");
            eLocalZoneIndex minLocalIndex;// = eLocalZoneIndex.Zone_20;
            LG_LayerType minLayerType;// = LG_LayerType.SecondaryLayer;

            GetMinLayerAndLocalIndex(out minLayerType, out minLocalIndex);

            if (minLayerType == LG_LayerType.MainLayer && (uint)minLocalIndex > 11u /*post main*/ ) 
            {
                // use vanilla impl.
                return null;
            }

            AIG_CourseNode spawnNode = null;

            LG_Zone mainZone2;
            if (Builder.CurrentFloor.TryGetZoneByLocalIndex(eDimensionIndex.Reality, LG_LayerType.MainLayer, eLocalZoneIndex.Zone_2, out mainZone2) == false)
            {
                if (mainZone2 == null)
                {
                    Logger.Error("sth went wrong with try get zone by main layer, local index 2");
                    return null;
                }
            }

            // only 1 area allowed
            LG_Zone spawnedCustomGeoZone = null;

            switch (minLayerType)
            {
                case LG_LayerType.MainLayer: 
                   switch (minLocalIndex)
                   {
                        case eLocalZoneIndex.Zone_0:
                        case eLocalZoneIndex.Zone_1:
                            spawnNode = Builder.GetElevatorZone().m_courseNodes[0];
                            break;

                        case eLocalZoneIndex.Zone_2: // hardest part

                            int minAreaIndex = GetMinAreaIndex(mainZone2);

                            switch (minAreaIndex)
                            {
                                case 0:
                                    spawnNode = Builder.GetElevatorZone().m_courseNodes[1];
                                    break;
                                case 1:
                                case 2:
                                    if(Builder.CurrentFloor.TryGetZoneByLocalIndex(eDimensionIndex.Reality, LG_LayerType.MainLayer, eLocalZoneIndex.Zone_1, out spawnedCustomGeoZone) == false)
                                    {
                                        if(spawnedCustomGeoZone == null)
                                        {
                                            Logger.Error("sth went wrong with getting local index 1 zone");
                                            return null;
                                        }
                                        Logger.Warning("return false but non null :)");
                                    }
                                    spawnNode = spawnedCustomGeoZone.m_courseNodes[0];
                                    break;
                                case 3:
                                    spawnNode = mainZone2.m_courseNodes[0];
                                    break;
                                case 4:
                                    spawnNode = mainZone2.m_courseNodes[1];
                                    break;
                                default:
                                    Logger.Error("minAreaIndex == {0}?????", minAreaIndex);
                                    return null;
                            }

                            break;
                        case eLocalZoneIndex.Zone_3:
                            spawnNode = mainZone2.m_courseNodes[mainZone2.m_courseNodes.Count - 1];
                            break;

                        default: break;
                   }
                   break;

                case LG_LayerType.SecondaryLayer:
                   switch(minLocalIndex)
                   {
                        case eLocalZoneIndex.Zone_0:
                            spawnNode = mainZone2.m_courseNodes[0];
                            break;
                        case eLocalZoneIndex.Zone_1:
                            spawnNode = mainZone2.m_courseNodes[1];
                            break;

                        case eLocalZoneIndex.Zone_2:
                            if (Builder.CurrentFloor.TryGetZoneByLocalIndex(eDimensionIndex.Reality, minLayerType, eLocalZoneIndex.Zone_0, out spawnedCustomGeoZone) == false)
                            {
                                if (spawnedCustomGeoZone == null)
                                {
                                    Logger.Error("sth went wrong with getting local index 1 zone");
                                    return null;
                                }
                                Logger.Warning("return false but non null :)");
                            }
                            spawnNode = spawnedCustomGeoZone.m_courseNodes[0];
                            break;

                        default: break;
                   }
                   break;

                default: 
                    Logger.Error("sth went wrong. You got LG_LayerType == ThirdLayer in L3E1, which is not possible!");
                    return null;
            }

            if(spawnNode != null)
            {
                return new SurvivalWave.ScoredSpawnPoint()
                {
                    courseNode = spawnNode
                };
            }

            return null;
        }

        private static SurvivalWave.ScoredSpawnPoint L3E2_GetScoredSpawnPoint_FromElevator_Overwrite()
        {
            eLocalZoneIndex minLocalIndex;  // = eLocalZoneIndex.Zone_20;
            LG_LayerType minLayerType;      // = LG_LayerType.SecondaryLayer;
            eDimensionIndex dimensionIndex = GetCurrentDimensionIndex();

            GetMinLayerAndLocalIndex(out minLayerType, out minLocalIndex);

            AIG_CourseNode spawnNode = null;

            // A - upper layer. B - lower layer
            if (minLayerType == LG_LayerType.SecondaryLayer && minLocalIndex == eLocalZoneIndex.Zone_3)
            {
                LG_Zone zone123 = null;
                if (Builder.CurrentFloor.TryGetZoneByLocalIndex(dimensionIndex, minLayerType, minLocalIndex, out zone123) == false || zone123 == null) {
                 
                    Logger.Error("Failed to get zone 123 by local index.");
                    return null;
                }

                int minAreaIndex = GetMinAreaIndex(zone123);

                switch(minAreaIndex)
                {
                    case 0:
                        spawnNode = Builder.GetElevatorZone().m_areas[1].m_courseNode;
                        break;
                    case 1:
                        spawnNode = zone123.m_areas[0].m_courseNode;
                        break;
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        LG_Zone spawnZone = null;
                        if (Builder.CurrentFloor.TryGetZoneByLocalIndex(dimensionIndex, LG_LayerType.SecondaryLayer, eLocalZoneIndex.Zone_0, out spawnZone) == false || spawnZone == null)
                        {
                            Logger.Error("TryGetZoneByLocalIndex failed.");
                            return null;
                        }

                        spawnNode = spawnZone.m_areas[0].m_courseNode;
                        break;

                    default: 
                        Logger.Error("minAreaIndex == {0}?????", minAreaIndex);
                        return null;
                }
            }

            else if(minLayerType == LG_LayerType.MainLayer)
            {
                if (dimensionIndex == eDimensionIndex.Reality)
                {
                    switch(minLocalIndex)
                    {
                        case eLocalZoneIndex.Zone_0: spawnNode = Builder.GetElevatorArea().m_courseNode; break;
                        default: return null;
                    }
                }
                else
                {
                    LG_Zone zone14 = null;
                    Builder.CurrentFloor.TryGetZoneByLocalIndex(dimensionIndex, LG_LayerType.MainLayer, eLocalZoneIndex.Zone_2, out zone14);
                    if(zone14 == null)
                    {
                        Logger.Error("Failed to get zone 14 in dimension");
                        return null;
                    }

                    LG_Zone dm_zone = null;
                    switch (minLocalIndex)
                    {
                        case eLocalZoneIndex.Zone_0:
                        case eLocalZoneIndex.Zone_1:
                            if (Utils.isSecDoorToZoneOpened(zone14))
                            {
                                Builder.CurrentFloor.TryGetZoneByLocalIndex(dimensionIndex, LG_LayerType.MainLayer, eLocalZoneIndex.Zone_0, out dm_zone);
                                if (dm_zone == null) return null;
                                spawnNode = dm_zone.m_areas[0].m_courseNode;
                            }
                            else
                            {
                                return null;
                            }
                            break;
                        case eLocalZoneIndex.Zone_2:
                            Builder.CurrentFloor.TryGetZoneByLocalIndex(dimensionIndex, LG_LayerType.MainLayer, eLocalZoneIndex.Zone_0, out dm_zone);
                            if (dm_zone == null) return null;
                            spawnNode = dm_zone.m_areas[0].m_courseNode; 
                           
                            break;
                        default: return null;
                    }                    
                }
            }

            if (spawnNode == null) return null;
            
            return new SurvivalWave.ScoredSpawnPoint() { courseNode = spawnNode };
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SurvivalWave), nameof(SurvivalWave.GetScoredSpawnPoint_FromElevator))]
        private static bool Pre_GetScoredSpawnPoint_FromElevator(SurvivalWave __instance, ref SurvivalWave.ScoredSpawnPoint __result)
        {
            SurvivalWave.ScoredSpawnPoint overwritten_result = null;
            switch (currentMainLayerUID)
            {
                case (uint)MainLayerID.L3E1:
                    return true; // now we don't use FromElevatorDirection spawn - just too lazy :) 

                    //overwritten_result = L3E1_GetScoredSpawnPoint_FromElevator_Overwrite();
                    //if (overwritten_result == null)
                    //{
                    //    return true;
                    //}

                    //__result = overwritten_result;
                    //return false;

                case (uint)MainLayerID.L3E2:
                    overwritten_result = L3E2_GetScoredSpawnPoint_FromElevator_Overwrite();
                    if (overwritten_result == null)
                    {
                        return true;
                    }

                    __result = overwritten_result;
                    return false;

                case (uint)MainLayerID.L2E1:
                    return true;
                    //overwritten_result = L2E1_GetScoredSpawnPoint_FromElevator_Overwrite();
                    //if (overwritten_result == null)
                    //{
                    //    return true;
                    //}

                    //__result = overwritten_result;
                    //return false;

                default: return true;
            }
        }

        // Overwrite OnSpawnPoint: instead of spawn at specified postion, instead spawn at random position in the course node
        // Note: no longer required since I now have more powerful spawn type.
        //       Just left this patch unmodified.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SurvivalWave), nameof(SurvivalWave.SpawnGroup))]
        private static bool Pre_SpawnGroup_Overwrite_OnSpawnPoint_SpawnPosition(SurvivalWave __instance)
        {
            if (!SNetwork.SNet.IsMaster)
            {
                Logger.Error("ERROR : Slave spawning enemy group through survival wave??");
                return false;
            }

            if (__instance.m_spawnType != SurvivalWaveSpawnType.OnSpawnPoints) return true;

            switch (currentMainLayerUID)
            {
                case (uint)MainLayerID.L3E2:
                    SurvivalWave.ScoredSpawnPoint scoredSpawnPoint = __instance.GetScoredSpawnPoint(__instance.m_spawnDirection);

                    if (scoredSpawnPoint == null || scoredSpawnPoint.courseNode == null)
                    {
                        __instance.StopEvent();
                    }
                    else
                    {
                        UnityEngine.Quaternion rot = UnityEngine.Quaternion.identity;
                        Enemies.eEnemyGroupSpawnType spawnType;
                        UnityEngine.Vector3 pos;


                        if (scoredSpawnPoint.courseNode == null || scoredSpawnPoint.courseNode.m_area == null)
                        {
                            __instance.StopEvent();
                            return false;
                        }

                        pos = scoredSpawnPoint.courseNode.GetRandomPositionInside(); // modified line
                        spawnType = Enemies.eEnemyGroupSpawnType.RandomInArea;
                        Enemies.EnemyGroup.Spawn(pos, rot, scoredSpawnPoint.courseNode, Enemies.EnemyGroupType.Survival, spawnType, 0U, 0.0f, __instance.Replicator, __instance);
                    }

                    return false;

                default: return true;
            }
        }
    
        static Patch_OverwriteSurvivalWaveSpawnPosition_Hardcoded()
        {
            LevelAPI.OnBuildDone += OnBuildDone;
        }
    }
}
