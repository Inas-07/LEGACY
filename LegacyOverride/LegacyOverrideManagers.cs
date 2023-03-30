using LEGACY.LegacyOverride.ElevatorCargo;
using LEGACY.LegacyOverride.FogBeacon;
using LEGACY.LegacyOverride.Terminal;
using MTFO.API;
using System.IO;

namespace LEGACY.LegacyOverride
{
    internal static class LegacyOverrideManagers
    {
        internal static readonly string LEGACY_CONFIG_PATH = Path.Combine(MTFOPathAPI.CustomPath, "LegacyOverride");

        internal static void Init()
        {
            ElevatorCargoOverrideManager.Current.Init();
            TerminalPositionOverrideManager.Current.Init();
            FogBeaconSettingManager.Current.Init();
        }
    }
}
