using GameData;
using LevelGeneration;
using System.Collections.Generic;
using LEGACY.Utils;

namespace LEGACY.LegacyOverride.PowerGenerator.IndividualGenerator
{
    public class IndividualGenerator
    {
        public uint PowerGeneratorIndex { set; get; } = uint.MaxValue; // valid index starts from 0

        public bool ForceAllowPowerCellInsertion { get; set; } = false;

        public List<WardenObjectiveEventData> EventsOnInsertCell { get; set; } = new();

        //public int AreaIndex { get; set; } = -1;

        public Vec3 Position { get; set; } = new();

        public Vec3 Rotation { get; set; } = new();
    }

    public class ZoneGenerators
    {
        public eDimensionIndex DimensionIndex { get; set; }

        public LG_LayerType LayerType { get; set; }

        public eLocalZoneIndex LocalIndex { get; set; }

        public List<IndividualGenerator> IndividualGeneratorsInZone { get; set; } = new() { new() };
    }

    public class LevelIndividualGenerators
    {
        public uint MainLevelLayout { set; get; }

        public List<ZoneGenerators> PowerGeneratorsInLevel { set; get; } = new() { new() };
    }
}