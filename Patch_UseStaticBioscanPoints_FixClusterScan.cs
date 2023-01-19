using HarmonyLib;
using ChainedPuzzles;
using LevelGeneration;
using System.Collections.Generic;
using UnityEngine;
using AIGraph;
using System;
using GameData;
using GTFO.API;
namespace LEGACY.Patch
{
    [HarmonyPatch]
    internal class Patch_UseStaticBioscanPoints_FixClusterScan
    {
        private static Dictionary<IntPtr, List<Vector3>> static_cp = null;

        // if data.WantDistanceFromStartPos <= 0 && first scan is single scan, then use the sec-door transform as the first single scan position,
        // therefore need to offset static_pos_index by 1.
        private static HashSet<IntPtr> offset_1_scan = null;

        private static bool TryGetNodes(LG_Area area, bool onlyReachable, out List<AIG_INode> nodes)
        {
            if (area.m_courseNode == null || !area.m_courseNode.IsValid)
            {
                nodes = null;
                return false;
            }
            if (onlyReachable)
            {
                if (area.m_courseNode.m_nodeCluster.m_reachableNodes.Count > 1)
                {
                    nodes = Utilities.Utils.cast(area.m_courseNode.m_nodeCluster.m_reachableNodes);
                }
                else
                {
                    nodes = Utilities.Utils.cast(area.m_courseNode.m_nodeCluster.m_nodes);
                    Debug.LogError("ERROR : No reachable nodes available in " + area.transform.parent.name + "/" + area.name + "  nodes:" + area.m_courseNode.m_nodeCluster.m_nodes.Count);
                }
                return true;
            }
            nodes = Utilities.Utils.cast(area.m_courseNode.m_nodeCluster.m_nodes);
            return true;
        }

        private static AIG_INode GetClosestNode(List<AIG_INode> nodes, Vector3 pos)
        {
            if (nodes == null || nodes.Count <= 0)
                return null;
            AIG_INode node = nodes[0];
            float num = float.MaxValue;
            for (int index = 1; index < nodes.Count; ++index)
            {
                float sqrMagnitude = (nodes[index].Position - pos).sqrMagnitude;
                if ((double)sqrMagnitude < (double)num)
                {
                    node = nodes[index];
                    num = sqrMagnitude;
                }
            }
            return node;
        }

        private static bool TryGetNodePositionsFromTransforms(Il2CppSystem.Collections.Generic.List<Transform> transforms, LG_Area inArea, out List<Vector3> nodePositions)
        {
            if (transforms == null || transforms.Count <= 0)
            {
                nodePositions = null;
                return false;
            }
            List<AIG_INode> nodes;

            if (!TryGetNodes(inArea, true, out nodes))
            {
                nodePositions = null;
                return false;
            }
            nodePositions = new List<Vector3>();
            for (int index = 0; index < transforms.Count; ++index)
            {
                AIG_INode closestNode = GetClosestNode(nodes, transforms[index].position);
                if (closestNode != null)
                {
                    nodePositions.Add(closestNode.Position);
                }
            }

            if (inArea.m_geomorph.m_geoPrefab.name == "geo_64x64_mining_refinery_I_HA_01_v2") // remove that stupid duplicate scan point.
            {
                nodePositions.RemoveAt(0);
            }

            return true;
        }

