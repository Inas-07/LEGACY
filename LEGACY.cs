using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using LEGACY.Reactor;
using ScanPosOverride.JSON;
using System.IO;

namespace LEGACY
{
    [BepInDependency("com.dak.MTFO")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("ScanPositionOverride", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("MTFO.Extension.PartialBlocks", BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin("Inas07.LEGACY", "LEGACY", "3.1.3.2")]
    
    public class EntryPoint: BasePlugin
    {
        private Harmony m_Harmony;
        internal static readonly string LEGACY_CUSTOM_PATH = Path.Combine(MTFOUtil.CustomPath, "LEGACY");
        
        public override void Load()
        {
            m_Harmony = new Harmony("LEGACY");
            m_Harmony.PatchAll();

            //ReactorConfigManager.Current.Init(Path.Combine(LEGACY_CUSTOM_PATH, "ReactorConfig"));
        }
    }
}

