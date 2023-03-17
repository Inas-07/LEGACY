using LEGACY.LegacyOverride.ElevatorCargo;
using LEGACY.LegacyOverride.Terminal;
using MTFO.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEGACY.LegacyConfig
{
    internal static class LegacyOverrideManagers
    {
        internal static readonly string LEGACY_CONFIG_PATH = Path.Combine(MTFOPathAPI.CustomPath, "LegacyOverride");

        internal static void Init()
        {
            ElevatorCargoOverrideManager.Current.Init();
            TerminalPositionOverrideManager.Current.Init();
        }
    }
}
