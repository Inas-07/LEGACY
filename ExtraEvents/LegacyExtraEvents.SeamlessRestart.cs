using GameData;
using UnityEngine;
using LEGACY.Utils;
using LEGACY.LegacyOverride;
using LevelGeneration;
using ScanPosOverride.Managers;
using Player;
using LEGACY.ExtraEvents.Patches.SeamlessRestart;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static void ToggleSeamlessReload(WardenObjectiveEventData e)
        {
            RestartBiz.SeamlessRestartEnabled = e.Enabled;
        }
    }
}