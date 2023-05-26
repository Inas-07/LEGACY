using GTFO.API.Utilities;
using LEGACY.Utils;
using System.IO;
using System.Collections.Generic;
using GTFO.API;
using LevelGeneration;
using GameData;

using ChainedPuzzles;
using UnityEngine;

namespace LEGACY.LegacyOverride.HSUActivators
{
    internal class HSUActivatorOverrideManager
    {
        public static readonly HSUActivatorOverrideManager Current;

        private Dictionary<uint, LevelHSUActivator> levelHSUActivators = new();

        private LiveEditListener LEListener;

        private static readonly string CONFIG_PATH = Path.Combine(LegacyOverrideManagers.LEGACY_CONFIG_PATH, "HSUActivator");

        private InstanceManager<LG_HSUActivator_Core> instanceManager = new(); // accessible within patch: LG_PowerGeneratorCluster.Setup 

        private Dictionary<(eDimensionIndex, LG_LayerType, eLocalZoneIndex, uint), ChainedPuzzleInstance> activationScans = new();

        private void AddOverride(LevelHSUActivator _override)
        {
            if (_override == null) return;

            // because we have chained puzzles, sorting is necessary to preserve chained puzzle instance order.
            _override.HSUActivators.Sort((u1, u2) =>
            {
                if (u1.DimensionIndex != u2.DimensionIndex) return (int)u1.DimensionIndex < (int)u2.DimensionIndex ? -1 : 1;
                if (u1.LayerType != u2.LayerType) return (int)u1.LayerType < (int)u2.LayerType ? -1 : 1;
                if (u1.LocalIndex != u2.LocalIndex) return (int)u1.LocalIndex < (int)u2.LocalIndex ? -1 : 1;
                if (u1.InstanceIndex != u2.InstanceIndex) return u1.InstanceIndex < u2.InstanceIndex ? -1 : 1;
                return 0;
            });

            if (levelHSUActivators.ContainsKey(_override.MainLevelLayout))
            {
                LegacyLogger.Warning("Replaced MainLevelLayout {0}", _override.MainLevelLayout);
                levelHSUActivators[_override.MainLevelLayout] = _override;
            }
            else
            {
                levelHSUActivators.Add(_override.MainLevelLayout, _override);
            }
        }

        public void Init()
        {
            if (!Directory.Exists(CONFIG_PATH))
            {
                Directory.CreateDirectory(CONFIG_PATH);

                var file = File.CreateText(Path.Combine(CONFIG_PATH, "Template.json"));
                file.WriteLine(Json.Serialize(new LevelHSUActivator()));
                file.Flush();
                file.Close();

                return;
            }

            foreach (string confFile in Directory.EnumerateFiles(CONFIG_PATH, "*.json", SearchOption.AllDirectories))
            {
                LevelHSUActivator conf;
                Json.Load(confFile, out conf);

                AddOverride(conf);
            }

            LEListener = LiveEdit.CreateListener(CONFIG_PATH, "*.json", true);
            LEListener.FileChanged += FileChanged;
        }

