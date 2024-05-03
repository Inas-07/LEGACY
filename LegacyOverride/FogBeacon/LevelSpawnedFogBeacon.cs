using AIGraph;
using GameData;
using GTFO.API;
using LEGACY.ExtraEvents;
using LEGACY.Utils;
using LevelGeneration;
using UnityEngine;

namespace LEGACY.LegacyOverride.FogBeacon
{
    public class LevelSpawnedFogBeacon
    {
        public static uint LSFB_ITEM_DB_ID { get; private set; } = 0;

        public static bool HasFogBeaconItemDBDefinition => LSFB_ITEM_DB_ID != 0;

        public string WorldEventObjectFilter => def?.WorldEventObjectFilter ?? string.Empty;

        public LevelSpawnedFogBeaconSettings def { get; private set; }

        public HeavyFogRepellerGlobalState GlobalState { get; private set; }

        public LG_PickupItem LG_PickupItem { get; private set; }

        public NavMarker NavMarker { get; private set; }

        public Vector3 Position => LG_PickupItem?.transform.position ?? Vector3.zero;

        public Color NAV_MARKER_COLOR { get; } = new Color(1f, 193f / 255f, 37f / 255f);

        public static LevelSpawnedFogBeacon Instantiate(eDimensionIndex dimensionIndex, LG_LayerType layer, eLocalZoneIndex localIndex, LevelSpawnedFogBeaconSettings def)
        {
            if (!HasFogBeaconItemDBDefinition)
            {
                LegacyLogger.Error($"LevelSpawnedFogBeaconManager: ItemDatablock Definition of vanilla Fog Repeller Turbine is not found...");
                return null;
            }

            if (!Builder.CurrentFloor.TryGetZoneByLocalIndex(dimensionIndex, layer, localIndex, out var zone) || zone == null
                || def.AreaIndex < 0 || def.AreaIndex >= zone.m_areas.Count)
            {
                LegacyLogger.Error($"LevelSpawnedFogBeacon: cannot find {(dimensionIndex, layer, localIndex)}, Area_{(char)('A' + def.AreaIndex)}");
                return null;
            }

            var node = zone.m_areas[def.AreaIndex].m_courseNode;
            LevelSpawnedFogBeacon ret = new(def, node);
            ret.def = def;


            return ret;
        }

        internal void Destroy()
        {
            GameObject.Destroy(LG_PickupItem.m_root.gameObject);
            GlobalState = null;
            LG_PickupItem = null;
            def = null;
        }

        private LevelSpawnedFogBeacon(LevelSpawnedFogBeaconSettings def, AIG_CourseNode node)
        {
            this.def = def;
            GameObject dummyGO = new GameObject($"LSBF_{def.WorldEventObjectFilter}-Area_{(char)('A' + def.AreaIndex)}");
            dummyGO.transform.SetPositionAndRotation(def.Position.ToVector3(), Quaternion.identity);
            LG_PickupItem = LG_PickupItem.SpawnGenericPickupItem(dummyGO.transform);
            LG_PickupItem.SpawnNode = node;

            // NOTE: as long as the id is not the id of "Carry_FogBeacon - ConstantFog"will reuse code in patch method: `Post_SetupBigPickupItemWithItemId`

            var __index = CarryItemWithGlobalStateManager.Current.m_carryItemGlobalStatesInstancesPerType[0].Count;
            LG_PickupItem.SetupAsBigPickupItem(
                randomSeed: 1,
                itemId: LSFB_ITEM_DB_ID,
                isWardenObjectiveItem: false,
                objectiveChainIndex: -1);

            var core = LG_PickupItem.m_root.GetComponentInChildren<CarryItemPickup_Core>();
            var sync = core.m_sync.Cast<LG_PickupItem_Sync>();
            if (sync != null)
            {
                sync.m_stateReplicator.State.placement.node.Set(LG_PickupItem.SpawnNode);
            }

            Interact_Pickup_PickupItem interact = core.m_interact.Cast<Interact_Pickup_PickupItem>();
            interact.gameObject.SetActive(false);

            var iterminalItem = LG_PickupItem.GetComponentInChildren<iTerminalItem>();
            if (iterminalItem != null)
            {
                LG_LevelInteractionManager.DeregisterTerminalItem(iterminalItem);
            }

            NavMarker = GuiManager.NavMarkerLayer.PrepareGenericMarker(LG_PickupItem.gameObject);
            if (NavMarker != null)
            {
                NavMarker.SetColor(NAV_MARKER_COLOR);
                NavMarker.SetStyle(eNavMarkerStyle.LocationBeaconNoText);
                NavMarker.SetVisible(false);
            }

            if (!CarryItemWithGlobalStateManager.TryGetItemInstance(0, (byte)__index, out var carryItemWithGlobalState))
            {
                LegacyLogger.Error($"LevelSpawnedFogBeaconManager: Didn't find GlobalState of '{def.WorldEventObjectFilter}'");
                return;
            }

            //interact.enabled = false;
            GlobalState = carryItemWithGlobalState.Cast<HeavyFogRepellerGlobalState>();

            var repellerSphere = GlobalState.m_repellerSphere;
            repellerSphere.GrowDuration = def.GrowDuration;
            repellerSphere.ShrinkDuration = def.ShrinkDuration;
            repellerSphere.Range = def.Range;

            GlobalState.CallbackOnStateChange += new System.Action<pCarryItemWithGlobalState_State, pCarryItemWithGlobalState_State, bool>((oldState, newState, isRecall) =>
            {
                    eHeavyFogRepellerStatus status = (eHeavyFogRepellerStatus)newState.status;

                    //bool enabled = false;
                    switch (status)
                    {
                        case eHeavyFogRepellerStatus.NoStatus:
                        case eHeavyFogRepellerStatus.Deactivated:
                            //enabled = false; 
                            NavMarker.SetVisible(false);
                            break;

                        case eHeavyFogRepellerStatus.Activated:
                            NavMarker.SetVisible(true);
                            if (isRecall)
                            {
                                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(
                                    new WardenObjectiveEventData()
                                    {
                                        Type = (eWardenObjectiveEventType)LegacyExtraEvents.EventType.ToggleLSFBState,
                                        WorldEventObjectFilter = WorldEventObjectFilter,
                                        Enabled = true,
                                        Delay = 1.1f // KillRepellerInstantly() makes repeller enter `Disabled` state for 1.0f seconds, so skipping it is required.
                                    },
                                    trigger: eWardenObjectiveEventTrigger.None,
                                    ignoreTrigger: true
                                );
                            }
                            break;
                    }
            });
        }

        private static void FindFogTurbineItemDBID()
        {
            if (!HasFogBeaconItemDBDefinition)
            {
                LSFB_ITEM_DB_ID = GameDataBlockBase<ItemDataBlock>.GetBlock("Carry_HeavyFogRepeller")?.persistentID ?? 0;
                if (LSFB_ITEM_DB_ID == 0)
                {
                    LegacyLogger.Error($"LevelSpawnedFogBeaconManager: ItemDatablock Definition of vanilla Fog Repeller Turbine is not found...");
                }
            }
        }

        static LevelSpawnedFogBeacon()
        {
            FindFogTurbineItemDBID();
            LevelAPI.OnBuildStart += FindFogTurbineItemDBID;
        }
    }
}
