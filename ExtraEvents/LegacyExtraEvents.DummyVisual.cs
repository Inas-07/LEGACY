using GameData;
using LEGACY.LegacyOverride.DummyVisual;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static void ToggleDummyVisual(WardenObjectiveEventData e)
        {
            string visualName = e.WorldEventObjectFilter;
            VisualType visualType = (VisualType)e.SustainedEventSlotIndex;

            VisualManager.Current.ToggleVisualType(visualName, visualType);
        }
    }
}