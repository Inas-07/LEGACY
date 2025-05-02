using GameData;
using UnityEngine;
using LEGACY.Utils;
using LEGACY.LegacyOverride;
using LevelGeneration;
using ScanPosOverride.Managers;
using Player;
using LEGACY.ExtraEvents.Patches;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static void ToggleSeamlessReload(WardenObjectiveEventData e)
        {
            SeamlessRestart.SeamlessRestartEnabled = e.Enabled;
        }
    }
}