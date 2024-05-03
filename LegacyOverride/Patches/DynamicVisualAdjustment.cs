using FirstPersonItem;
using HarmonyLib;
using ExtraObjectiveSetup.Expedition.Gears;
using LEGACY.LegacyOverride.EnemyTagger;
using UnityEngine;
using System.Collections.Generic;
using GameData;
using LEGACY.Utils;
using GTFO.API;
using ChainedPuzzles;
using System.Linq;
using ScanPosOverride.Managers;
using System;
using LEGACY.LegacyOverride.ThermalSightAdjustment;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal static class DynamicVisualAdjustment
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FirstPersonItemHolder), nameof(FirstPersonItemHolder.SetWieldedItem))]
        private static void Post_SetWieldedItem(FirstPersonItemHolder __instance, ItemEquippable item)
        {
            TSAManager.Current.OnPlayerItemWielded(__instance, item);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPIS_Aim), nameof(FPIS_Aim.Update))]
        private static void Post_Aim_Update(FPIS_Aim __instance)
        {
            if (__instance.Holder.WieldedItem == null) return;

            float t = 1.0f - FirstPersonItemHolder.m_transitionDelta;
            if (!TSAManager.Current.IsGearWithThermal(TSAManager.Current.CurrentGearPID))
            {
                t = Math.Max(0.05f, t);
            }

            TSAManager.Current.SetPuzzleVisualsIntensity(t);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CP_Bioscan_Core), nameof(CP_Bioscan_Core.Setup))]
        private static void Post_CaptureBioscanVisual(CP_Bioscan_Core __instance)
        {
            TSAManager.Current.RegisterPuzzleVisual(__instance);
        }
    }
}
