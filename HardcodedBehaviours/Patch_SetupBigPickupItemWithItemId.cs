using Gear;
using HarmonyLib;
using LEGACY.LegacyOverride.FogBeacon;
using LEGACY.LegacyOverride.EnemyTagger;
using LevelGeneration;
using Player;
using UnityEngine;
using LEGACY.Components;
using AK;
using CullingSystem;
using FX_EffectSystem;

namespace LEGACY.HardcodedBehaviours
{
    [HarmonyPatch]
    internal class Patch_SetupBigPickupItemWithItemId
    {
        // enemy tagger
        private static void SetupAsObserver(LG_PickupItem __instance)
        {
            CarryItemPickup_Core core = __instance.m_root.GetComponentInChildren<CarryItemPickup_Core>();
            Interact_Pickup_PickupItem interact = core.m_interact.Cast<Interact_Pickup_PickupItem>();
            LG_PickupItem_Sync sync = core.m_sync.Cast<LG_PickupItem_Sync>();
            
            EnemyTagger tagger = core.gameObject.AddComponent<EnemyTagger>();
            tagger.Parent = core;
            tagger.gameObject.SetActive(true);

            var setting = EnemyTaggerSettingManager.Current.SettingForCurrentLevel;
            interact.InteractDuration = setting.TimeToPickup;

            tagger.ApplySetting(setting);

            sync.OnSyncStateChange += new System.Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>((status, placement, playerAgent, isRecall) => {
                switch (status)
                {
                    case ePickupItemStatus.PlacedInLevel:
                        tagger.PickedByPlayer = null;
                        tagger.ChangeState(eEnemyTaggerState.Active);
                        interact.InteractDuration = setting.TimeToPickup;
                        break;

                    case ePickupItemStatus.PickedUp:
                        tagger.gameObject.SetActive(true);
                        tagger.PickedByPlayer = playerAgent;
                        tagger.ChangeState(eEnemyTaggerState.Inactive);
                        interact.InteractDuration = setting.TimeToPlace;

                        break;
                }
            });
        }

        private static void SetupAsFogBeacon(LG_PickupItem __instance)
        {
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_PickupItem), nameof(LG_PickupItem.SetupBigPickupItemWithItemId))]
        private static void Post_SetupBigPickupItemWithItemId(LG_PickupItem __instance, uint itemId)
        {
            if (itemId == 233u)
            {
                SetupAsFogBeacon(__instance); 
            }

            else if(itemId == 234u)
            {
                SetupAsObserver(__instance);
            }
        }
    }
}
