using GameData;
using LEGACY.LegacyOverride.Music;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static void PlayMusic(WardenObjectiveEventData e)
        {
            MusicStateOverrider.Current.PlayMusic(e.WorldEventObjectFilter);
        }

        private static void StopMusic(WardenObjectiveEventData e)
        {
            MusicStateOverrider.Current.StopMusic(e.WorldEventObjectFilter);
        }
    }
}