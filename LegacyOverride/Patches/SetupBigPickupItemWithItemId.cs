using Gear;
using HarmonyLib;
using LEGACY.LegacyOverride.FogBeacon;
using LEGACY.LegacyOverride.EnemyTagger;
using LevelGeneration;
using Player;
using UnityEngine;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal class SetupBigPickupItemWithItemId
    {

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
            switch (itemId) 
            {
                case 233u: 
                    SetupAsFogBeacon(__instance); break;
                case 234u:
                case 235u:
                case 236u:
                case 237u:
                    EnemyTaggerSettingManager.Current.SetupAsObserver(__instance); 
                    break;
                //case 236u:
                //    ConfigureXRayObserver(__instance);
                //    LegacyLogger.Warning($"ConfigureXRayObserver!");
                //    break;
            }
        }

        static SetupBigPickupItemWithItemId()
        {

        }

        //private static HashSet<IntPtr> xrays = new();

        //private static void ConfigureXRayObserver(LG_PickupItem __instance)
        //{
        //    var xray = __instance.GetComponentInChildren<XRays>(true);
        //    LegacyLogger.Error($"xray isActiveAndEnabled? {xray.isActiveAndEnabled}");

        //    __instance.gameObject.SetActiveRecursively(true);
        //    if (xray.m_renderer == null)
        //    {
        //        xray.m_renderer = __instance.GetComponent<XRayRenderer>();
        //    }

        //    xrays.Add(xray.gameObject.Pointer);
        //}

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(XRays), nameof(XRays.Update))]
        //private static bool Pre_XRayUpdate(XRays __instance)
        //{
        //    if (!xrays.Contains(__instance.gameObject.Pointer)) return true;

        //    if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return false;

        //    int n = Mathf.CeilToInt(__instance.raysPerSecond * Mathf.Min(0.05f, Time.deltaTime));
        //    __instance.Cast(n);
        //    __instance.m_renderer.range = __instance.maxDistance;
        //    __instance.m_renderer.mode = 1;
        //    LegacyLogger.Warning($"{n}, {__instance.m_renderer.range} ,{__instance.m_renderer.mode}");
        //    LegacyLogger.Warning($"{__instance.gameObject.transform.position}");
        //    return false;
        //}

        //static Patch_SetupBigPickupItemWithItemId() {
        //    LevelAPI.OnBuildStart += xrays.Clear;
        //    LevelAPI.OnLevelCleanup += xrays.Clear;
        //}
    }
}
