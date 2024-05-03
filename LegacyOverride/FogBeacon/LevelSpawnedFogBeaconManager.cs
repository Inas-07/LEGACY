using ExtraObjectiveSetup.BaseClasses;
using GameData;
using GTFO.API;
using LEGACY.ExtraEvents;
using LEGACY.Utils;
using LevelGeneration;
using PlayFab.AuthenticationModels;
using SNetwork;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using UnityEngine;

namespace LEGACY.LegacyOverride.FogBeacon
{
    public class LevelSpawnedFogBeaconManager : ZoneDefinitionManager<LevelSpawnedFogBeaconDefinition>
    {
        public static LevelSpawnedFogBeaconManager Current { get; } = new();

        private Dictionary<string, LevelSpawnedFogBeacon> LevelSpawnedFogBeacons { get; } = new();

        // key: HeavyFogRepellerGlobalState.Pointer
        private Dictionary<System.IntPtr, LevelSpawnedFogBeaconSettings> LSFBGlobalStatesSet = new();

        protected override string DEFINITION_NAME => "LevelSpawnedFogBeacon";

        private void Build(LevelSpawnedFogBeaconDefinition def)
        {
            foreach (var d in def.SpawnedBeaconsInZone)
            {
                if (d.WorldEventObjectFilter == null || d.WorldEventObjectFilter == string.Empty || LevelSpawnedFogBeacons.ContainsKey(d.WorldEventObjectFilter))
                {
                    LegacyLogger.Error($"LevelSpawnedFogBeaconManager: WorldEventObjectFilter '{d.WorldEventObjectFilter}' is either unassigned or has already been assigned.");
                    continue;
                }

                var LSFB = LevelSpawnedFogBeacon.Instantiate(def.DimensionIndex, def.LayerType, def.LocalIndex, d);
                if(LSFB == null)
                {
                    continue;
                }

                LevelSpawnedFogBeacons[d.WorldEventObjectFilter] = LSFB;
                LSFBGlobalStatesSet[LSFB.GlobalState.Pointer] = d;
                LegacyLogger.Debug($"LevelSpawnedFogBeaconManager: spawned '{d.WorldEventObjectFilter}' in {def.GlobalZoneIndexTuple()}, Area_{(char)('A' + d.AreaIndex)}");
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
                LSFB.GlobalState.AttemptInteract(new() {
                    type = (byte)(enable ? eHeavyFogRepellerInteraction.Activate: eHeavyFogRepellerInteraction.Deactivate),
                    owner = eCarryItemWithGlobalStateOwner.StaticPosition,
                    staticPosition = LSFB.Position
                });
            }
        }

        private void Clear()
        {
            foreach(var LSFB in LevelSpawnedFogBeacons.Values)
            {
                LSFB.Destroy();
            }
            LSFBGlobalStatesSet.Clear();
            LevelSpawnedFogBeacons.Clear();
        }

        private void BuildLevelSpawnedFogBeacons()
        {
            if (!definitions.ContainsKey(RundownManager.ActiveExpedition.LevelLayoutData)) return;
            definitions[RundownManager.ActiveExpedition.LevelLayoutData].Definitions.ForEach(Build);
        }


        private LevelSpawnedFogBeaconManager() 
        {
            LevelAPI.OnBuildStart += Clear;
            LevelAPI.OnLevelCleanup += Clear;
            LevelAPI.OnBuildDone += BuildLevelSpawnedFogBeacons;
        }

        static LevelSpawnedFogBeaconManager() { }
    }
}
