using HarmonyLib;
using System.Collections.Generic;
using LevelGeneration;
using LEGACY.Utils;
using GameData;
using LEGACY.LegacyOverride.PowerGenerator.GeneratorCluster;
using UnityEngine;
using LEGACY.LegacyOverride.PowerGenerator.IndividualGenerator;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    class Patch_LG_PowerGeneratorCluster
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_PowerGeneratorCluster), nameof(LG_PowerGeneratorCluster.Setup))]
        private static void Post_PowerGeneratorCluster_Setup(LG_PowerGeneratorCluster __instance)
        {
            uint zoneInstanceIndex = GeneratorClusterOverrideManager.Current.Register(__instance);

            List<ZoneGeneratorCluster> levelPGCConfigs = GeneratorClusterOverrideManager.Current.GetLevelGeneratorClusterOverride(RundownManager.ActiveExpedition.LevelLayoutData);
            
            // modify position / rotation if config specified. 
            if (levelPGCConfigs == null || levelPGCConfigs.Count < 1) return;
            
            int i = levelPGCConfigs.FindIndex((zonePGConfig) =>
                __instance.SpawnNode.m_zone.LocalIndex == zonePGConfig.LocalIndex &&
                __instance.SpawnNode.m_zone.Layer.m_type == zonePGConfig.LayerType &&
                __instance.SpawnNode.m_dimension.DimensionIndex == zonePGConfig.DimensionIndex
            );
            if (i == -1) return;
            ZoneGeneratorCluster zoneGCConfig = levelPGCConfigs[i];

            i = zoneGCConfig.GeneratorClustersInZone.FindIndex((config) => config.GeneratorClusterIndex == zoneInstanceIndex);
            if (i == -1) return;
            GeneratorCluster GeneratorClusterConfig = zoneGCConfig.GeneratorClustersInZone[i];

            if (WardenObjectiveManager.Current.m_activeWardenObjectives[__instance.SpawnNode.LayerType].Type == eWardenObjectiveType.CentralGeneratorCluster)
            {
                LegacyLogger.Error("Found built Warden Objective LG_PowerGeneratorCluster but there's also a config for it! Won't apply this config");
                LegacyLogger.Error($"Zone_{zoneGCConfig.LocalIndex}, {zoneGCConfig.LayerType}, {zoneGCConfig.DimensionIndex}");
                return;
            }

            LegacyLogger.Warning("Found LG_PowerGeneratorCluster and its config! Building this Generator cluster...");

            // ========== vanilla build =================
            __instance.m_serialNumber = SerialGenerator.GetUniqueSerialNo();
            __instance.m_itemKey = "GENERATOR_CLUSTER_" + __instance.m_serialNumber.ToString();
            __instance.m_terminalItem = GOUtil.GetInterfaceFromComp<iTerminalItem>(__instance.m_terminalItemComp);
            __instance.m_terminalItem.Setup(__instance.m_itemKey);
            __instance.m_terminalItem.FloorItemStatus = eFloorInventoryObjectStatus.UnPowered;
            if (__instance.SpawnNode != null)
                __instance.m_terminalItem.FloorItemLocation = __instance.SpawnNode.m_zone.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore);

            List<Transform> transformList = new List<Transform>(__instance.m_generatorAligns);
            uint numberOfGenerators = GeneratorClusterConfig.NumberOfGenerators;
            __instance.m_generators = new LG_PowerGenerator_Core[numberOfGenerators];

            if (transformList.Count >= numberOfGenerators)
            {
                for (int j = 0; j < numberOfGenerators; ++j)
                {
                    int k = Builder.BuildSeedRandom.Range(0, transformList.Count, "NO_TAG");
                    LG_PowerGenerator_Core generator = GOUtil.SpawnChildAndGetComp<LG_PowerGenerator_Core>(__instance.m_generatorPrefab, transformList[k]);
                    __instance.m_generators[j] = generator;

                    generator.SpawnNode = __instance.SpawnNode;
                    IndividualGeneratorOverrideManager.Current.MarkAsGCGenerator(generator);
                    generator.Setup();
                    generator.SetCanTakePowerCell(true);
                    generator.OnSyncStatusChanged += new System.Action<ePowerGeneratorStatus>(status =>
                    {
                        Debug.Log("LG_PowerGeneratorCluster.powerGenerator.OnSyncStatusChanged! status: " + status);
                        
                        if (status != ePowerGeneratorStatus.Powered) return;

                        uint poweredGenerators = 0u;

                        for (int m = 0; m < __instance.m_generators.Length; ++m)
                        {
                            if (__instance.m_generators[m].m_stateReplicator.State.status == ePowerGeneratorStatus.Powered) 
                                poweredGenerators++;
                        }

                        LegacyLogger.Log($"Generator Cluster PowerCell inserted ({poweredGenerators} / {__instance.m_generators.Count})");
                        var EventsOnInsertCell = GeneratorClusterConfig.EventsOnInsertCell;
                        
                        int eventsIndex = (int)(poweredGenerators - 1);
                        if(eventsIndex >= 0 && eventsIndex < EventsOnInsertCell.Count)
                        {
                            LegacyLogger.Log($"Executing events ({poweredGenerators} / {__instance.m_generators.Count}). Event count: {EventsOnInsertCell[eventsIndex].Count}");
                            EventsOnInsertCell[eventsIndex].ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));
                        }

                        if (poweredGenerators == __instance.m_generators.Count && !__instance.m_endSequenceTriggered)
                        {
                            LegacyLogger.Log("All generators powered, executing end sequence");
                            __instance.StartCoroutine(__instance.ObjectiveEndSequence());
                            __instance.m_endSequenceTriggered = true;
                        }
                    });
                    Debug.Log("Spawning generator at alignIndex: " + k);
                    transformList.RemoveAt(k);
                }
            }
            else
                Debug.LogError("LG_PowerGeneratorCluster does NOT have enough generator aligns to support the warden objective! Has " + transformList.Count + " needs " + numberOfGenerators);
            __instance.ObjectiveItemSolved = true;

            if(GeneratorClusterConfig.EndSequenceChainedPuzzle != 0u)
            {
                GeneratorClusterOverrideManager.Current.RegisterForChainedPuzzleBuild(__instance, GeneratorClusterConfig);
            }
        }
    }
}
