using HarmonyLib;
using LEGACY.Utils;
using UnityEngine;
using LevelGeneration;
using LEGACY.LegacyConfig;
using System.Collections.Generic;

namespace LEGACY.HardcodedBehaviours
{
    [HarmonyPatch]
    internal class Patch_ElevatorCargoOverride
    {
        //private static bool ForceDisable() => RundownManager.ActiveExpedition.LevelLayoutData == (uint)MainLayerID.L2E2
        //        || RundownManager.ActiveExpedition.LevelLayoutData == (uint)MainLayerID.L2E3
        //        || RundownManager.ActiveExpedition.LevelLayoutData == (uint)MainLayerID.L3E2;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ElevatorCargoCage), nameof(ElevatorCargoCage.SpawnObjectiveItemsInLandingArea))]
        private static bool Pre_ElevatorCargoOverride(ElevatorCargoCage __instance)
        {
            LevelElevatorCargo levelElevatorCargos = ElevatorCargoOverrideManager.Current.GetLevelElevatorCargoItems(RundownManager.ActiveExpedition.LevelLayoutData);
            if (levelElevatorCargos == null) return true;

            if (levelElevatorCargos.ForceDisable)
            {
                __instance.m_itemsToMoveToCargo.Clear();
                __instance.m_itemsToMoveToCargo = null;
                ElevatorRide.Current.m_cargoCageInUse = false;
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ElevatorCargoCage), nameof(ElevatorCargoCage.SpawnObjectiveItemsInLandingArea))]
        private static void Post_ElevatorCargoOverride(ElevatorCargoCage __instance)
        {
            LevelElevatorCargo levelElevatorCargos = ElevatorCargoOverrideManager.Current.GetLevelElevatorCargoItems(RundownManager.ActiveExpedition.LevelLayoutData);
            if (levelElevatorCargos == null || levelElevatorCargos.ElevatorCargoItems.Count < 1) return;

            foreach (var elevatorCargo in levelElevatorCargos.ElevatorCargoItems)
            {
                LG_PickupItem item = LG_PickupItem.SpawnGenericPickupItem(ElevatorShaftLanding.CargoAlign);
                item.SpawnNode = Builder.GetElevatorArea().m_courseNode;

                switch(elevatorCargo.ItemType) 
                {
                    case ItemType.Consumable:
                        item.SetupAsConsumable(Random.Range(0, int.MaxValue), elevatorCargo.ItemID);
                        break;
                    case ItemType.BigPickup:
                        item.SetupAsBigPickupItem(Random.Range(0, int.MaxValue), elevatorCargo.ItemID, false, 0);
                        break;
                    default: Utils.Logger.Error($"Undefined Item Type {elevatorCargo.ItemType}"); continue;
                }

                __instance.m_itemsToMoveToCargo.Add(item.transform);
                ElevatorRide.Current.m_cargoCageInUse = true;
            }

            //if (RundownManager.ActiveExpedition.MainLayerData.ObjectiveData.DataBlockId == 30000u) // L0EM
            //{
            //    LG_PickupItem fogReps = LG_PickupItem.SpawnGenericPickupItem(ElevatorShaftLanding.CargoAlign);
            //    fogReps.SpawnNode = Builder.GetElevatorArea().m_courseNode;
            //    fogReps.SetupAsConsumable(Random.Range(0, int.MaxValue), 117 /*Fog reps*/);
            //    __instance.m_itemsToMoveToCargo.Add(fogReps.transform);

            //    LG_PickupItem fogBeacon = LG_PickupItem.SpawnGenericPickupItem(ElevatorShaftLanding.CargoAlign);
            //    fogBeacon.SpawnNode = Builder.GetElevatorArea().m_courseNode;
            //    fogBeacon.SetupAsBigPickupItem(Random.Range(0, int.MaxValue), 233, false, 0);
            //    __instance.m_itemsToMoveToCargo.Add(fogBeacon.transform);

            //    ElevatorRide.Current.m_cargoCageInUse = true;
            //}
        }
    }
}
