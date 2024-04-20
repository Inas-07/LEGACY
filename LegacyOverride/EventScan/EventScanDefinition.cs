using ExtraObjectiveSetup.Utils;
using GameData;
using Localization;
using System.Collections.Generic;

namespace LEGACY.LegacyOverride.EventScan
{
    public class ColorSetting 
    { 
        public Vec3 Waiting { get; set; } = new Vec3() { x = 0.5294f, y = 0.8078f, z = 0.9215f };

        public Vec3 Active { get; set; } = new Vec3() { x = 1f, y = 0.7529f, z = 0.145f };
    }

    public class ActiveCondition
    {
        public int RequiredPlayerCount { get; set; } = 0;

        public List<int> RequiredBigPickupIndices { get; set; } = new();
    }

    public class EventScanDefinition
    {
        public string WorldEventObjectFilter { get; set; } = string.Empty;

        public Vec3 Position { get; set; } = new();

        public float Radius { get; set; } = 3.2f;

        public ColorSetting ColorSetting { get; set; } = new();

        public LocalizedText DisplayText { get; set; } = null;

        public ActiveCondition ActiveCondition { get; set; } = new();

        public List<WardenObjectiveEventData> EventsOnActivate { get; set; } = new();

        public List<WardenObjectiveEventData> EventsOnDeactivate { get; set; } = new();
    }
}
