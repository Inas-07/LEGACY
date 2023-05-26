using AK;
using Gear;
using HarmonyLib;
using LEGACY.LegacyOverride.ExtraExpeditionSettings;
using LEGACY.Utils;
using UnityEngine;
namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal class Patch_ExpeditionSettings
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyScanner), nameof(EnemyScanner.OnGearSpawnComplete))]
        private static void Post_EnemyScanner_OnGearSpawnComplete(EnemyScanner __instance)
        {
            ExpeditionSettingsManager.Current.Register(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnemyScanner), nameof(EnemyScanner.UpdateTagProgress))]
        private static bool Post_EnemyScanner_Update(EnemyScanner __instance)
        {
            if (ExpeditionSettingsManager.Current.IsBioTrackerDisabled)
            {
                __instance.Sound.UpdatePosition(__instance.transform.position);
                __instance.m_graphics.UpdateCameraOrientation(__instance.Owner.Position, __instance.Owner.Forward);

                __instance.m_screen.SetStatusText("<color=red>############</color>");
                __instance.m_progressBar.SetProgress(1.0f);
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
