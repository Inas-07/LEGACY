using BepInEx;
using BepInEx.Unity.IL2CPP;
//using BepInEx.IL2CPP;

using HarmonyLib;


namespace LEGACY
{
    [BepInDependency("com.dak.MTFO")]
    [BepInDependency("dev.gtfomodding.gtfo-api")]
    [BepInPlugin("Inas07.LEGACY", "LEGACY", "1.0.0.1")]
    
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

