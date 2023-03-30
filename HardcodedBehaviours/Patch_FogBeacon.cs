using Gear;
using HarmonyLib;
using LEGACY.LegacyOverride.FogBeacon;
using LevelGeneration;
using Player;
using UnityEngine;

namespace LEGACY.HardcodedBehaviours
{
    [HarmonyPatch]
    internal class Patch_FogBeacon
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_PickupItem), nameof(LG_PickupItem.SetupBigPickupItemWithItemId))]
        private static void Post_SpawnBeacon(LG_PickupItem __instance, uint itemId)
        {
            if (itemId != 233u) return;

            //FogRepeller_Sphere fogRepSmall = new GameObject("FogInstance_Beacon_SmallLayer").AddComponent<FogRepeller_Sphere>();
            //fogRepSmall.InfiniteDuration = true;
            //fogRepSmall.GrowDuration = 7f;
            //fogRepSmall.ShrinkDuration = 7f;
            //fogRepSmall.Range = 6f;
            //fogRepSmall.Offset = Vector3.zero;

            //FogRepeller_Sphere fogRepBig = new GameObject("FogInstance_Beacon_BigLayer").AddComponent<FogRepeller_Sphere>();
            //fogRepBig.InfiniteDuration = true;
            //fogRepBig.GrowDuration = 3f;
            //fogRepBig.ShrinkDuration = 3f;
            //fogRepBig.Range = 11f; 
            //fogRepBig.Offset = Vector3.zero;

            FogRepeller_Sphere fogRepFake = new GameObject("FogInstance_Beacon_Fake").AddComponent<FogRepeller_Sphere>();
            fogRepFake.InfiniteDuration = false;
            fogRepFake.LifeDuration = 99999f;
            fogRepFake.GrowDuration = 99999f;
            fogRepFake.ShrinkDuration = 99999f;
            fogRepFake.Range = 1f;

            var setting = FogBeaconSettingManager.Current.SettingForCurrentLevel;
            FogRepeller_Sphere fogRepHold = new GameObject("FogInstance_Beacon_SmallLayer").AddComponent<FogRepeller_Sphere>();
            fogRepHold.InfiniteDuration = setting.RSHold.InfiniteDuration;
            fogRepHold.GrowDuration = setting.RSHold.GrowDuration;
            fogRepHold.ShrinkDuration = setting.RSHold.ShrinkDuration;
            fogRepHold.Range = setting.RSHold.Range;
            fogRepHold.Offset = Vector3.zero;

            FogRepeller_Sphere fogRepPlaced = new GameObject("FogInstance_Beacon_BigLayer").AddComponent<FogRepeller_Sphere>();
            fogRepPlaced.InfiniteDuration = setting.RSPlaced.InfiniteDuration;
            fogRepPlaced.GrowDuration = setting.RSPlaced.GrowDuration;
            fogRepPlaced.ShrinkDuration = setting.RSPlaced.ShrinkDuration;
            fogRepPlaced.Range = setting.RSPlaced.Range;
            fogRepPlaced.Offset = Vector3.zero;

            CarryItemPickup_Core core = __instance.m_root.GetComponentInChildren<CarryItemPickup_Core>();

            HeavyFogRepellerPickup fogRepellerPickup = core.Cast<HeavyFogRepellerPickup>();
            iCarryItemWithGlobalState itemWithGlobalState;
            byte index2;
            if (CarryItemWithGlobalStateManager.TryCreateItemInstance(eCarryItemWithGlobalStateType.FogRepeller, __instance.m_root, out itemWithGlobalState, out index2))
            {
                pItemData_Custom customData = fogRepellerPickup.GetCustomData() with
                {
                    byteId = index2
                };
                fogRepellerPickup.SetCustomData(customData, true);
            }

            HeavyFogRepellerGlobalState repellerGlobalState = itemWithGlobalState.Cast<HeavyFogRepellerGlobalState>();
            fogRepHold.transform.SetParent(repellerGlobalState.transform, false);
            fogRepPlaced.transform.SetParent(repellerGlobalState.transform, false);
            repellerGlobalState.m_repellerSphere = fogRepFake;

            // eliminate null ref
            fogRepHold.m_sphereAllocator = new();
            fogRepPlaced.m_sphereAllocator = new();

            Interact_Pickup_PickupItem interact = core.m_interact.Cast<Interact_Pickup_PickupItem>();
            interact.InteractDuration = setting.TimeToPickup;

            repellerGlobalState.CallbackOnStateChange += new System.Action<pCarryItemWithGlobalState_State, pCarryItemWithGlobalState_State, bool>((oldState, newState, isRecall) =>
            {
                switch ((eHeavyFogRepellerStatus)newState.status)
                {
                    case eHeavyFogRepellerStatus.Activated:
                        fogRepHold?.StartRepelling();

                        // eliminate StopRepelling() exception
                        if ((eHeavyFogRepellerStatus)oldState.status != eHeavyFogRepellerStatus.NoStatus)
                            fogRepPlaced?.StopRepelling();
                        interact.InteractDuration = setting.TimeToPlace;
                        break;
                    case eHeavyFogRepellerStatus.Deactivated:
                        fogRepHold?.StopRepelling();
                        fogRepPlaced?.StartRepelling();
                        interact.InteractDuration = setting.TimeToPickup;
                        break;
                }

                if (!isRecall)
                    return;
                fogRepHold?.KillRepellerInstantly();
                fogRepPlaced?.KillRepellerInstantly();
            });
        }
    }
}
