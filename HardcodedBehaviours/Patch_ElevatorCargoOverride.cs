using HarmonyLib;
using UnityEngine;
using LevelGeneration;
using LEGACY.LegacyOverride.ElevatorCargo;

namespace LEGACY.HardcodedBehaviours
{
    [HarmonyPatch]
    internal class Patch_ElevatorCargoOverride
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ElevatorCargoCage), nameof(ElevatorCargoCage.SpawnObjectiveItemsInLandingArea))]
        private static bool Pre_ElevatorCargoOverride(ElevatorCargoCage __instance)
        {
            LevelElevatorCargo levelElevatorCargos = ElevatorCargoOverrideManager.Current.GetLevelElevatorCargoItems(RundownManager.ActiveExpedition.LevelLayoutData);
            if (levelElevatorCargos == null) return true;

            if (levelElevatorCargos.ForceDisable)
            {
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
        }
    }
}
