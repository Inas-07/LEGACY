
////using Il2CppSystem.Collections.Generic;
//using HarmonyLib;
//using GameData;
//using ChainedPuzzles;
//using LevelGeneration;
//using System.Collections.Generic;
//using UnityEngine;
//using AIGraph;
//using SNetwork;
//using Player;
//using Il2CppInterop.Runtime.InteropTypes.Arrays;

//namespace LEGACY.Patch
//{
//    [HarmonyPatch]
//    internal class Patch_UseStaticBioscanPoints_FixClusterScan
//    {
//        private static HashSet<System.IntPtr> static_bioscans = null;

//        private static Dictionary<System.IntPtr, List<Vector3>> ClusterStaticBioscanPoints = null;

//        [HarmonyPrefix]
//        [HarmonyPatch(typeof(ChainedPuzzleInstance), nameof(ChainedPuzzleInstance.Setup))]
//        private static bool Pre_ChainedPuzzleInstance_Setup(ChainedPuzzleInstance __instance,
//            ChainedPuzzleDataBlock data, LG_Area sourceArea, Vector3 sourcePos, Transform parent, LG_Area targetArea,
//            bool overrideUseStaticBioscanPoints)
//        {
//            // not using static bioscan points
//            List<Vector3> staticpos = null;

//            if (overrideUseStaticBioscanPoints == true
//                || (targetArea != null && targetArea.m_courseNode.m_zone.m_settings.m_zoneData.UseStaticBioscanPointsInZone))
//            {
//                if (sourceArea.m_bioscanSpawnPoints.Count <= 0) return true;

//                if (TryGetNodePositionsFromTransforms(sourceArea.m_bioscanSpawnPoints, sourceArea, out staticpos) == false)
//                {
//                    Utilities.Logger.Error("No static bioscan point transform fouund in zone {0}, falling back to vanilla impl.", sourceArea.m_zone.Alias);
//                    return true;
//                }
//            }


//            __instance.Data = data;
//            __instance.m_sourceArea = sourceArea;
//            __instance.m_parent = parent;
//            int count = data.ChainedPuzzle.Count;
//            __instance.m_chainedPuzzleCores = new iChainedPuzzleCore[count];
//            __instance.m_sound = new CellSoundPlayer(__instance.m_parent.position);
//            __instance.m_puzzleUID = "CPUID_" + ChainedPuzzleInstance.m_uidCounter;
//            ++ChainedPuzzleInstance.m_uidCounter;
//            __instance.m_stateReplicator = SNet_StateReplicator<pChainedPuzzleState, pChainedPuzzleInteraction>.Create(new iSNet_StateReplicatorProvider<pChainedPuzzleState, pChainedPuzzleInteraction>(__instance.Pointer), eSNetReplicatorLifeTime.DestroyedOnLevelReset);

//            if (staticpos == null || staticpos.Count <= 0) return true;

//            int staticpos_index = 0;

//            for (int i = 0; i < data.ChainedPuzzle.Count; ++i)
//            {
//                Vector3 cur_pos_single = staticpos[staticpos_index];
//                Vector3 prevPuzzlePos = i <= 1 ? sourcePos : (staticpos_index > 0 ? staticpos[staticpos_index - 1] : staticpos[staticpos.Count - 1]);

//                bool revealWithHoloPath = true;
//                if (i == 0 && (double)data.WantedDistanceFromStartPos <= 0.0)
//                {
//                    cur_pos_single = sourcePos;
//                    revealWithHoloPath = false;
//                }
//                else if (i == 1 && (double)data.WantedDistanceFromStartPos <= 0.0)
//                    prevPuzzlePos = sourcePos;


//                GameObject componentPrefabForType = ChainedPuzzleManager.GetPuzzleComponentPrefabForType(data.ChainedPuzzle[i].PuzzleType);
//                if (componentPrefabForType != null)
//                {
//                    GameObject gameObject = GOUtil.SpawnChild(componentPrefabForType, cur_pos_single, Quaternion.identity, parent);
//                    __instance.m_chainedPuzzleCores[i] = gameObject.GetComponent<iChainedPuzzleCore>();

//                    // determine if it is cluster scan
//                    CP_Cluster_Core cluster_cp = __instance.m_chainedPuzzleCores[i].TryCast<CP_Cluster_Core>();


