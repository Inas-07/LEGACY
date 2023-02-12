using Il2CppSystem.Collections.Generic;
using HarmonyLib;
using GameData;
using LEGACY.Utilities;
using UnityEngine;
using LevelGeneration;

namespace LEGACY.HardcodedBehaviours
{
    [HarmonyPatch]
    class Patch_ChangeElevatorCargo_Hardcoded
    {
        private static bool ForceDisable() => RundownManager.ActiveExpedition.LevelLayoutData == (uint)MainLayerID.L2E2
                || RundownManager.ActiveExpedition.LevelLayoutData == (uint)MainLayerID.L2E3
                || RundownManager.ActiveExpedition.LevelLayoutData == (uint)MainLayerID.L3E2;

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
    }
}
