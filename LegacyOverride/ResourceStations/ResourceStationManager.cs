using ExtraObjectiveSetup.BaseClasses;
using ExtraObjectiveSetup.ExtendedWardenEvents;
using GameData;
using GTFO.API;
using LEGACY.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEGACY.LegacyOverride.ResourceStations
{
    public class ResourceStationManager : GenericExpeditionDefinitionManager<ResourceStationDefinition>
    {
        public static ResourceStationManager Current { get; private set; } = new();

        protected override string DEFINITION_NAME => "ResourceStation";

        private Dictionary<string, ResourceStation> Stations { get; } = new();

        private void Build(ResourceStationDefinition def)
        {
            if(Stations.ContainsKey(def.WorldEventObjectFilter))
            {
                LegacyLogger.Error($"ResourceStationManager: WorldEventObjectFilter '{def.WorldEventObjectFilter}' is already used");
                return;
            }

            ResourceStation station = null;
            switch (def.StationType)
            {
                case StationType.MEDI:
                    station = MediStation.Instantiate(def);
                    break;

                case StationType.AMMO:
                    station = AmmoStation.Instantiate(def);
                    break;

                case StationType.TOOL:
                    station = ToolStation.Instantiate(def);
                    break;
                default:
                    LegacyLogger.Error($"ResourceStation {def.StationType} is unimplemented");
                    return;
            }

            if (station != null)
            {
                Stations[def.WorldEventObjectFilter] = station;
                LegacyLogger.Debug($"ResourceStation '{def.WorldEventObjectFilter}' instantiated");
            }
        }

        private void BuildStations()
        {
            if(definitions.TryGetValue(CurrentMainLevelLayout, out var defs))
            {
                defs.Definitions.ForEach(Build);
            }
        }

        private void Clear()
        {
            foreach(var station in Stations.Values)
            {
                station.Destroy();
            }
            Stations.Clear();
        }

        private ResourceStationManager() 
        {
            LevelAPI.OnBuildStart += () => { Clear(); };
            LevelAPI.OnLevelCleanup += Clear;
            LevelAPI.OnBuildDone += BuildStations;
        }

        private static void SetEnabled(WardenObjectiveEventData e)
        {
            if (Current.Stations.TryGetValue(e.WorldEventObjectFilter, out var station))
            {
                station.StateReplicator.SetState(new() { 
                    LastInteractedPlayer = -1,
                    CurrentCooldownTime = station.State.CurrentCooldownTime,
                    RemainingUseTime = station.State.RemainingUseTime,
                    Enabled = e.Enabled,
                });
            }
            else 
            {
                LegacyLogger.Error($"ResourceStationManager: WorldEventObjectFilter '{e.WorldEventObjectFilter}' not found");
            }
        }

        public enum RSEvents
        {
            ResourceStation_SetEnabled = 156,
        }

        static ResourceStationManager() 
        {
            EOSWardenEventManager.Current.AddEventDefinition(RSEvents.ResourceStation_SetEnabled.ToString(), (uint)RSEvents.ResourceStation_SetEnabled, SetEnabled);
        }
    }
}