//                    // Basic scan or movable scan. 
//                    // use 1 static pos - vanilla impl.
//                    if (cluster_cp == null)
//                    {
//                        // handle movable scan
//                        bool isMovableScan = IsMovableScan(data.ChainedPuzzle[i].PuzzleType);

//                        __instance.m_chainedPuzzleCores[i].Setup(
//                            i, new iChainedPuzzleOwner(__instance.Pointer), sourceArea,
//                            // if is movable scan, don't reveal holopath
//                            // TODO: start from here
//                            isMovableScan ? false : revealWithHoloPath,
//                            prevPuzzlePos, hasAlarm: data.TriggerAlarmOnActivate, useRandomPositions: data.UseRandomPositions, onlyShowHudWhenPlayerIsClose: data.OnlyShowHUDWhenPlayerIsClose, parentGUID: __instance.m_puzzleUID);

//                        __instance.m_chainedPuzzleCores[i].add_OnPuzzleDone(new System.Action<int>(__instance.OnPuzzleDone));

//                        // 这行报错了：Type name too long。
//                        __instance.m_chainedPuzzleCores[i].add_Master_OnScanStateChanged(
//                            (Il2CppSystem.Action<float, Il2CppSystem.Collections.Generic.List<PlayerAgent>, int, Il2CppStructArray<bool>>)__instance.Master_OnPlayerScanChanged);
//                        if (__instance.m_chainedPuzzleCores[i].IsMovable)
//                            __instance.SetupMovement(gameObject, sourceArea); // 能work，但是会有spline，并且spline会跟着平移

//                        staticpos_index = (staticpos_index + 1) % staticpos.Count;
//                    }

//                    else // 
//                    {
//                        List<Vector3> cur_pos_list = new();

//                        while (cur_pos_list.Count < cluster_cp.m_amountOfPuzzles)
//                        {
//                            cur_pos_list.Add(staticpos[staticpos_index]);
//                            staticpos_index = (staticpos_index + 1) % staticpos.Count;
//                        }

//                        if (ClusterStaticBioscanPoints == null)
//                            ClusterStaticBioscanPoints = new();

//                        ClusterStaticBioscanPoints.Add(cluster_cp.Pointer, cur_pos_list);

//                        // Do normal setup. Cluster static bioscan points would be set in CP_Cluster_Core.Setup(), by finding static pos in `ClusterStaticBioscanPoints`.
//                        __instance.m_chainedPuzzleCores[i].Setup(i, new iChainedPuzzleOwner(__instance.Pointer), sourceArea, revealWithHoloPath, prevPuzzlePos, hasAlarm: data.TriggerAlarmOnActivate, useRandomPositions: data.UseRandomPositions, onlyShowHudWhenPlayerIsClose: data.OnlyShowHUDWhenPlayerIsClose, parentGUID: __instance.m_puzzleUID);
//                        __instance.m_chainedPuzzleCores[i].add_OnPuzzleDone(new System.Action<int>(__instance.OnPuzzleDone));
//                        __instance.m_chainedPuzzleCores[i].add_Master_OnScanStateChanged((Il2CppSystem.Action<float, Il2CppSystem.Collections.Generic.List<PlayerAgent>, int, Il2CppStructArray<bool>>)__instance.Master_OnPlayerScanChanged);
//                    }
//                }
//                else
//                {
//                    Debug.LogError("ChainedPuzzleInstance got a NULL prefab for component type: " + data.ChainedPuzzle[i].PuzzleType);
//                    break;
//                }
//            }

//            return false;
//        }

//        [HarmonyPrefix]
//        [HarmonyPatch(typeof(CP_Cluster_Core), nameof(CP_Cluster_Core.Setup))]
//        private static bool Pre_CP_Cluster_Core_Setup(
//            // params that i care about
//            CP_Cluster_Core __instance, int puzzleIndex, iChainedPuzzleOwner owner, LG_Area sourceArea, Vector3 prevPuzzlePos,
//            // params that i dont care
//            bool revealWithHoloPath, iChainedPuzzleHUD replacementHUD, bool hasAlarm, bool useRandomPosition, bool onlyShowHUDWhenPlayerIsClose, string parentGUID)
//        {
//            // Not static cluster scan. 
//            if (ClusterStaticBioscanPoints == null || !ClusterStaticBioscanPoints.ContainsKey(__instance.Pointer)) return true;

//            ChainedPuzzleInstance cp_instance = new ChainedPuzzleInstance(owner.Pointer);

