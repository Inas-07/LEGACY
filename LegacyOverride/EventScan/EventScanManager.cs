using ExtraObjectiveSetup.BaseClasses;
using UnityEngine;
using GTFO.API;
using Il2CppInterop.Runtime.Injection;
using Steamworks;
using System.Collections.Generic;
using LEGACY.Utils;
using SNetwork;

namespace LEGACY.LegacyOverride.EventScan
{
    internal class EventScanManager : GenericExpeditionDefinitionManager<EventScanDefinition>
    {
        public static EventScanManager Current { get; private set; } = new();

        private Dictionary<string, List<EventScanComponent>> eventScans = new();

        protected override string DEFINITION_NAME => "EventScan";

        private void BuildEventScan(EventScanDefinition def)
        {
            var pos = def.Position.ToVector3();
            if (pos == Vector3.zero) return;

            var go = Object.Instantiate(Assets.CircleSensor);
            var comp = go.AddComponent<EventScanComponent>();
            comp.def = def;
            comp.Setup();

            List<EventScanComponent> list = null;
            if (eventScans.ContainsKey(def.WorldEventObjectFilter))
            {
                list = eventScans[def.WorldEventObjectFilter];
            }
            else
            {
                list = new();
                eventScans[def.WorldEventObjectFilter] = list; 
            }

            list.Add(comp);
        }

        public void ToggleEventScanState(string WorldEventObjectFilter, bool active = true)
        {
            if(!eventScans.ContainsKey(WorldEventObjectFilter))
            {
                LegacyLogger.Error($"ToggleEventScanState: {WorldEventObjectFilter} does not correspond to any event scans!");
                return;
            }

            foreach (var comp in eventScans[WorldEventObjectFilter])
            {
                if(!active && comp.StateReplicator.State.Status != EventScanState.Disabled )
                    comp.ChangeState(EventScanState.Disabled);
                else if (active && comp.StateReplicator.State.Status == EventScanState.Disabled ) 
                    comp.ChangeState(EventScanState.Waiting);
            }
        }

        private void Build()
        {
            if (!definitions.ContainsKey(RundownManager.ActiveExpedition.LevelLayoutData)) return;
            var defs = definitions[RundownManager.ActiveExpedition.LevelLayoutData];
            defs.Definitions.ForEach(BuildEventScan);
        }

        private void Clear()
        {
            foreach(var list in eventScans.Values)
            {
                foreach (var comp in list)
                {
                    Object.Destroy(comp.gameObject);
                }
            }

            eventScans.Clear();
        }

        static EventScanManager() { }

        private EventScanManager() {
            LevelAPI.OnBuildDone += Build;
            LevelAPI.OnLevelCleanup += Clear;
        }
    }
}
