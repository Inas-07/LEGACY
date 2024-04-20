using GTFO.API;
using ExtraObjectiveSetup.BaseClasses;
using System.Collections.Generic;
using LEGACY.Utils;
using GTFO.API.Utilities;

namespace LEGACY.LegacyOverride.DummyVisual
{
    internal class VisualManager : GenericExpeditionDefinitionManager<VisualGroupDefinition>
    {
        public static VisualManager Current = new VisualManager();

        protected override string DEFINITION_NAME => "DummyVisual";

        private Dictionary<string, VisualGroup> Visuals { get; } = new();

        public void Build(VisualGroupDefinition def)
        {
            if (Visuals.ContainsKey(def.WorldEventObjectFilter))
            {
                LegacyLogger.Error($"Build DummyVisual: found duplicate WorldEventObjectFilter {def.WorldEventObjectFilter}, won't build!");
                return;
            }

            var group = new VisualGroup(def);
            if(group.Setup())
            {
                Visuals[def.WorldEventObjectFilter] = group;
            }
        }

        public void ToggleVisualType(string worldEventObjectFilter, VisualType visualType)
        {
            if(!Visuals.TryGetValue(worldEventObjectFilter, out var vsg))
            {
                LegacyLogger.Error($"ToggleVisualType: Visual '{worldEventObjectFilter}' is not found");
                return;
            }

            vsg.ChangeToState(new() { visualType = visualType });
        }

        private void Clear()
        {
            foreach(var group in Visuals.Values)
            {
                group.Destroy();
            }

            Visuals.Clear();
        }

        private void OnBuildDone()
        {
            if (!definitions.ContainsKey(RundownManager.ActiveExpedition.LevelLayoutData)) return;
            definitions[RundownManager.ActiveExpedition.LevelLayoutData].Definitions.ForEach(Build);
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            base.FileChanged(e);
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;
            if (!definitions.ContainsKey(CurrentMainLevelLayout)) return;

            var defs = definitions[CurrentMainLevelLayout].Definitions;
            foreach(var def in defs)
            {
                if(Visuals.TryGetValue(def.WorldEventObjectFilter, out var vsg))
                {
                    LegacyLogger.Debug($"Rebuilding visual group '{def.WorldEventObjectFilter}'");
                    vsg.ResetupOnDef(def);
                    vsg.ChangeToState(new() { visualType = VisualType.ON });
                }
            }
        }

        private VisualManager() 
        {
            LevelAPI.OnBuildDone += OnBuildDone;
            LevelAPI.OnBuildStart += Clear;
            LevelAPI.OnLevelCleanup += Clear;
        }

        static VisualManager() { }
    }
}
