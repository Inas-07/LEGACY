using LevelGeneration;
using GameData;
using LEGACY.Utils;
using Player;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static void AlertEnemiesInArea(LG_Area area)
        {
            var node = area.m_courseNode;
            if (node.m_enemiesInNode.Count <= 0) return;

            var enemy = node.m_enemiesInNode[0];
            PlayerAgent playerAgent = null;
            if (PlayerManager.TryGetClosestAlivePlayerAgent(enemy.CourseNode, out playerAgent) && playerAgent != null)
            {
                //enemy.PropagateTargetFull(playerAgent); // NOTE: in R6alt build this method works but enemies behind unopened door won't be awakened.
             
                // NOTE: the following code will awaken enemies that's too close to target zone as well
                UnityEngine.Vector3 position = node.m_enemiesInNode[0].Position;
                NoiseManager.MakeNoise(new NM_NoiseData
                {
                    noiseMaker = null,
                    position = position,
                    radiusMin = 50f,
                    radiusMax = 120f,
                    yScale = 1f,
                    node = node,
                    type = NM_NoiseType.InstaDetect,
                    includeToNeightbourAreas = true,
                    raycastFirstNode = false
                });
            }
            else
            {
                LegacyLogger.Error($"AlertEnemies: failed to alert enemies in area in Zone_{area.m_zone.LocalIndex}, {area.m_zone.m_layer.m_type}, {area.m_zone.DimensionIndex}");
            }
        }

        private static void AlertEnemiesInZone(WardenObjectiveEventData e)
        {
            if (Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out var zone) == false || zone == null)
            {
                LegacyLogger.Error($"AlertEnemiesInZone: zone not found, {(e.DimensionIndex, e.Layer, e.LocalIndex)}");
                return;
            }

            LegacyLogger.Debug($"AlertEnemiesInZone: {e.LocalIndex}, {e.Layer}, {e.DimensionIndex}");
            foreach (var area in zone.m_areas)
            {
                AlertEnemiesInArea(area);
            }
        }

        private static void AlertEnemiesInArea(WardenObjectiveEventData e)
        {
            if (Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out var zone) == false || zone == null)
            {
                LegacyLogger.Error($"AlertEnemiesInArea: zone not found, {(e.DimensionIndex, e.Layer, e.LocalIndex)}");
                return;
            }

            if (e.Count >= zone.m_areas.Count && e.Count < 0)
            {
                LegacyLogger.Error($"AlertEnemiesInArea: invalid area index {e.Count} (specified by 'Count')");
                return;
            }

            AlertEnemiesInArea(zone.m_areas[e.Count]);
        }
    }
}