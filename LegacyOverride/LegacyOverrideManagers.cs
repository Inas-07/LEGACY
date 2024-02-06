using LEGACY.LegacyOverride.ElevatorCargo;
using LEGACY.LegacyOverride.FogBeacon;
using LEGACY.LegacyOverride.EnemyTagger;
using LEGACY.LegacyOverride.SecDoorIntText;
using MTFO.API;
using System.IO;
using LEGACY.LegacyOverride.ForceFail;
using LEGACY.LegacyOverride.EnemyTargeting;
using Il2CppInterop.Runtime.Injection;
using LEGACY.LegacyOverride.ExpeditionIntelNotification;

namespace LEGACY.LegacyOverride
{
    internal static class LegacyOverrideManagers
    {
        internal static readonly string LEGACY_CONFIG_PATH = Path.Combine(MTFOPathAPI.CustomPath, "LegacyOverride");

        internal static void Init()
        {
            ElevatorCargoOverrideManager.Current.Init();
            FogBeaconSettingManager.Current.Init();
            EnemyTaggerSettingManager.Current.Init();
            SecDoorIntTextOverrideManager.Current.Init();
            DimensionWarpPositionManager.Current.Init();
            ForceFailManager.Current.Init();
            ExpeditionIntelNotifier.Current.Init();
            ClassInjector.RegisterTypeInIl2Cpp<EnemyTargetingPrioritizer>();
        }
    }
}
