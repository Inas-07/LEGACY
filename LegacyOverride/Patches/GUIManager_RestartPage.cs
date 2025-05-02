using GTFO.API;
using HarmonyLib;
using LEGACY.LegacyOverride.Restart;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal class GUIManager_RestartPage
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(GuiManager), nameof(GuiManager.Setup))]
        private static void Post_Setup(GuiManager __instance)
        {
            //CM_PageRestart.Setup(); // TOO EARLY, assets yet loaded
            LevelAPI.OnBuildStart += () =>
            {
                CM_PageRestart.Setup();
            };
        }
    }
}
