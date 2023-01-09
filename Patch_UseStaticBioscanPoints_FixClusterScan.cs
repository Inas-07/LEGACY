//using HarmonyLib;
//using ChainedPuzzles;
//using LevelGeneration;
//using System.Collections.Generic;
//using UnityEngine;
//using AIGraph;
//using System;
//using GameData;
//using System.Reflection.Metadata.Ecma335;
//using static IRF.InstancedRenderFeatureRenderer;

//namespace LEGACY.Patch
//{
//    [HarmonyPatch]
//    internal class Patch_UseStaticBioscanPoints_FixClusterScan
//    {
//        private static HashSet<IntPtr> static_cp = null;

//        private static bool TryGetNodes(LG_Area area, bool onlyReachable, out List<AIG_INode> nodes)
//        {
//            if (area.m_courseNode == null || !area.m_courseNode.IsValid)
//            {
//                nodes = null;
//                return false;
//            }
//            if (onlyReachable)
//            {
//                if (area.m_courseNode.m_nodeCluster.m_reachableNodes.Count > 1)
//                {
//                    nodes = Utilities.Utils.cast(area.m_courseNode.m_nodeCluster.m_reachableNodes);
//                }
//                else
//                {
//                    nodes = Utilities.Utils.cast(area.m_courseNode.m_nodeCluster.m_nodes);
//                    Debug.LogError("ERROR : No reachable nodes available in " + area.transform.parent.name + "/" + area.name + "  nodes:" + area.m_courseNode.m_nodeCluster.m_nodes.Count);
//                }
//                return true;
//            }
//            nodes = Utilities.Utils.cast(area.m_courseNode.m_nodeCluster.m_nodes);
//            return true;
//        }

//        private static AIG_INode GetClosestNode(List<AIG_INode> nodes, Vector3 pos)
//        {
//            if (nodes == null || nodes.Count <= 0)
//                return null;
//            AIG_INode node = nodes[0];
//            float num = float.MaxValue;
//            for (int index = 1; index < nodes.Count; ++index)
//            {
//                float sqrMagnitude = (nodes[index].Position - pos).sqrMagnitude;
//                if ((double)sqrMagnitude < (double)num)
//                {
//                    node = nodes[index];
//                    num = sqrMagnitude;
//                }
//            }
//            return node;
//        }

//        private static bool TryGetNodePositionsFromTransforms(Il2CppSystem.Collections.Generic.List<Transform> transforms, LG_Area inArea, out List<Vector3> nodePositions)
//        {
//            if (transforms == null || transforms.Count <= 0)
//            {
//                nodePositions = null;
//                return false;
//            }
//            List<AIG_INode> nodes;

//            if (!TryGetNodes(inArea, true, out nodes))
//            {
//                nodePositions = null;
//                return false;
//            }
//            nodePositions = new List<Vector3>();
//            for (int index = 0; index < transforms.Count; ++index)
//            {
//                AIG_INode closestNode = GetClosestNode(nodes, transforms[index].position);
//                if (closestNode != null)
//                {
//                    nodePositions.Add(closestNode.Position);
//                }
//            }
//            return true;
//        }

//        private static int ScanCount(ChainedPuzzleInstance owner_cp, int currentPuzzleIndex)
//        {
//            int count = 0;
//            int bound = currentPuzzleIndex < owner_cp.m_chainedPuzzleCores.Count ? currentPuzzleIndex : owner_cp.m_chainedPuzzleCores.Count;
//            for (int i = 0; i < bound; i++)
//            {
//                CP_Cluster_Core cluster_cp = owner_cp.m_chainedPuzzleCores[i].TryCast<CP_Cluster_Core>();
//                if (cluster_cp == null) count += 1;
//                else
//                {
//                    count += cluster_cp.m_amountOfPuzzles;
//                }

//            }

//            //Utilities.Logger.Warning("{0}", count);
//            return count;
//        }

//        [HarmonyPrefix]
//        [HarmonyPatch(typeof(ChainedPuzzleInstance), nameof(ChainedPuzzleInstance.Setup))]
//        private static void Pre_ChainedPuzzleInstance_Setup(ChainedPuzzleInstance __instance,
//            ChainedPuzzleDataBlock data, LG_Area sourceArea, Vector3 sourcePos, Transform parent, LG_Area targetArea,
//            bool overrideUseStaticBioscanPoints)
//        {
//            if (overrideUseStaticBioscanPoints == false
//                && (targetArea == null || targetArea.m_courseNode.m_zone.m_settings.m_zoneData.UseStaticBioscanPointsInZone == false)) return;
//            if (sourceArea.m_bioscanSpawnPoints.Count <= 0) return;

//            if (static_cp == null) static_cp = new();
//            static_cp.Add(__instance.Pointer);

//            Utilities.Logger.Debug("Overwriting static bioscan in Zone {0}. Alarm name: {2}. Found {1} bioscan point(s).", sourceArea.m_zone.Alias, sourceArea.m_bioscanSpawnPoints.Count, data.PublicAlarmName);
//        }


//        [HarmonyPrefix]
//        [HarmonyPatch(typeof(CP_Bioscan_Core), nameof(CP_Bioscan_Core.Setup))]
//        private static void Pre_CP_Bioscan_Core_Setup(CP_Bioscan_Core __instance, int puzzleIndex, iChainedPuzzleOwner owner,
//            LG_Area sourceArea, bool revealWithHoloPath, ref Vector3 prevPuzzlePos,
//            iChainedPuzzleHUD replacementHUD, bool hasAlarm, bool useRandomPositions, bool onlyShowHUDWhenPlayerIsClose, string parentGUID)
//        {
//            if (static_cp == null || !static_cp.Contains(owner.Pointer)) return;

