using System;
using ExtraObjectiveSetup.BaseClasses;
using System.Collections.Generic;
using ExtraObjectiveSetup.Utils;

namespace LEGACY.LegacyOverride.FogBeacon
{

    public class LevelSpawnedFogBeaconSettings 
    {
        public int AreaIndex { get; set; } = 0;

        public float GrowDuration { get; set; } = 10f;
        
        public float ShrinkDuration { get; set; } = 10f;
        
        public float Range { get; set; } = 11f;

        public string WorldEventObjectFilter { get; set; } = string.Empty;

        public Vec3 Position { get; set; } = new();
    }


    public class LevelSpawnedFogBeacon: ExtraObjectiveSetup.BaseClasses.GlobalZoneIndex
    {
        public List<LevelSpawnedFogBeaconSettings> SpawnedBeaconsInZone { get; set; } = new() { new() };
    }
}
