using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppSystem;

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
        public const string VERSION = "3.2.0";

        private Harmony m_Harmony;
        
        public override void Load()
        {
            m_Harmony = new Harmony("LEGACY");
            m_Harmony.PatchAll();
        }
    }
}

