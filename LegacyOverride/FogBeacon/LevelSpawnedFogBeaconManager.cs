using ExtraObjectiveSetup.BaseClasses;
using GameData;
using GTFO.API;
using LEGACY.Utils;
using LevelGeneration;
using SNetwork;
using System.Collections.Generic;
using UnityEngine;

namespace LEGACY.LegacyOverride.FogBeacon
{
    public class LevelSpawnedFogBeaconManager : ZoneDefinitionManager<LevelSpawnedFogBeacon>
    {
        public static LevelSpawnedFogBeaconManager Current { get; } = new();

        private Dictionary<string, (LG_PickupItem pickUpItem, HeavyFogRepellerGlobalState globalState)> LevelSpawnedFogBeacons { get; } = new();

        private Dictionary<System.IntPtr, LevelSpawnedFogBeaconSettings> LSFBGlobalStatesSet = new();

        private uint LSFB_ITEM_DB_ID = 0;

        public bool HasFogBeaconItemDBDefinition => LSFB_ITEM_DB_ID != 0;

        protected override string DEFINITION_NAME => "LevelSpawnedFogBeacon";

        private void Build(LevelSpawnedFogBeacon def)
        {
            if (!HasFogBeaconItemDBDefinition) return;

            if (!Builder.CurrentFloor.TryGetZoneByLocalIndex(def.DimensionIndex, def.LayerType, def.LocalIndex, out var zone) || zone == null)
            {
                LegacyLogger.Error($"LevelSpawnedFogBeaconManager: cannot find {def.GlobalZoneIndexTuple()}");
                return;
            }

            foreach (var d in def.SpawnedBeaconsInZone)
            {
                if (d.AreaIndex < 0 || d.AreaIndex >= zone.m_areas.Count)
                {
                    LegacyLogger.Error($"LevelSpawnedFogBeaconManager: cannot find {def.GlobalZoneIndexTuple()}, Area_{'A' + d.AreaIndex}");
                    continue;
                }

                if (d.WorldEventObjectFilter == null || d.WorldEventObjectFilter == string.Empty || LevelSpawnedFogBeacons.ContainsKey(d.WorldEventObjectFilter))
                {
                    LegacyLogger.Error($"LevelSpawnedFogBeaconManager: WorldEventObjectFilter '{d.WorldEventObjectFilter}' is either unassigned or has already been assigned.");
                    continue;
                }

                GameObject dummyGO = new GameObject($"LevelSpawnFogBeacon-{def.GlobalZoneIndexTuple()}_Area_{'A' + d.AreaIndex}-{d.WorldEventObjectFilter}");
                dummyGO.transform.SetPositionAndRotation(d.Position.ToVector3(), Quaternion.identity);
                LG_PickupItem lg_PickupItem = LG_PickupItem.SpawnGenericPickupItem(dummyGO.transform);
                lg_PickupItem.SpawnNode = zone.m_areas[d.AreaIndex].m_courseNode;

                // NOTE: as long as the id is not the id of "Carry_FogBeacon - ConstantFog"will reuse code in patch method: `Post_SetupBigPickupItemWithItemId`

                var __index = CarryItemWithGlobalStateManager.Current.m_carryItemGlobalStatesInstancesPerType[0].Count;
                lg_PickupItem.SetupAsBigPickupItem(
                    randomSeed: 1,
                    itemId: LSFB_ITEM_DB_ID,
                    isWardenObjectiveItem: false,
                    objectiveChainIndex: -1);

                var core = lg_PickupItem.m_root.GetComponentInChildren<CarryItemPickup_Core>();

                Interact_Pickup_PickupItem interact = core.m_interact.Cast<Interact_Pickup_PickupItem>();
                interact.gameObject.SetActive(false);    
                //interact.enabled = false;

                if(!CarryItemWithGlobalStateManager.TryGetItemInstance(0, (byte)__index, out var carryItemWithGlobalState))
                {
                    LegacyLogger.Error($"LevelSpawnedFogBeaconManager: Didn't find GlobalState of '{d.WorldEventObjectFilter}'");
                    continue;
                }

                HeavyFogRepellerGlobalState globalState = carryItemWithGlobalState.Cast<HeavyFogRepellerGlobalState>();

                var repellerSphere = globalState.m_repellerSphere;
                repellerSphere.GrowDuration = d.GrowDuration;
                repellerSphere.ShrinkDuration = d.ShrinkDuration;
                repellerSphere.Range = d.Range;

                LevelSpawnedFogBeacons[d.WorldEventObjectFilter] = (lg_PickupItem, globalState);
                LSFBGlobalStatesSet[globalState.Pointer] = d;

                var iterminalItem = lg_PickupItem.GetComponentInChildren<iTerminalItem>();
                if (iterminalItem != null)
                {
                    LG_LevelInteractionManager.DeregisterTerminalItem(iterminalItem);
                }

                LegacyLogger.Debug($"LevelSpawnedFogBeaconManager: spawned '{d.WorldEventObjectFilter}' in {def.GlobalZoneIndexTuple()}, Area_{(char)('A' + d.AreaIndex)}, Range: {repellerSphere.Range}");
            }
        }

