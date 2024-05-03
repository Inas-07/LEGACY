using ExtraObjectiveSetup;
using ExtraObjectiveSetup.BaseClasses;
using FloLib.Networks.Replications;
using GTFO.API;
using LEGACY.Utils;
using SNetwork;

namespace LEGACY.LegacyOverride.ExpeditionSuccessPage
{
    public class SuccessPageCustomizationManager: GenericExpeditionDefinitionManager<SuccessPageCustomization>
    {
        public static SuccessPageCustomizationManager Current { get; } = new();

        private StateReplicator<StateSync> StateReplicator { get; set; }

        protected override string DEFINITION_NAME => "ExpeditionSuccessPage";
        
        //public void ApplyCustomization(SuccessPageCustomization def)
        //{
        //    if (def.OverrideSuccessMusic != 0)
        //    {
        //        MainMenuGuiLayer.Current.PageExpeditionSuccess.m_overrideSuccessMusic = def.OverrideSuccessMusic;
        //    }
        //    MainMenuGuiLayer.Current.PageExpeditionSuccess.m_header.SetText(def.PageHeader);
        //}

        public SuccessPageCustomization CurrentCustomization { get; private set; }

        public void ApplyCustomization(string WorldEventObjectFilter)
        {
            if(!definitions.ContainsKey(CurrentMainLevelLayout))
            {
                LegacyLogger.Error($"SuccessPageCustomization: definition not found for main level layout {CurrentMainLevelLayout}");
                return;
            }

            var defs = definitions[CurrentMainLevelLayout].Definitions;

            var defIndex = defs.FindIndex(def => def.WorldEventObjectFilter.Equals(WorldEventObjectFilter));
            if (defIndex == -1)
            {
                LegacyLogger.Error($"SuccessPageCustomization: customization WorldEventObjectFilter '{WorldEventObjectFilter}' is not found");
                return;
            }

            //ApplyCustomization(def);
            CurrentCustomization = defs[defIndex];
            LegacyLogger.Debug($"SuccessPageCustomization: customization with WorldEventObjectFilter '{WorldEventObjectFilter}' applied");
            if(SNet.IsMaster)
            {
                StateReplicator?.SetState(new() { index = defIndex });
            }
        }

        private void SetDefaultCustomization()
        {
            if (!definitions.ContainsKey(CurrentMainLevelLayout))
            {
                LegacyLogger.Error($"SuccessPageCustomization: definition not found for main level layout {CurrentMainLevelLayout}");
                return;
            }

            var defs = definitions[CurrentMainLevelLayout].Definitions;
            var defIndex = defs.FindIndex(def => def.SetAsDefaultCustomization == true);
            if (defIndex != -1)
            {
                CurrentCustomization = defs[defIndex];
                //ApplyCustomization(defIndex);
                LegacyLogger.Debug($"SuccessPageCustomization: default customization with WorldEventObjectFilter '{CurrentCustomization.WorldEventObjectFilter}' applied");

                if (SNet.IsMaster)
                {
                    StateReplicator?.SetState(new() { index = defIndex });
                }
            }
        }

        private void OnStateChanged(StateSync oldState, StateSync newState, bool isRecall)
        {
            if (!isRecall) return;

            if (definitions.TryGetValue(CurrentMainLevelLayout, out var defs))
            {
                if(0 <= newState.index && newState.index < defs.Definitions.Count)
                {
                    CurrentCustomization = defs.Definitions[newState.index];
                }
            }
        }

        private void Clear()
        {
            CurrentCustomization = null;
        }

        private SuccessPageCustomizationManager() 
        {
            LevelAPI.OnEnterLevel += SetDefaultCustomization;
            LevelAPI.OnBuildStart += Clear;
            LevelAPI.OnLevelCleanup += Clear;

            EventAPI.OnAssetsLoaded += () =>
            {
                uint id = EOSNetworking.AllotForeverReplicatorID();
                if(id == EOSNetworking.INVALID_ID)
                {
                    LegacyLogger.Error("SuccessPageCustomizationManager: Cannot allot replicator!");
                    return;
                }

                StateReplicator = StateReplicator<StateSync>.Create(id, new() { index = 0 }, LifeTimeType.Forever);
                StateReplicator.OnStateChanged += OnStateChanged;
            };
        }

        static SuccessPageCustomizationManager() { }
    }
}
