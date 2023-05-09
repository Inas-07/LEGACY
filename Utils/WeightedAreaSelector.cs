using System.Collections.Generic;
using GameData;
using LevelGeneration;
using GTFO.API;

namespace LEGACY.Utils
{
    public class WeightedAreaSelector
    {
        private static readonly Dictionary<LG_Zone, WeightedAreaSelector> dict = new();

        public static WeightedAreaSelector Get(LG_Zone zone)
        {
            if(!dict.ContainsKey(zone))
            {
                WeightedAreaSelector result = new();

                foreach (var area in zone.m_areas)
                {
                    float weight = 0.0f;
                    switch (area.m_size)
                    {
                        // tiny - 2 + 5 = 7
                        // small - 5 + 15 = 20
                        // medium - 10 + 20 = 30
                        // large - 10 + 25 = 35
                        // huge - 15 + 30 = 45
                        case LG_AreaSize.Tiny: weight = 7f; break;
                        case LG_AreaSize.Small: weight = 20f; break;
                        case LG_AreaSize.Medium: weight = 30f; break;
                        case LG_AreaSize.Large: weight = 35f; break;
                        case LG_AreaSize.Huge: weight = 45f; break;
                        default: LegacyLogger.Error($"Unhandled LG_AreaSize: {area.m_size}. Won't build."); return null;
                    }

                    result.AddEntry(area, weight);
                }

                dict.Add(zone, result);
            }

            return dict[zone];
        }

        public static WeightedAreaSelector Get(eDimensionIndex eDimensionIndex, LG_LayerType layerType, eLocalZoneIndex localIndex)
        {
            LG_Zone zone;
            if (!Builder.CurrentFloor.TryGetZoneByLocalIndex(eDimensionIndex, layerType, localIndex, out zone) || zone == false)
            {
                return null;
            }

            return Get(zone);
        }

        private WeightedRandomBag<LG_Area> weightedRandomBag;

        private WeightedAreaSelector()
        {
            weightedRandomBag = new();
        }

        private void AddEntry(LG_Area area, float weight)
        {
            weightedRandomBag.AddEntry(area, weight);
        }

        public LG_Area GetRandom()
        {
            return weightedRandomBag.GetRandom();
        }

        private static void OnBuildDone()
        {
            // prolly prebuild bags for all built zone
        }

        private static void Clear()
        {
            dict.Clear();
        }

        static WeightedAreaSelector()
        {
            LevelAPI.OnLevelCleanup += dict.Clear;
        }
    }
}
