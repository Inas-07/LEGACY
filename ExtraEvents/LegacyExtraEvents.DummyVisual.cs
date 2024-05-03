using GameData;
using LEGACY.LegacyOverride.DummyVisual;
using LEGACY.LegacyOverride.DummyVisual.VisualGOAnimation;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static void ToggleDummyVisual(WardenObjectiveEventData e)
        {
            string visualName = e.WorldEventObjectFilter;
            VisualAnimationType visualType = (VisualAnimationType)e.SustainedEventSlotIndex;

            VisualManager.Current.ToggleVisualAnimationType(visualName, visualType);
        }
    }
}