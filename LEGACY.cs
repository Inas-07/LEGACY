using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;


namespace LEGACY
{
    [BepInDependency("com.dak.MTFO")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("ScanPositionOverride", BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin("Inas07.LEGACY", "LEGACY", "3.1.3.2")]
    
    public class EntryPoint: BasePlugin
    {
        private Harmony m_Harmony;

        public override void Load()
        {
            m_Harmony = new Harmony("LEGACY");
            m_Harmony.PatchAll();
        }
    }
}

