using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using LEGACY.VanillaFix;
using LEGACY.ExtraEvents;
using LEGACY.LegacyOverride;

namespace LEGACY
{
    [BepInDependency("com.dak.MTFO")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("ScanPositionOverride", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("MTFO.Extension.PartialBlocks", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("Inas.LocalProgression", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("Inas.ExtraSurvivalWaveSettings", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("Inas.ExtraObjectiveSetup", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("Inas.EOSExt.SecurityDoorTerminal", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("Inas.EOSExt.Reactor", BepInDependency.DependencyFlags.HardDependency)]
    [BepInIncompatibility("GTFO.AWO")]
    [BepInPlugin(AUTHOR + "." + RUNDOWN_NAME, RUNDOWN_NAME, VERSION)]
    
    public class EntryPoint: BasePlugin
    {
        public const string AUTHOR = "Inas";
        public const string RUNDOWN_NAME = "LEGACY";
        public const string VERSION = "3.8.5";
        public const bool TESTING = false;
        public const string TEST_STRING = "TESTING";

        private Harmony m_Harmony;
        
        public override void Load()
        {

            m_Harmony = new Harmony("LEGACY");
            m_Harmony.PatchAll();

            LegacyOverrideManagers.Init();
            LegacyExtraEvents.Init();

            Debugger.Current.Init();
        }
    }
}

