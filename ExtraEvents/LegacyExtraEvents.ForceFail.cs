using GameData;
using LEGACY.Utils;
using Player;
using Localization;
using LEGACY.LegacyOverride.ForceFail;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static void ToggleFFCheckGroup(WardenObjectiveEventData e)
        {
            ForceFailManager.Current.ToggleCheck(e.Enabled);
            LegacyLogger.Debug($"ToggleFFCheck: Enabled ?: {e.Enabled}");
        }

        private static void AddPlayersInRangeToFFCheckGroup(WardenObjectiveEventData e)
        {
            float range = e.FogTransitionDuration;
            var rangeOrigin = e.Position;
            int checkGroupIndex = e.Count;
            foreach(var player in PlayerManager.PlayerAgentsInLevel)
            {
                if((player.Position - rangeOrigin).magnitude < range)
                {
                    ForceFailManager.Current.AddPlayerToGroup(player.PlayerSlotIndex, checkGroupIndex);
                    LegacyLogger.Debug($"FFCheck: player slot '{player.PlayerSlotIndex}' player_pos: {player.Position}, origin: {rangeOrigin}, distance {(player.Position - rangeOrigin).magnitude} added to FF-check_group_{checkGroupIndex}");
                }
                else
                {
                    LegacyLogger.Warning($"FFCheck: player slot '{player.PlayerSlotIndex}' player_pos: {player.Position}, origin: {rangeOrigin}, distance {(player.Position - rangeOrigin).magnitude} not added");
                }
            }
        }

        private static void AddPlayersOutOfRangeToFFCheckGroup(WardenObjectiveEventData e)
        {
            float range = e.FogTransitionDuration;
            var rangeOrigin = e.Position;
            int checkGroupIndex = e.Count;
            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                if ((player.transform.position - rangeOrigin).magnitude > range)
                {
                    ForceFailManager.Current.AddPlayerToGroup(player.PlayerSlotIndex, checkGroupIndex);
                    LegacyLogger.Debug($"FFCheck: player slot '{player.PlayerSlotIndex}' added to FF-check_group_{checkGroupIndex}");
                }
            }
        }

        private static void ToggleFFCheckOnGroup(WardenObjectiveEventData e)
        {
            int checkGroupIndex = e.Count;
            bool enabled = e.Enabled;
            if (ForceFailManager.Current.ToggleCheckOnGroup(checkGroupIndex, enabled))
            {
                LegacyLogger.Debug($"ToggleCheckOnGroup: FF-check_group_{checkGroupIndex} enabled ?: {enabled}");
            }
            else
            {
                LegacyLogger.Error($"ToggleCheckOnGroup: FF-check_group_{checkGroupIndex} is not defined");
            }
        }

        private static void ResetFFCheck(WardenObjectiveEventData e)
        {
            ForceFailManager.Current.ResetSynced();
            LegacyLogger.Debug("FFCheck: reset-ed");
        }

        private static void ResetFFCheckGroup(WardenObjectiveEventData e)
        {
            int groupIndex = e.Count;
            ForceFailManager.Current.ResetGroupSynced(groupIndex);
        }

        private static void SetExpeditionFailedText(WardenObjectiveEventData e)
        {
            var localizedText = e.CustomSubObjective;
            
            string t = string.Empty;

            if (localizedText.Id != 0)
            {
                t = Text.Get(localizedText.Id);
            }
            else
            {
                t = localizedText.UntranslatedText;
            }

            ForceFailManager.Current.SetExpeditionFailedText(t);
        }

        private static void ResetExpeditionFailedText(WardenObjectiveEventData e)
        {
            ForceFailManager.Current.ResetExpeditionFailedText();
        }
    }
}