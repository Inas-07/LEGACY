using LEGACY.LegacyOverride.ElevatorCargo;
using LEGACY.LegacyOverride.FogBeacon;
using LEGACY.LegacyOverride.Terminal;
using LEGACY.LegacyOverride.EnemyTagger;
using MTFO.API;
using System.IO;
using LEGACY.LegacyOverride.SecDoorIntText;

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
            EnemyTaggerSettingManager.Current.Init();
            SecDoorIntTextOverrideManager.Current.Init();
        }
    }
}
