using GameData;
using LEGACY.LegacyOverride.Terminal;
using LEGACY.Utils;


namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static void ToggleTerminalState(WardenObjectiveEventData e)
        {
            bool enabled = e.Enabled;

            var terminal = Helper.FindTerminal(e.DimensionIndex, e.Layer, e.LocalIndex, e.Count);
            if (terminal == null)
            {
                LegacyLogger.Error($"ToggleTerminalState: terminal with index {(e.DimensionIndex, e.Layer, e.LocalIndex, e.Count)} not found");
                return;
            }

            var wrapper = TerminalStateManager.Current.Get(terminal);
            if(wrapper == null) 
            {
                LegacyLogger.Error($"ToggleTerminalState: internal error: terminal wrapper not found - {(e.DimensionIndex, e.Layer, e.LocalIndex, e.Count)}");
                return;
            }

            wrapper.ChangeState(enabled);
        }
    }
}