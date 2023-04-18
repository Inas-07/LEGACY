using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using LEGACY.LegacyOverride;
using LEGACY.LegacyOverride.EnemyTagger;

namespace LEGACY
{
    [BepInDependency("com.dak.MTFO")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("ScanPositionOverride", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("MTFO.Extension.PartialBlocks", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(AUTHOR + "." + RUNDOWN_NAME, RUNDOWN_NAME, VERSION)]
    
    public class EntryPoint: BasePlugin
    {
        public const string AUTHOR = "Inas07";
        public const string RUNDOWN_NAME = "LEGACY";
        public const string VERSION = "3.4.3";
        public const bool TESTING = false;
        public const string TEST_STRING = "T1";

        private Harmony m_Harmony;
        
        public override void Load()
        {

            m_Harmony = new Harmony("LEGACY");
            m_Harmony.PatchAll();

            LegacyOverrideManagers.Init();
        }
    }
}

