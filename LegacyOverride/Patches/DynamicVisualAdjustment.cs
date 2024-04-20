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

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal static class DynamicVisualAdjustment
    {
        private const float INTENSITY = 0.84f;

        private static uint CurrentGearID { get; set; } = 0;

        private static HashSet<uint> ThermalGearIDs { get; } = new();

        private static List<(GameObject go, Renderer renderer, float Intensity, float BehindWallIntensity)> VisualRenderers { get; } = new();

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
            for (int i = 0; i < VisualRenderers.Count; i++)
            {
                (GameObject go, Renderer renderer, float intensity, float behindWallIntensity) = VisualRenderers[i];
                if (!go.active) continue;

                if (intensity > 0.0f)
                {
                    renderer.material.SetFloat("_Intensity", intensity);
                }

                if (behindWallIntensity > 0.0f)
                {
                    renderer.material.SetFloat("_BehindWallIntensity", behindWallIntensity);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FPIS_Aim), nameof(FPIS_Aim.Update))]
        private static void Post_Aim_Update(FPIS_Aim __instance)
        {
            if (__instance.Holder.WieldedItem == null) return;

            float t = 1.0f - FirstPersonItemHolder.m_transitionDelta;
            if (!ThermalGearIDs.Contains(CurrentGearID))
            {
                t = Math.Max(0.05f, t);
            }

            for (int i = 0; i < VisualRenderers.Count; i++)
            {
                (GameObject go, Renderer renderer, float intensity, float behindWallIntensity) = VisualRenderers[i];
                if (!go.active) continue;

                if (intensity > 0.0f)
                {
                    renderer.material.SetFloat("_Intensity", intensity * t);
                }

                if (behindWallIntensity > 0.0f)
                {
                    renderer.material.SetFloat("_BehindWallIntensity", behindWallIntensity * t);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CP_Bioscan_Core), nameof(CP_Bioscan_Core.Setup))]
        private static void Post_CaptureBioscanVisual(CP_Bioscan_Core __instance)
        {
            var components = __instance.gameObject.GetComponentsInChildren<Renderer>(true);
            
            if (components != null)
            {
                var renderers = components.Where(comp => comp.gameObject.name.Equals("Zone")).ToList();
                foreach(var r in renderers)
                {
                    var go = r.gameObject;
                    float intensity = r.material.GetFloat("_Intensity");
                    float behindWallIntensity = r.material.GetFloat("_BehindWallIntensity");
                    VisualRenderers.Add((go, r, 
                        intensity > 0.0f ? intensity : -1.0f, 
                        behindWallIntensity > 0.0f ? behindWallIntensity : -1.0f));
                }
            }
        }

        private static void Clear()
        {
            //foreach((GameObject go, Renderer renderer, float OriginalIntensity) in VisualRenderers)
            //{
            //    renderer.material.SetFloat("_Intensity", OriginalIntensity);
            //}

            VisualRenderers.Clear();
        }

        private static void AddOBSVisualRenderers()
        {
            foreach (var go in EnemyTaggerSettingManager.Current.OBSVisuals)
            {
                var renderer = go.GetComponentInChildren<Renderer>();
                float intensity = renderer.material.GetFloat("_Intensity");
                float behindWallIntensity = -1.0f;
                VisualRenderers.Add((go, renderer, intensity, behindWallIntensity));
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

            LevelAPI.OnBuildStart += Clear;
            LevelAPI.OnLevelCleanup += Clear;
            LevelAPI.OnEnterLevel += AddOBSVisualRenderers;
        }
    }
}
