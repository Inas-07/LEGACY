using GameData;
using LevelGeneration;
using Localization;
using System.Collections.Generic;
namespace LEGACY.LegacyOverride.SecDoorIntText
{
    public class DoorToZone
    {
        public eDimensionIndex DimensionIndex { get; set; }

        public LG_LayerType LayerType { get; set; }

        public eLocalZoneIndex LocalIndex { get; set; }

        public LocalizedText Prefix { get; set; } = null;

        public LocalizedText Postfix { get; set; } = null;

        public LocalizedText TextToReplace { get; set; } = null;
    }

    public class LevelSecDoorIntTextOverride
    {
        public uint MainLevelLayout { set; get; } = 0;

        public List<DoorToZone> doorToZones { set; get; } = new List<DoorToZone>() { new DoorToZone() };
    }
}