//            List<Vector3> static_scanpoints = null;
//            if (ClusterStaticBioscanPoints.TryGetValue(__instance.Pointer, out static_scanpoints) == false)
//            {
//                Utilities.Logger.Error("WTF?");
//                return true;
//            }

//            if (static_scanpoints.Count != __instance.m_amountOfPuzzles)
//            {
//                Utilities.Logger.Error("Found static bioscan points in the dictionary but vector3List.Count != __instance.m_amountOfPuzzles, WTF?");
//                return true;
//            }

//            __instance.m_puzzleIndex = puzzleIndex;
//            revealWithHoloPath = false;

//            __instance.m_revealWithHoloPath = revealWithHoloPath;
//            __instance.m_spline = GOUtil.GetInterfaceFromComp<iChainedPuzzleHolopathSpline>(__instance.m_splineComp);
//            __instance.m_parentGUID = parentGUID;
//            if (__instance.m_revealWithHoloPath)
//            {
//                __instance.m_spline.Setup(hasAlarm);
//                __instance.m_spline.add_OnRevealDone(new System.Action(__instance.OnSplineRevealDone));
//                __instance.m_spline.GeneratePath(prevPuzzlePos, __instance.transform.position);
//            }

//            __instance.m_sync = GOUtil.GetInterfaceFromComp<iChainedPuzzleClusterSync>(__instance.m_syncComp);
//            __instance.m_sync.Setup();
//            __instance.m_sync.add_OnSyncStateChange(new System.Action<eClusterStatus, float, bool>(__instance.OnSyncStateChange));
//            __instance.m_hud = GOUtil.GetInterfaceFromComp<iChainedPuzzleHUD>(__instance.m_HUDComp);
//            __instance.m_hud.Setup(puzzleIndex, hasAlarm);
//            GOUtil.GetInterfaceFromComp<iChainedPuzzleClusterHUD>(__instance.m_HUDComp).SetupClusterHUD(__instance.m_puzzleIndex, __instance.m_HUDType, __instance.m_amountOfPuzzles, hasAlarm);

//            __instance.m_childCores = new iChainedPuzzleCore[__instance.m_amountOfPuzzles];

//            for (int i = 0; i < __instance.m_amountOfPuzzles; ++i)
//            {
//                __instance.m_childCores[i] = GOUtil.SpawnChildAndGetComp<iChainedPuzzleCore>(__instance.m_childPuzzlePrefab, static_scanpoints[i], Quaternion.identity, __instance.transform);
//                __instance.m_childCores[i].Setup(i, new iChainedPuzzleOwner(__instance.Pointer), sourceArea, prevPuzzlePos: prevPuzzlePos, replacementHUD: __instance.m_hud, hasAlarm: hasAlarm, parentGUID: parentGUID);
//                __instance.m_childCores[i].add_OnPuzzleDone(new System.Action<int>(__instance.OnChildPuzzleDone));
//            }

//            Utilities.Logger.Warning("Overwritten scan cluster (index: {0}) in zone {1}", puzzleIndex, sourceArea.m_zone.Alias);

//            return false;
//        }

//        // re-implemented method. function identically as those in vanilla
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



//        /**
//         * return System.Collections.Generic.List of ALL node pos.
//         */
//        private static bool TryGetNodePositionsFromTransforms(Il2CppSystem.Collections.Generic.List<Transform> transforms, LG_Area inArea, out System.Collections.Generic.List<Vector3> nodePositions)
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

//            nodePositions = new();
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

//        // all puzzle id with name containing 'moving'
//        private static readonly HashSet<uint> MovableScanID = new() { 21u, 24u, 22u, 31u, 42u, 38u };

//        /**
//         * IsMovable is available only after call to Setup(). But I need to know that before Setup()
//         */
//        private static bool IsMovableScan(uint puzzleType) => MovableScanID.Contains(puzzleType);

//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(GS_AfterLevel), nameof(GS_AfterLevel.CleanupAfterExpedition))]
//        private static void Post_CleanupAfterExpedition()
//        {
//            if (ClusterStaticBioscanPoints != null)
//            {
//                ClusterStaticBioscanPoints.Clear();
//                ClusterStaticBioscanPoints = null;
//            }

//            if (static_bioscans != null)
//            {
//                static_bioscans.Clear();
//                static_bioscans = null;
//            }
//        }
//    }
//}