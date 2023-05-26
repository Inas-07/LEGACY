using GameData;
using LevelGeneration;
using System.Collections.Generic;

namespace LEGACY.LegacyOverride.PowerGenerator.GeneratorCluster
{
    public class GeneratorCluster
    {
        public uint GeneratorClusterIndex { get; set; } = 0; // default to 0. In most case there's only 1 GC in a zone

        public uint NumberOfGenerators { get; set; } = 0;

        // OnActivateOnSolveItem is enabled by default
        public List<List<WardenObjectiveEventData>> EventsOnInsertCell { get; set; } = new() { new() };

        public uint EndSequenceChainedPuzzle { get; set; } = 0u;

        public List<WardenObjectiveEventData> EventsOnEndSequenceChainedPuzzleComplete { get; set; } = new() { };
    }

    public class ZoneGeneratorCluster
    {
        public eDimensionIndex DimensionIndex { get; set; }

        public LG_LayerType LayerType { get; set; }

        public eLocalZoneIndex LocalIndex { get; set; }

        // OnActivateOnSolveItem is enabled by default
        public List<GeneratorCluster> GeneratorClustersInZone { get; set; } = new() { new() };
    }

    public class LevelGeneratorClusters
    {
        public uint MainLevelLayout { set; get; }

        public List<ZoneGeneratorCluster> GeneratorClustersInLevel { set; get; } = new() { new() };
    }
}