        /**
         Would return 0 if Data.WantedDistanceFromStartPos <= 0.0, which means that first scan position uses sec-door transform
         */
        private static int ScanCount(ChainedPuzzleInstance owner_cp, int currentPuzzleIndex)
        {
            int count = 0;
            int bound = currentPuzzleIndex < owner_cp.m_chainedPuzzleCores.Count ? currentPuzzleIndex : owner_cp.m_chainedPuzzleCores.Count;
            for (int i = 0; i < bound; i++)
            {
                CP_Cluster_Core cluster_cp = owner_cp.m_chainedPuzzleCores[i].TryCast<CP_Cluster_Core>();
                if (cluster_cp == null) count += 1;
                else
                {
                    count += cluster_cp.m_amountOfPuzzles;
                }
            }

            return count;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChainedPuzzleInstance), nameof(ChainedPuzzleInstance.Setup))]
        private static void Pre_ChainedPuzzleInstance_Setup(ChainedPuzzleInstance __instance,
            ChainedPuzzleDataBlock data, LG_Area sourceArea, Vector3 sourcePos, Transform parent, LG_Area targetArea,
            bool overrideUseStaticBioscanPoints)
        {
            if (overrideUseStaticBioscanPoints == false
                && (targetArea == null || targetArea.m_courseNode.m_zone.m_settings.m_zoneData.UseStaticBioscanPointsInZone == false)) return;
            if (sourceArea.m_bioscanSpawnPoints.Count <= 0) return;

            if (static_cp == null) static_cp = new();

            List<Vector3> static_pos = null;

            if (TryGetNodePositionsFromTransforms(sourceArea.m_bioscanSpawnPoints, sourceArea, out static_pos) == false)
            {
                Utilities.Logger.Error("Cannot get static bioscan points in zone {0}, falling back to vanilla impl.", sourceArea.m_zone.Alias);
                return;
            }

            static_cp.Add(__instance.Pointer, static_pos);

            Utilities.Logger.Debug("Overwriting static bioscan in Zone {0}. Alarm name: {1}. ", sourceArea.m_zone.Alias, data.PublicAlarmName);
            Utilities.Logger.Debug("Geoname: {0}, Found {1} bioscan point(s).", sourceArea.m_geomorph.m_geoPrefab.name, static_pos.Count);
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(CP_Bioscan_Core), nameof(CP_Bioscan_Core.Setup))]
        private static void Pre_CP_Bioscan_Core_Setup(CP_Bioscan_Core __instance, int puzzleIndex, iChainedPuzzleOwner owner,
            LG_Area sourceArea, bool revealWithHoloPath, ref Vector3 prevPuzzlePos,
            iChainedPuzzleHUD replacementHUD, bool hasAlarm, bool useRandomPositions, bool onlyShowHUDWhenPlayerIsClose, string parentGUID)
        {
            if (static_cp == null || !static_cp.ContainsKey(owner.Pointer)) return;

            List<Vector3> static_pos = null;

            if (static_cp.TryGetValue(owner.Pointer, out static_pos) == false)
            {
                Utilities.Logger.Error("Registered static scan but didn't find its checked static biocan points, WTF?");
                Utilities.Logger.Error("Falling back to vanilla impl.");
                return;
            }

            int static_pos_idx = 0;

            CP_Cluster_Core cluster_owner = owner.TryCast<CP_Cluster_Core>();
            ChainedPuzzleInstance cp_instance = null;


            if (cluster_owner == null) // single scan 
            {
                cp_instance = new ChainedPuzzleInstance(owner.Pointer);

                if (puzzleIndex == 0 && cp_instance.Data.WantedDistanceFromStartPos <= 0.0f)
                {
                    // first scan use sec-door transform
                    // use vanilla impl.
                    // need to offset static_pos_idx by 1 afterwards.

                    if (offset_1_scan == null) offset_1_scan = new();
                    offset_1_scan.Add(owner.Pointer);

                    return;
                }

                static_pos_idx = ScanCount(cp_instance, puzzleIndex);

                if (offset_1_scan != null && offset_1_scan.Contains(owner.Pointer))
                {
                    static_pos_idx -= 1;
                }

                static_pos_idx %= static_pos.Count;

                if (puzzleIndex > 0)
                {
                    if (puzzleIndex == 1 && offset_1_scan != null && offset_1_scan.Contains(owner.Pointer))
                    {
                        // use sec-door transform, do nothing
                    }
                    else
                    {
                        prevPuzzlePos = static_pos_idx > 0 ? static_pos[static_pos_idx - 1] : static_pos[static_pos.Count - 1];
                    }
                }
            }


            else // This scan belongs to a cluster scan, do nothing
            {
                //cp_instance = new ChainedPuzzleInstance(cluster_owner.m_owner.Pointer);

                //static_pos_idx = ScanCount(cp_instance, cluster_owner.m_puzzleIndex) + puzzleIndex;

                //if(offset_1_scan != null && offset_1_scan.Contains(cluster_owner.m_owner.Pointer))
                //{
                //    static_pos_idx -= 1;   
                //}

                //static_pos_idx %= static_pos.Count;

                //if (cluster_owner.m_puzzleIndex > 0)
                //{
                //    prevPuzzlePos = static_pos_idx > 0 ? static_pos[static_pos_idx - 1] : static_pos[static_pos.Count - 1];
                //}
            }

            __instance.transform.SetPositionAndRotation(static_pos[static_pos_idx], __instance.transform.rotation);
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(CP_Cluster_Core), nameof(CP_Cluster_Core.Setup))]
        private static bool Pre_CP_Cluster_Core_Setup(
            // params that i care about
            CP_Cluster_Core __instance, int puzzleIndex, iChainedPuzzleOwner owner, LG_Area sourceArea, Vector3 prevPuzzlePos,
            // params that i dont care
            bool revealWithHoloPath, iChainedPuzzleHUD replacementHUD, bool hasAlarm, bool useRandomPosition, bool onlyShowHUDWhenPlayerIsClose, string parentGUID)
        {
            if (static_cp == null || !static_cp.ContainsKey(owner.Pointer)) return true;

            List<Vector3> static_pos = new();

            if (static_cp.TryGetValue(owner.Pointer, out static_pos) == false)
            {
                Utilities.Logger.Error("Registered static scan but didn't find its checked static biocan points, WTF?");
                Utilities.Logger.Error("Falling back to vanilla impl.");
                return true;
            }

            ChainedPuzzleInstance cp_instance = new ChainedPuzzleInstance(owner.Pointer);
            int static_pos_idx = ScanCount(cp_instance, puzzleIndex);

            if (offset_1_scan != null && offset_1_scan.Contains(owner.Pointer))
            {
                static_pos_idx -= 1;
            }

            static_pos_idx %= static_pos.Count;

            // modify prevPuzzlePos
            if (puzzleIndex > 0)
            {
                if (puzzleIndex == 1 && offset_1_scan != null && offset_1_scan.Contains(owner.Pointer))
                {
                    // use sec-door transform, do nothing
                }
                else
                {
                    prevPuzzlePos = static_pos_idx > 0 ? static_pos[static_pos_idx - 1] : static_pos[static_pos.Count - 1];
                }
            }

            __instance.m_puzzleIndex = puzzleIndex;
            revealWithHoloPath = false;

            __instance.m_revealWithHoloPath = revealWithHoloPath;
            __instance.m_spline = GOUtil.GetInterfaceFromComp<iChainedPuzzleHolopathSpline>(__instance.m_splineComp);
            __instance.m_parentGUID = parentGUID;
            if (__instance.m_revealWithHoloPath)
            {
                __instance.m_spline.Setup(hasAlarm);
                __instance.m_spline.add_OnRevealDone(new Action(__instance.OnSplineRevealDone));
                __instance.m_spline.GeneratePath(prevPuzzlePos, __instance.transform.position);
            }

            __instance.m_sync = GOUtil.GetInterfaceFromComp<iChainedPuzzleClusterSync>(__instance.m_syncComp);
            __instance.m_sync.Setup();
            __instance.m_sync.add_OnSyncStateChange(new Action<eClusterStatus, float, bool>(__instance.OnSyncStateChange));
            __instance.m_hud = GOUtil.GetInterfaceFromComp<iChainedPuzzleHUD>(__instance.m_HUDComp);
            __instance.m_hud.Setup(puzzleIndex, hasAlarm);
            GOUtil.GetInterfaceFromComp<iChainedPuzzleClusterHUD>(__instance.m_HUDComp).SetupClusterHUD(__instance.m_puzzleIndex, __instance.m_HUDType, __instance.m_amountOfPuzzles, hasAlarm);

            //Vector3 position = __instance.transform.position;

            __instance.m_childCores = new iChainedPuzzleCore[__instance.m_amountOfPuzzles];

            for (int i = 0; i < __instance.m_amountOfPuzzles; ++i)
            {
                __instance.m_childCores[i] = GOUtil.SpawnChildAndGetComp<iChainedPuzzleCore>(__instance.m_childPuzzlePrefab, static_pos[static_pos_idx], Quaternion.identity, __instance.transform);
                __instance.m_childCores[i].Setup(i, new iChainedPuzzleOwner(__instance.Pointer), sourceArea, prevPuzzlePos: prevPuzzlePos, replacementHUD: __instance.m_hud, hasAlarm: hasAlarm, parentGUID: parentGUID);
                __instance.m_childCores[i].add_OnPuzzleDone(new Action<int>(__instance.OnChildPuzzleDone));

                static_pos_idx = (static_pos_idx + 1) % static_pos.Count;
            }

            return false;
        }

        private static void CleanupAfterExpedition()
        {
            if (static_cp != null)
            {
                static_cp.Clear();
                static_cp = null;
            }

            offset_1_scan = null;
            Utilities.Logger.Warning("clean Patch_UseStaticBioscanPoints_FixClusterScan");
        }

        static Patch_UseStaticBioscanPoints_FixClusterScan()
        {
            LevelAPI.OnLevelCleanup += CleanupAfterExpedition;
        }
    }
}