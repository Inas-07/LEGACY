//using HarmonyLib;
//using Gear;
//using LEGACY.Utils;
//using AssetShards;
//using UnityEngine;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//namespace LEGACY.LegacyOverride.Patches
//{
//    [HarmonyPatch]
//    internal class Patch_ThermalScope
//    {
//        private static List<GameObject> sights = new();
//        private static ImmutableHashSet<string> t_sight_models = ImmutableHashSet.Create(
//            "Assets/AssetPrefabs/Items/Gear/Parts/Sights/Sight_10_t.prefab",
//            "Assets/AssetPrefabs/Items/Gear/Parts/Sights/Sight_19_t.prefab"
//        );

//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(GearPartSpawner.PartSpawnData), nameof(GearPartSpawner.PartSpawnData.LoadAssets))]
//        private static void Post_PartSpawnData_LoadAssets(GearPartSpawner.PartSpawnData __instance, bool forceAssetDatabase)
//        {
//            if (__instance.type != eGearComponent.SightPart) return;

//            LegacyLogger.Warning($"Sight: {__instance.general.Model}");
//            if (!t_sight_models.Contains(__instance.general.Model)) return;


//            LegacyLogger.Error($"Got thermal sights");

//            GameObject loadedAsset = AssetShardManager.GetLoadedAsset<GameObject>(__instance.general.Model, forceAssetDatabase);
//            loadedAsset.getcom
//        }
//    }
//}
