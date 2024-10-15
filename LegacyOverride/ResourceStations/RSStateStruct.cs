using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEGACY.LegacyOverride.ResourceStations
{
    public struct RSStateStruct
    {
        public int LastInteractedPlayer;

        public int RemainingUseTime;

        public float CurrentCooldownTime;

        public bool Enabled;

        public RSStateStruct() { }
    }
}
