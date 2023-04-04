using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEGACY.LegacyOverride.EnemyTagger
{
    public class EnemyTaggerSetting
    {
        public uint MainLevelLayout { set; get; } = 0;
        public float TimeToPickup { set; get; } = 1f;
        public float TimeToPlace { set; get; } = 1f;
        public float WarmupTime { set; get; } = 5f;
        public int MaxTagPerScan { set; get; } = 12;
        public float TagInterval { set; get; } = 3.0f;
        public float TagRadius { set; get; } = 12f;
        public bool TagWhenPlaced { set; get; } = true;
        public bool TagWhenHold { set; get; } = false;
    }
}
