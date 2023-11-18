using LevelGeneration;
using GameData;
using LEGACY.Utils;
using SNetwork;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static void KillEnemiesInArea(LG_Area area)
        {
            foreach (var enemy in area.m_courseNode.m_enemiesInNode.ToArray())
            {
                if (enemy != null && enemy.Damage != null)
                {
                    enemy.Damage.MeleeDamage(float.MaxValue, null, UnityEngine.Vector3.zero, UnityEngine.Vector3.up, 0, 1f, 1f, 1f, 1f, false, DamageNoiseLevel.Normal);
                }
            }
        }

        private static void KillEnemiesInArea(WardenObjectiveEventData e)
        {
            if(!Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out var zone) || zone == null)
            {
                LegacyLogger.Error($"KillEnemiesInArea - Failed to find {(e.DimensionIndex, e.Layer, e.LocalIndex)}");
                return;
            }

            if(e.Count < 0 || e.Count >= zone.m_areas.Count)
            {
                LegacyLogger.Error($"KillEnemiesInArea - invalid area index {e.Count} (specified by 'Count')");
                return;
            }

            KillEnemiesInArea(zone.m_areas[e.Count]);
        }

        private static void KillEnemiesInZone(WardenObjectiveEventData e)
        {
            if (!SNet.IsMaster) return;

            if (!Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out var zone) || zone == null)
            {
                LegacyLogger.Error($"KillEnemiesInArea - Failed to find {(e.DimensionIndex, e.Layer, e.LocalIndex)}");
                return;
            }

            foreach(var area in zone.m_areas)
            {
                KillEnemiesInArea(area);
            }
        }

        private static void KillEnemiesInDimension(WardenObjectiveEventData e)
        {
            if (!SNet.IsMaster) return;
            if(!Dimension.GetDimension(e.DimensionIndex, out var dimension))
            {
                LegacyLogger.Error($"KillEnemiesInDimension: invalid dimension index {e.DimensionIndex}");
                return;
            }

            for (int i = 0; i < dimension.Layers.Count; ++i)
            {
                LG_Layer layer = dimension.Layers[i];
                for (int j = 0; j < layer.m_zones.Count; ++j)
                {
                    LG_Zone zone2 = layer.m_zones[j];
                    LG_SecurityDoor door;

                    // ignore return value (instead evaluated below)
                    Helper.TryGetZoneEntranceSecDoor(zone2, out door);

                    // door opened, kill all. 
                    // if door is not opened, enemies behind the door wont be killed
                    if (j == 0 || door != null && door.m_sync.GetCurrentSyncState().status == eDoorStatus.Open)
                    {
                        // kill all enemies in zone
                        foreach(var area in zone2.m_areas)
                        {
                            KillEnemiesInArea(area);
                        }
                    }
                }
            }
        }
    }
}