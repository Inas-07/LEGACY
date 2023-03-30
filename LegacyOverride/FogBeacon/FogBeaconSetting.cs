using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEGACY.LegacyOverride.FogBeacon
{
    public class RepellerSphereSetting
    {
        public bool InfiniteDuration { get; set; } = false;
        public float GrowDuration { get; set; } = 10f;
        public float ShrinkDuration { get; set; } = 10f;
        public float Range { get; set; } = 11f;
    }

    public class FogBeaconSetting
    {
        public uint MainLevelLayout { set; get; } = 0;
        public float TimeToPickup { set; get; } = 1f;
        public float TimeToPlace { set; get; } = 1f;
        public RepellerSphereSetting RSHold { get; set; } = new();
        public RepellerSphereSetting RSPlaced { get; set; } = new();
    }
}
