using System.IO;
using System.Collections.Generic;
using LEGACY.LegacyConfig;
using MTFO.API;

namespace LEGACY
{
    internal class LegacyConfigManager
    {
        public static readonly LegacyConfigManager Current;

        internal static readonly string LEGACY_CONFIG_PATH = Path.Combine(MTFOPathAPI.CustomPath, "LegacyConfig");

        public void Init()
        {
            ElevatorCargoOverrideManager.Current.Init();
            TerminalPositionOverrideManager.Current.Init();
        }

        static LegacyConfigManager()
        {
            Current = new();
        }

        private LegacyConfigManager() { }
    }
}
