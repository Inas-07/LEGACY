using FirstPersonItem;
using HarmonyLib;
using ExtraObjectiveSetup.Expedition.Gears;
using LEGACY.LegacyOverride.EnemyTagger;
using UnityEngine;
using System.Collections.Generic;
using GameData;
using LEGACY.Utils;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal class OBSVisualOptimization
    {
        private const float INTENSITY = 0.84f;

        private static uint CurrentGearID = 0;

        private static HashSet<uint> ThermalGearIDs = new();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FirstPersonItemHolder), nameof(FirstPersonItemHolder.SetWieldedItem))]
        private static void Post_SetWieldedItem(FirstPersonItemHolder __instance, ItemEquippable item)
        {
            if (item.GearIDRange == null)
            {
                CurrentGearID = 0;
                return;
            }
            CurrentGearID = ExpeditionGearManager.GetOfflineGearPID(item.GearIDRange);
            foreach (var go in EnemyTaggerSettingManager.Current.OBSVisuals)
            {
                if (!go.active) continue;
                go.GetComponentInChildren<Renderer>().sharedMaterial.SetFloat("_Intensity", INTENSITY);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPIS_Aim), nameof(FPIS_Aim.Update))]
        private static void Post_Aim_Update(FPIS_Aim __instance)
        {
            if (__instance.Holder.WieldedItem == null) return;

            if (!ThermalGearIDs.Contains(CurrentGearID)) return;
            foreach (var go in EnemyTaggerSettingManager.Current.OBSVisuals)
            {
                if (!go.active) continue;
                go.GetComponentInChildren<Renderer>().sharedMaterial.SetFloat("_Intensity", INTENSITY * (1.0f - FirstPersonItemHolder.m_transitionDelta));
            }
        }

        internal static void Init()
        {
            foreach(var b in GameDataBlockBase<PlayerOfflineGearDataBlock>.GetAllBlocks())
            {
                if(b.name.ToLowerInvariant().EndsWith("_t"))
                {
                    if (ThermalGearIDs.Add(b.persistentID))
                    {
                        LegacyLogger.Debug($"Found OfflineGear with thermal sight - {b.name}");
                    }
                }
            }
        }
    }
}
