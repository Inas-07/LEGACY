using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using LEGACY.LegacyOverride;

namespace LEGACY
{
    [BepInDependency("com.dak.MTFO")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("ScanPositionOverride", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("MTFO.Extension.PartialBlocks", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("Flowaria.MeltdownReactor", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("Inas.LocalProgression", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("Inas.ExtraSurvivalWaveSettings", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("Inas.ExtraObjectiveSetup", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(AUTHOR + "." + RUNDOWN_NAME, RUNDOWN_NAME, VERSION)]
    
    public class EntryPoint: BasePlugin
    {
        public const string AUTHOR = "Inas";
        public const string RUNDOWN_NAME = "LEGACY";
        public const string VERSION = "3.6.4";
        public const bool TESTING = false;
        public const string TEST_STRING = "T2";

        private Harmony m_Harmony;
        
        public override void Load()
        {

            m_Harmony = new Harmony("LEGACY");
            m_Harmony.PatchAll();

            LegacyOverrideManagers.Init();
        }
    }
}

