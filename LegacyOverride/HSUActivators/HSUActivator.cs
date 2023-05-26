using GameData;
using LEGACY.Utils;
using LevelGeneration;
using System.Collections.Generic;

namespace LEGACY.LegacyOverride.HSUActivators
{
    public class HSUActivator
    { 
        public eDimensionIndex DimensionIndex { get; set; }

        public LG_LayerType LayerType { get; set; }

        public eLocalZoneIndex LocalIndex { get; set; }

        public uint InstanceIndex { get; set; } = uint.MaxValue;

        public List<WardenObjectiveEventData> EventsOnHSUActivation { get; set; } = new();

        public uint ItemFromStart { get; set; } = 0u;

        public uint ItemAfterActivation { get; set; } = 0u;

        public bool RequireItemAfterActivationInExitScan { get; set; } = false;

        public bool TakeOutItemAfterActivation { get; set; } = true;

        public uint ChainedPuzzleOnActivation { get; set; } = 0u;

        public Vec3 ChainedPuzzleStartPosition { get; set; } = new();

        public List<WardenObjectiveEventData> EventsOnActivationScanSolved { get; set; } = new();
    }

    public class LevelHSUActivator
    {
        public uint MainLevelLayout { set; get; }

        public List<HSUActivator> HSUActivators { get; set; } = new() { new() };
    }
}