//            List<Vector3> static_pos = null;

//            CP_Cluster_Core cluster_owner = owner.TryCast<CP_Cluster_Core>();
//            if (cluster_owner == null) // single scan 
//            {
//                if (TryGetNodePositionsFromTransforms(sourceArea.m_bioscanSpawnPoints, sourceArea, out static_pos) == false)
//                {
//                    Utilities.Logger.Error("Cannot get static bioscan points in zone {0}, falling back to vanilla impl.", sourceArea.m_zone.Alias);
//                    return;
//                }

//                ChainedPuzzleInstance cp_instance = new ChainedPuzzleInstance(owner.Pointer);
//                int static_pos_idx = ScanCount(cp_instance, puzzleIndex) % static_pos.Count;

//                if (puzzleIndex > 0)
//                {
//                    prevPuzzlePos = static_pos_idx > 0 ? static_pos[static_pos_idx - 1] : static_pos[static_pos.Count - 1];
//                }

//                __instance.transform.SetPositionAndRotation(static_pos[static_pos_idx], __instance.transform.rotation);
//            }
//            //else // This scan belongs to a cluster scan. 
//            //{
//            //    return;
//            //}            
//        }


//        [HarmonyPrefix]
//        [HarmonyPatch(typeof(CP_Cluster_Core), nameof(CP_Cluster_Core.Setup))]
//        private static bool Pre_CP_Cluster_Core_Setup(
//            // params that i care about
//            CP_Cluster_Core __instance, int puzzleIndex, iChainedPuzzleOwner owner, LG_Area sourceArea, Vector3 prevPuzzlePos,
//            // params that i dont care
//            bool revealWithHoloPath, iChainedPuzzleHUD replacementHUD, bool hasAlarm, bool useRandomPosition, bool onlyShowHUDWhenPlayerIsClose, string parentGUID)
//        {
//            if (static_cp == null || !static_cp.Contains(owner.Pointer)) return true;

//            ChainedPuzzleInstance cp_instance = new ChainedPuzzleInstance(owner.Pointer);

//            List<Vector3> static_pos = new();

//            if (TryGetNodePositionsFromTransforms(sourceArea.m_bioscanSpawnPoints, sourceArea, out static_pos) == false)
//            {
//                Utilities.Logger.Error("Cannot get static bioscan points in zone {0}, falling back to vanilla impl.", sourceArea.m_zone.Alias);
//                return true;
//            }

//            int static_pos_idx = ScanCount(cp_instance, puzzleIndex) % static_pos.Count;
//            if (puzzleIndex > 0)
//            {
//                prevPuzzlePos = static_pos_idx > 0 ? static_pos[static_pos_idx - 1] : static_pos[static_pos.Count - 1];
//            }

//            __instance.m_puzzleIndex = puzzleIndex;
//            revealWithHoloPath = false;

//            __instance.m_revealWithHoloPath = revealWithHoloPath;
//            __instance.m_spline = GOUtil.GetInterfaceFromComp<iChainedPuzzleHolopathSpline>(__instance.m_splineComp);
//            __instance.m_parentGUID = parentGUID;
//            if (__instance.m_revealWithHoloPath)
//            {
//                __instance.m_spline.Setup(hasAlarm);
//                __instance.m_spline.add_OnRevealDone(new Action(__instance.OnSplineRevealDone));
//                __instance.m_spline.GeneratePath(prevPuzzlePos, __instance.transform.position);
//            }

//            __instance.m_sync = GOUtil.GetInterfaceFromComp<iChainedPuzzleClusterSync>(__instance.m_syncComp);
//            __instance.m_sync.Setup();
//            __instance.m_sync.add_OnSyncStateChange(new Action<eClusterStatus, float, bool>(__instance.OnSyncStateChange));
//            __instance.m_hud = GOUtil.GetInterfaceFromComp<iChainedPuzzleHUD>(__instance.m_HUDComp);
//            __instance.m_hud.Setup(puzzleIndex, hasAlarm);
//            GOUtil.GetInterfaceFromComp<iChainedPuzzleClusterHUD>(__instance.m_HUDComp).SetupClusterHUD(__instance.m_puzzleIndex, __instance.m_HUDType, __instance.m_amountOfPuzzles, hasAlarm);

//            //Vector3 position = __instance.transform.position;

//            __instance.m_childCores = new iChainedPuzzleCore[__instance.m_amountOfPuzzles];

//            for (int i = 0; i < __instance.m_amountOfPuzzles; ++i)
//            {
//                __instance.m_childCores[i] = GOUtil.SpawnChildAndGetComp<iChainedPuzzleCore>(__instance.m_childPuzzlePrefab, static_pos[static_pos_idx], Quaternion.identity, __instance.transform);
//                __instance.m_childCores[i].Setup(i, new iChainedPuzzleOwner(__instance.Pointer), sourceArea, prevPuzzlePos: prevPuzzlePos, replacementHUD: __instance.m_hud, hasAlarm: hasAlarm, parentGUID: parentGUID);
//                __instance.m_childCores[i].add_OnPuzzleDone(new Action<int>(__instance.OnChildPuzzleDone));

//                static_pos_idx = (static_pos_idx + 1) % static_pos.Count;
//            }

//            return false;
//        }

//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(GS_AfterLevel), nameof(GS_AfterLevel.CleanupAfterExpedition))]
//        private static void Post_CleanupAfterExpedition()
//        {
//            if (static_cp != null)
//            {
//                static_cp.Clear();
//                static_cp = null;
//            }
//        }
//    }
//}