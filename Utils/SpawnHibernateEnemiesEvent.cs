using GameData;
using LevelGeneration;

namespace LEGACY.Utils
{
    public class SpawnHibernateEnemiesEvent
    {
        public eWardenObjectiveEventTrigger Trigger { set; get; } = eWardenObjectiveEventTrigger.None;

        public int Type { set; get; } = 170;

        public eDimensionIndex DimensionIndex { set; get; } = eDimensionIndex.Reality;

        public LG_LayerType Layer { set; get; } = LG_LayerType.MainLayer;

        public eLocalZoneIndex LocalIndex { set; get; } = eLocalZoneIndex.Zone_0;

        public string WorldEventObjectFilter { set; get; } = "RANDOM";

        public uint EnemyID { set; get; }

        public int Count { set; get; } = 1;

        public float Delay { set; get; } = 0.0f;
    }
}
