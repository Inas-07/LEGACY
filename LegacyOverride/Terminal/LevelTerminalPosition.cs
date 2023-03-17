using GameData;
using GTFO.API.Utilities;
using LevelGeneration;
using System.Collections.Generic;
using LEGACY.Utils;
using System.IO;
using LEGACY.LegacyConfig;

namespace LEGACY.LegacyOverride.Terminal
{
    public class TerminalPosition
    {
        public eDimensionIndex DimensionIndex { get; set; }

        public LG_LayerType LayerType { get; set; }

        public eLocalZoneIndex LocalIndex { get; set; }

        public int TerminalIndex { get; set; } = -1;

        public Vec3 Position { get; set; } = new();

        public Vec3 Rotation { get; set; } = new();
    }

    public class LevelTerminalPosition
    {
        public uint MainLevelLayout { set; get; }

        public List<TerminalPosition> TerminalPositions { set; get; } = new() { new() };
    }


}
