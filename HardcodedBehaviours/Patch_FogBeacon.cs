using Gear;
using HarmonyLib;
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

            FogRepeller_Sphere fogRepSmall = new GameObject("FogInstance_Beacon_SmallLayer").AddComponent<FogRepeller_Sphere>();
            fogRepSmall.InfiniteDuration = true;
            fogRepSmall.GrowDuration = 7f;
            fogRepSmall.ShrinkDuration = 7f;
            fogRepSmall.Range = 6f;
            fogRepSmall.Offset = Vector3.zero;

            FogRepeller_Sphere fogRepBig = new GameObject("FogInstance_Beacon_BigLayer").AddComponent<FogRepeller_Sphere>();
            fogRepBig.InfiniteDuration = true;
            fogRepBig.GrowDuration = 3f;
            fogRepBig.ShrinkDuration = 3f;
            fogRepBig.Range = 11f; 
            fogRepBig.Offset = Vector3.zero;

            FogRepeller_Sphere fogRepFake = new GameObject("FogInstance_Beacon_Fake").AddComponent<FogRepeller_Sphere>();
            fogRepFake.InfiniteDuration = false;
            fogRepFake.LifeDuration = 99999f;
            fogRepFake.GrowDuration = 99999f;
            fogRepFake.ShrinkDuration = 99999f;
            fogRepFake.Range = 1f;

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
            fogRepSmall.transform.SetParent(repellerGlobalState.transform, false);
            fogRepBig.transform.SetParent(repellerGlobalState.transform, false);
            repellerGlobalState.m_repellerSphere = fogRepFake;

            // eliminate null ref
            fogRepSmall.m_sphereAllocator = new();
            fogRepBig.m_sphereAllocator = new();

            Interact_Pickup_PickupItem interact = core.m_interact.Cast<Interact_Pickup_PickupItem>();
            interact.InteractDuration = 2.5f;

            repellerGlobalState.CallbackOnStateChange += new System.Action<pCarryItemWithGlobalState_State, pCarryItemWithGlobalState_State, bool>((oldState, newState, isRecall) =>
            {
                switch ((eHeavyFogRepellerStatus)newState.status)
                {
                    case eHeavyFogRepellerStatus.Activated:
                        fogRepSmall?.StartRepelling();

                        // eliminate StopRepelling() exception
                        if ((eHeavyFogRepellerStatus)oldState.status != eHeavyFogRepellerStatus.NoStatus)
                            fogRepBig?.StopRepelling();
                        interact.InteractDuration = 1.0f;
                        break;
                    case eHeavyFogRepellerStatus.Deactivated:
                        fogRepSmall?.StopRepelling();
                        fogRepBig?.StartRepelling();
                        interact.InteractDuration = 2.5f;
                        break;
                }

                if (!isRecall)
                    return;
                fogRepSmall?.KillRepellerInstantly();
                fogRepBig?.KillRepellerInstantly();
            });
        }
    }
}
