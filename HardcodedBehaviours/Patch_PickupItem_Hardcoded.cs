using HarmonyLib;
using GameData;
using LevelGeneration;

namespace LEGACY.HardcodedBehaviours
{
    [HarmonyPatch]
    class Patch_PickupItem_Hardcoded
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_Distribute_PickupItemsPerZone), nameof(LG_Distribute_PickupItemsPerZone.Build))]
        private static void Pre_LG_Distribute_PickupItemsPerZone(LG_Distribute_PickupItemsPerZone __instance)
        {
            var block = GameDataBlockBase<LevelLayoutDataBlock>.GetBlock("LAYOUT_O4_1_L1");
            if (block == null || RundownManager.ActiveExpedition.LevelLayoutData != block.persistentID) return;

            switch (__instance.m_zone.LocalIndex)
            {
                case eLocalZoneIndex.Zone_1:
                    switch(__instance.m_pickupType)
                    {
                        case ePickupItemType.Consumable:
                            __instance.m_zonePlacementWeights.Start = 100000.0f;
                            __instance.m_zonePlacementWeights.Middle = 0.0f;
                            __instance.m_zonePlacementWeights.End = 0.0f; 
                            break;
                    }
                    break;

                case eLocalZoneIndex.Zone_5:
                    __instance.m_zonePlacementWeights.Start = 0.0f;
                    __instance.m_zonePlacementWeights.Middle = 0.0f;
                    __instance.m_zonePlacementWeights.End = 100000.0f;
                    break;
            }
        }
    }
}
