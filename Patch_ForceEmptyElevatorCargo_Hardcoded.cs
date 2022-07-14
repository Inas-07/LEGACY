using Il2CppSystem.Collections.Generic;
using HarmonyLib;
using GameData;
using LEGACY.Utilities;
using UnityEngine;
using LevelGeneration;
namespace LEGACY.Hardcoded_Behaviour
{
    [HarmonyPatch]
    class Patch_ForceEmptyElevatorCargo_Hardcoded
    {
        private static uint POWERCELL_ID = 131;

        private static bool ForceDisable() => RundownManager.ActiveExpedition.LevelLayoutData == (uint)MainLayerID.L2E2
                || RundownManager.ActiveExpedition.LevelLayoutData == (uint)MainLayerID.L2E3;


        [HarmonyPrefix]
        [HarmonyPatch(typeof(ElevatorCargoCage), nameof(ElevatorCargoCage.SpawnObjectiveItemsInLandingArea))]
        private static bool Pre_ForceEmptyElevatorCargo(ElevatorCargoCage __instance)
        {
            if (ForceDisable() == true)
            {
                ElevatorRide.Current.m_cargoCageInUse = false;
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ElevatorCargoCage), nameof(ElevatorCargoCage.SpawnObjectiveItemsInLandingArea))]
        private static void Post_AddSpecificItemToElevatorCargo(ElevatorCargoCage __instance)
        {
            if(RundownManager.ActiveExpedition.LevelLayoutData == (uint)MainLayerID.L3E2)
            {
                __instance.m_itemsToMoveToCargo = new List<Transform>();
                LG_PickupItem lgPickupItem = LG_PickupItem.SpawnGenericPickupItem(ElevatorShaftLanding.CargoAlign);
                lgPickupItem.SpawnNode = Builder.GetElevatorArea().m_courseNode;
                lgPickupItem.SetupAsBigPickupItem(Random.Range(0, int.MaxValue), POWERCELL_ID, false, 0);
                __instance.m_itemsToMoveToCargo.Add(lgPickupItem.transform);
                ElevatorRide.Current.m_cargoCageInUse = true;
            }
        }
    }
}
