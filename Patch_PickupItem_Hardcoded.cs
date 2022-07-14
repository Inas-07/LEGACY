using Il2CppSystem.Collections.Generic;
using HarmonyLib;
using GameData;
using LEGACY.Utilities;
using SNetwork;
using ChainedPuzzles;
using LevelGeneration;
using Localization;

namespace LEGACY.Hardcoded_Behaviour
{
    [HarmonyPatch]
    class Patch_PickupItem_Hardcoded
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_Distribute_PickupItemsPerZone), nameof(LG_Distribute_PickupItemsPerZone.Build))]
        private static void Pre_LG_Distribute_PickupItemsPerZone(LG_Distribute_PickupItemsPerZone __instance)
        {

            if (RundownManager.ActiveExpedition.LevelLayoutData != (uint)MainLayerID.L2E1) return;

            //Logger.Debug("Overwriting Fog turbine distribution for L2E1. Zone: {0}", __instance.m_zone.AliasName);

            switch (__instance.m_zone.LocalIndex) 
            {
                case eLocalZoneIndex.Zone_10:
                    __instance.m_zonePlacementWeights.Start = 10000.0f;
                    __instance.m_zonePlacementWeights.Middle = 0.0f;
                    __instance.m_zonePlacementWeights.End = 0.0f;
                    break;

                case eLocalZoneIndex.Zone_11:
                    __instance.m_zonePlacementWeights.Start = 0.0f;
                    __instance.m_zonePlacementWeights.Middle = 5000.0f;
                    __instance.m_zonePlacementWeights.End = 5000.0f;
                    break;
                default: break;
            }
        }
    }
}
