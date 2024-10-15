using LEGACY.LegacyOverride.ElevatorCargo;
using LEGACY.LegacyOverride.FogBeacon;
using LEGACY.LegacyOverride.EnemyTagger;
//using LEGACY.LegacyOverride.SecDoorIntText;
using MTFO.API;
using System.IO;
using LEGACY.LegacyOverride.ForceFail;
using LEGACY.LegacyOverride.ExpeditionIntelNotification;
using LEGACY.LegacyOverride.EventScan;
using LEGACY.LegacyOverride.DummyVisual;
using LEGACY.LegacyOverride.Music;
using LEGACY.LegacyOverride.ExpeditionSuccessPage;
using GTFO.API;
using AssetShards;
using LEGACY.LegacyOverride.Patches;
//using LEGACY.LegacyOverride.ThermalSightAdjustment;
using LEGACY.LegacyOverride.ResourceStations;

namespace LEGACY.LegacyOverride
{
    internal static class LegacyOverrideManagers
    {
        internal static readonly string LEGACY_CONFIG_PATH = Path.Combine(MTFOPathAPI.CustomPath, "LegacyOverride");

        internal static void Init()
        {
            ElevatorCargoOverrideManager.Current.Init();
            BigPickupFogBeaconSettingManager.Current.Init();
            EnemyTaggerSettingManager.Current.Init();
            ForceFailManager.Current.Init();
            ExpeditionIntelNotifier.Current.Init();
            EventScanManager.Current.Init();
            VisualManager.Current.Init();
            MusicStateOverrider.Current.Init();
            LevelSpawnedFogBeaconManager.Current.Init();
            SuccessPageCustomizationManager.Current.Init();
            ResourceStationManager.Current.Init();
        }
    }
}
