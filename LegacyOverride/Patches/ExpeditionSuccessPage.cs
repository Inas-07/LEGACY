using CellMenu;
using HarmonyLib;
using LEGACY.LegacyOverride.ExpeditionSuccessPage;
using LEGACY.Utils;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal static class ExpeditionSuccessPage
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CM_PageExpeditionSuccess), nameof(CM_PageExpeditionSuccess.OnEnable))]
        private static void Post_CM_PageExpeditionSuccess_OnEnable(CM_PageExpeditionSuccess __instance) 
        {
            var page = __instance;//MainMenuGuiLayer.Current.PageExpeditionSuccess;
            if (page == null) return;

            var c = SuccessPageCustomizationManager.Current.CurrentCustomization;
            if (c == null) return;

            page.m_header.SetText(c.PageHeader);
            page.m_overrideSuccessMusic = c.OverrideSuccessMusic;
            LegacyLogger.Warning($"Post_CM_PageExpeditionSuccess_OnEnable: {c.PageHeader.ToString()}");
        }
    }
}
