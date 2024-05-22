using HarmonyLib;
using LEGACY;
using MTFO;

[HarmonyPatch(typeof(PUI_Watermark), nameof(PUI_Watermark.UpdateWatermark))]
internal static class Patch_WatermarkUpdateWatermark
{
    private static void Postfix(PUI_Watermark __instance)
    {
        string MTFOVersion = MTFO.MTFO.VERSION.Remove("x.x.x".Length);
        __instance.m_watermarkText.SetText($"<color=red>MODDED</color> <color=orange>{MTFOVersion}</color>\n<color=#00ae9d>LEGACY</color> <color=orange>{EntryPoint.VERSION} STANDALONE</color>");
    }
}