using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using LEGACY.VanillaFix;
using LEGACY.ExtraEvents;
using LEGACY.LegacyOverride;
using LEGACY.HardcodedBehaviours;
using GTFO.API;
using AssetShards;
using LEGACY.LegacyOverride.Patches;
using CellMenu;
using ChainedPuzzles;

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
    [BepInDependency("Inas.EOSExt.EMP", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("Inas.EOSExt.SecDoor", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("Inas.EOSExt.DimensionWarp", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("GTFO.FloLib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInIncompatibility("GTFO.AWO")]
    [BepInPlugin(AUTHOR + "." + RUNDOWN_NAME, RUNDOWN_NAME, VERSION)]
    
    public class EntryPoint: BasePlugin
    {
        public const string AUTHOR = "Inas";
        public const string RUNDOWN_NAME = "LEGACY";
        public const string VERSION = "4.2.4";
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
            //ChainedPuzzleManager.DEBUG_ENABLED = true;
            //PlayFabManager.OnTitleDataUpdated += new System.Action(RundownSelectionPageConfig.Setup);
            AssetAPI.OnAssetBundlesLoaded += Assets.Init;
            EventAPI.OnManagersSetup += new System.Action(() => 
                AssetShardManager.add_OnStartupAssetsLoaded(new System.Action(MainMenuGuiLayer.Current.PageRundownNew.SetupCustomTutorialButton)) // init after pdata             
            );
        }
    }
}