        internal LevelSpawnedFogBeaconSettings GetLSFBDef(HeavyFogRepellerGlobalState h) => LSFBGlobalStatesSet.TryGetValue(h.Pointer, out var def) ? def : null;

        public void ToggleLSFBState(string worldEventgObjectFilter, bool enable)
        {
            if(!LevelSpawnedFogBeacons.TryGetValue(worldEventgObjectFilter, out var LSFB))
            {
                LegacyLogger.Error($"ToggleLSFBState: '{worldEventgObjectFilter}' is not defined");
                return;
            }

            if(SNet.IsMaster)
            {
                (LG_PickupItem item, HeavyFogRepellerGlobalState globalState) = LSFB;
                globalState.AttemptInteract(new() {
                    type = (byte)(enable ? eHeavyFogRepellerInteraction.Activate: eHeavyFogRepellerInteraction.Deactivate),
                    owner = eCarryItemWithGlobalStateOwner.StaticPosition,
                    staticPosition = item.transform.position
                });
            }
        }

        private void Clear()
        {
            foreach((LG_PickupItem pickUpItem, HeavyFogRepellerGlobalState _) in LevelSpawnedFogBeacons.Values)
            {
                Object.Destroy(pickUpItem.m_root.gameObject);
            }
            LSFBGlobalStatesSet.Clear();
            LevelSpawnedFogBeacons.Clear();
        }

        private void BuildLevelSpawnedFogBeacons()
        {
            if (!HasFogBeaconItemDBDefinition) return;
            if (!definitions.ContainsKey(RundownManager.ActiveExpedition.LevelLayoutData)) return;
            definitions[RundownManager.ActiveExpedition.LevelLayoutData].Definitions.ForEach(Build);
        }

        private void FindFogTurbineItemDBID()
        {
            if (!HasFogBeaconItemDBDefinition)
            {
                LSFB_ITEM_DB_ID = GameDataBlockBase<ItemDataBlock>.GetBlock("Carry_HeavyFogRepeller")?.persistentID ?? 0;
                if(LSFB_ITEM_DB_ID == 0)
                {
                    LegacyLogger.Error($"LevelSpawnedFogBeaconManager: ItemDatablock Definition of vanilla Fog Repeller Turbine is not found...");
                }
            }
        }

        private LevelSpawnedFogBeaconManager() 
        {
            LevelAPI.OnBuildStart += FindFogTurbineItemDBID;
            LevelAPI.OnBuildStart += Clear;
            LevelAPI.OnLevelCleanup += Clear;
            LevelAPI.OnBuildDone += BuildLevelSpawnedFogBeacons;
        }

        static LevelSpawnedFogBeaconManager() { }
    }
}
