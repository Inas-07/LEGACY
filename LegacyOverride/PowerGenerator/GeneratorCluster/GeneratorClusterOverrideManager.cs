using GTFO.API.Utilities;
using LEGACY.Utils;
using System.IO;
using System.Collections.Generic;
using GTFO.API;
using LevelGeneration;
using GameData;
using System.Runtime.CompilerServices;
using ChainedPuzzles;

namespace LEGACY.LegacyOverride.PowerGenerator.GeneratorCluster
{
    internal class GeneratorClusterOverrideManager
    {
        public static readonly GeneratorClusterOverrideManager Current;

        private Dictionary<uint, LevelGeneratorClusters> levelPowerGeneratorClusters = new();

        private LiveEditListener PGC_LEListener;

        private static readonly string PGC_CONFIG_PATH = Path.Combine(PowerGeneratorOverrideManager.BASE_CONFIG_PATH, "PowerGeneratorCluster");

        private InstanceManager<LG_PowerGeneratorCluster> instanceManager = new(); // accessible within patch: LG_PowerGeneratorCluster.Setup 

        private List<(LG_PowerGeneratorCluster, GeneratorCluster)> chainedPuzzleToBuild = new();

        private void AddOverride(LevelGeneratorClusters _override)
        {
            if (_override == null) return;

            if (levelPowerGeneratorClusters.ContainsKey(_override.MainLevelLayout))
            {
                LegacyLogger.Warning("Replaced MainLevelLayout {0}", _override.MainLevelLayout);
                levelPowerGeneratorClusters[_override.MainLevelLayout] = _override;
            }
            else
            {
                levelPowerGeneratorClusters.Add(_override.MainLevelLayout, _override);
            }
        }

        public void Init()
        {
            if (!Directory.Exists(PGC_CONFIG_PATH))
            {
                Directory.CreateDirectory(PGC_CONFIG_PATH);

                var file = File.CreateText(Path.Combine(PGC_CONFIG_PATH, "Template.json"));
                file.WriteLine(Json.Serialize(new LevelGeneratorClusters()));
                file.Flush();
                file.Close();

                return;
            }

            foreach (string confFile in Directory.EnumerateFiles(PGC_CONFIG_PATH, "*.json", SearchOption.AllDirectories))
            {
                LevelGeneratorClusters conf;
                Json.Load(confFile, out conf);

                AddOverride(conf);
            }

            PGC_LEListener = LiveEdit.CreateListener(PGC_CONFIG_PATH, "*.json", true);
            PGC_LEListener.FileChanged += FileChanged;
        }

        private void FileChanged(LiveEditEventArgs e)
        {
            LegacyLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                LevelGeneratorClusters conf = Json.Deserialize<LevelGeneratorClusters>(content);
                AddOverride(conf);
            });
        }

        public List<ZoneGeneratorCluster> GetLevelGeneratorClusterOverride(uint MainLevelLayout) => levelPowerGeneratorClusters.ContainsKey(MainLevelLayout) ? levelPowerGeneratorClusters[MainLevelLayout].GeneratorClustersInLevel : null;

        public uint Register(LG_PowerGeneratorCluster __instance)
        {
            uint instanceIndex = instanceManager.Register(
                __instance.SpawnNode.m_dimension.DimensionIndex,
                __instance.SpawnNode.LayerType,
                __instance.SpawnNode.m_zone.LocalIndex,
                __instance);

            return instanceIndex;
        }

        internal void RegisterForChainedPuzzleBuild(LG_PowerGeneratorCluster __instance, GeneratorCluster GeneratorClusterConfig)
        {
            chainedPuzzleToBuild.Add((__instance, GeneratorClusterConfig));
        }

        private void OnBuildDone()
        {
            foreach(var tuple in chainedPuzzleToBuild)
            {
                LG_PowerGeneratorCluster __instance = tuple.Item1;
                var config = tuple.Item2;
                uint persistentId = config.EndSequenceChainedPuzzle;

                var block = GameDataBlockBase<ChainedPuzzleDataBlock>.GetBlock(persistentId);

                if (block != null)
                {
                    LegacyLogger.Debug($"Building EndSequenceChainedPuzzle for LG_PowerGeneratorCluster in {__instance.SpawnNode.m_zone.LocalIndex}, {__instance.SpawnNode.LayerType}, {__instance.SpawnNode.m_dimension.DimensionIndex}");

                    __instance.m_chainedPuzzleMidObjective = ChainedPuzzleManager.CreatePuzzleInstance(
                        block,
                        __instance.SpawnNode.m_area,
                        __instance.m_chainedPuzzleAlignMidObjective.position,
                        __instance.m_chainedPuzzleAlignMidObjective);

                    if (config.EventsOnEndSequenceChainedPuzzleComplete != null && config.EventsOnEndSequenceChainedPuzzleComplete.Count > 0)
                    {
                        __instance.m_chainedPuzzleMidObjective.OnPuzzleSolved += new System.Action(() => config.EventsOnEndSequenceChainedPuzzleComplete.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true)));
                        LegacyLogger.Debug("EventsOnEndSequenceChainedPuzzleComplete: executing events...");
                    }
                }
            }
        }

        private void OnEnterLevel() { }

        private void OnLevelCleanup()
        {
            instanceManager.Clear();
            chainedPuzzleToBuild.Clear();
        }

        private GeneratorClusterOverrideManager() { }

        static GeneratorClusterOverrideManager()
        {
            Current = new();
            LevelAPI.OnEnterLevel += Current.OnEnterLevel;
            LevelAPI.OnBuildDone += Current.OnBuildDone;
            LevelAPI.OnLevelCleanup += Current.OnLevelCleanup;
        }
    }
}
