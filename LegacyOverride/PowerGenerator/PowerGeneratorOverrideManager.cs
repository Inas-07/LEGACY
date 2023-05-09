using GTFO.API.Utilities;
using LEGACY.LegacyOverride.PowerGenerator.GeneratorCluster;
using LEGACY.LegacyOverride.PowerGenerator.IndividualGenerator;
using LEGACY.Utils;
using System.IO;

namespace LEGACY.LegacyOverride.PowerGenerator
{
    internal static class PowerGeneratorOverrideManager
    {
        internal static readonly string BASE_CONFIG_PATH = Path.Combine(LegacyOverrideManagers.LEGACY_CONFIG_PATH, "PowerGeneratorOverride");

        internal static void Init()
        {
            if (!Directory.Exists(BASE_CONFIG_PATH))
            {
                Directory.CreateDirectory(BASE_CONFIG_PATH);
            }

            IndividualGeneratorOverrideManager.Current.Init();
            GeneratorClusterOverrideManager.Current.Init();
        }
    }
}