        private void FileChanged(LiveEditEventArgs e)
        {
            LegacyLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                LevelHSUActivator conf = Json.Deserialize<LevelHSUActivator>(content);
                AddOverride(conf);
            });
        }

        public uint Register(LG_HSUActivator_Core __instance)
        {
            uint instanceIndex = instanceManager.Register(
                __instance.SpawnNode.m_dimension.DimensionIndex,
                __instance.SpawnNode.LayerType,
                __instance.SpawnNode.m_zone.LocalIndex,
                __instance);

            return instanceIndex;
        }

        public uint GetIndex(LG_HSUActivator_Core instance) 
        {
            // TODO: improve InstanceManager and thereby support IntPtr search
            uint result = instanceManager.GetIndex(instance.SpawnNode.m_dimension.DimensionIndex, instance.SpawnNode.LayerType, instance.SpawnNode.m_zone.LocalIndex, instance);
            if (result != uint.MaxValue) return result;

            var instanceInZone = instanceManager.GetInstanceInZone(instance.SpawnNode.m_dimension.DimensionIndex, instance.SpawnNode.LayerType, instance.SpawnNode.m_zone.LocalIndex);
            for(int i = 0; i < instanceInZone.Count; i++)
            {
                var core = instanceInZone[i];
                if (core.Pointer == instance.Pointer)
                {
                    return (uint)i;
                }
            }

            return uint.MaxValue;
        }

        public HSUActivator GetOverride(eDimensionIndex dimensionIndex, LG_LayerType layerType, eLocalZoneIndex localIndex, uint instanceIndex)
        {
            if(!levelHSUActivators.ContainsKey(RundownManager.ActiveExpedition.LevelLayoutData)) return null;

            var levelConfig = levelHSUActivators[RundownManager.ActiveExpedition.LevelLayoutData];

            int i = levelConfig.HSUActivators.FindIndex(e => 
                e.DimensionIndex == dimensionIndex && 
                e.LayerType == layerType && 
                e.LocalIndex == localIndex &&
                e.InstanceIndex == instanceIndex
            );

            return i == -1 ? null : levelConfig.HSUActivators[i];
        }

        public ChainedPuzzleInstance GetActivationScan(eDimensionIndex dimensionIndex, LG_LayerType layerType, eLocalZoneIndex localIndex, uint instanceIndex) => activationScans.ContainsKey((dimensionIndex, layerType, localIndex, instanceIndex)) ? activationScans[(dimensionIndex, layerType, localIndex, instanceIndex)] : null;

        private void Build(HSUActivator config)
        {
            var instance = instanceManager.GetInstance(config.DimensionIndex, config.LayerType, config.LocalIndex, config.InstanceIndex);
            if (instance == null)
            {
                LegacyLogger.Error($"Found unused HSUActivator config: {(config.DimensionIndex, config.LayerType, config.LocalIndex, config.InstanceIndex)}");
                return;
            }

            if (config.RequireItemAfterActivationInExitScan)
            {
                WardenObjectiveManager.AddObjectiveItemAsRequiredForExitScan(true, new iWardenObjectiveItem[1] { new iWardenObjectiveItem(instance.m_linkedItemComingOut.Pointer) });
                LegacyLogger.Debug($"HSUActivator: {(config.DimensionIndex, config.LayerType, config.LocalIndex, config.InstanceIndex)} - added required item for extraction scan");
            }

            if (config.ChainedPuzzleOnActivation != 0)
            {
                var block = GameDataBlockBase<ChainedPuzzleDataBlock>.GetBlock(config.ChainedPuzzleOnActivation);
                if (block == null)
                {
                    LegacyLogger.Error($"HSUActivator: ChainedPuzzleOnActivation is specified but ChainedPuzzleDatablock definition is not found, won't build");
                }
                else
                {
                    Vector3 startPosition = config.ChainedPuzzleStartPosition.ToVector3();
                    
                    if (startPosition == Vector3.zeroVector)
                    {
                        startPosition = instance.m_itemGoingInAlign.position;
                    }
                    
                    var puzzleInstance = ChainedPuzzleManager.CreatePuzzleInstance(
                        block,
                        instance.SpawnNode.m_area,
                        startPosition,
                        instance.SpawnNode.m_area.transform);

                    activationScans[(config.DimensionIndex, config.LayerType, config.LocalIndex, config.InstanceIndex)] = puzzleInstance;
                    LegacyLogger.Debug($"HSUActivator: ChainedPuzzleOnActivation ID: {config.ChainedPuzzleOnActivation} specified and created");
                }
            }

        }

        private void OnBuildDone()
        {
            if (!levelHSUActivators.ContainsKey(RundownManager.ActiveExpedition.LevelLayoutData)) return;

            levelHSUActivators[RundownManager.ActiveExpedition.LevelLayoutData].HSUActivators.ForEach(Build);
        }

        private void OnEnterLevel() { }

        private void OnLevelCleanup()
        {
            instanceManager.Clear();
            activationScans.Clear();
        }

        private HSUActivatorOverrideManager() { }

        static HSUActivatorOverrideManager()
        {
            Current = new();
            LevelAPI.OnEnterLevel += Current.OnEnterLevel;
            LevelAPI.OnBuildDone += Current.OnBuildDone;
            LevelAPI.OnLevelCleanup += Current.OnLevelCleanup;
        }
    }
}
