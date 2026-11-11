using BepInEx.Unity.IL2CPP.Utils.Collections;
using ExtraObjectiveSetup.BaseClasses;
using GameData;
using GTFO.API;
using LEGACY.Utils;
using Localization;
using System;
using System.Collections;
using UnityEngine;

namespace LEGACY.LegacyOverride.ExpeditionIntelNotification
{
    public class ExpeditionIntelNotifier : GenericExpeditionDefinitionManager<ExpeditionIntel>
    {
        public static ExpeditionIntelNotifier Current { get; private set; } = new();

        private System.Random rand { get; } = new();

        protected override string DEFINITION_NAME => "ExpeditionIntel";

        private void ShowIntelForExpedition(ExpeditionIntel intel)
        {
            LegacyLogger.Warning($"{intel.Header.Id}, {intel.Header.UntranslatedText}, {intel.Header.ShouldLocalize}");
            var pop = GlobalPopupMessageManager.ShowPopup(new PopupMessage()
            {
                BlinkInContent = intel.BlinkInContent,
                BlinkTimeInterval = intel.BlinkTimeInterval,
                Header = intel.Header,
                UpperText = intel.Text,
                LowerText = string.Empty,
                PopupType = PopupType.BoosterImplantMissed,
                OnCloseCallback = (Il2CppSystem.Action)(() => { })
            });
        }

        internal void OnLevelSelected(ExpeditionInTierData expData)
        {
            if (definitions.ContainsKey(expData.LevelLayoutData) && definitions[expData.LevelLayoutData].Definitions.Count > 0)
            {
                var intels = definitions[expData.LevelLayoutData].Definitions;
                ShowIntelForExpedition(intels[rand.Next(0, intels.Count)]);
                LegacyLogger.Warning("ShowIntelForExpedition");
            }
        }

        private ExpeditionIntelNotifier() 
        {
            LevelAPI.OnLevelSelected += (_, _, e) => OnLevelSelected(e);
        }

        static ExpeditionIntelNotifier() { }
    }
}
