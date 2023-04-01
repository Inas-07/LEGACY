using HarmonyLib;
using LevelGeneration;
using LEGACY.LegacyOverride.Terminal;
using LEGACY.Utils;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    class Patch_TerminalPositionOverride
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.Setup))]
        private static void Post_ChangeTerminalPosition(LG_ComputerTerminal __instance)
        {
            var terminalPositionOverride = TerminalPositionOverrideManager.Current.GetLevelTerminalPositionOverride(RundownManager.ActiveExpedition.LevelLayoutData);
            if (terminalPositionOverride == null) return;

            int i = terminalPositionOverride.FindIndex((t) =>
                t.DimensionIndex == __instance.SpawnNode.m_dimension.DimensionIndex
                && t.LayerType == __instance.SpawnNode.LayerType
                && t.LocalIndex == __instance.SpawnNode.m_zone.LocalIndex
                // mono-code trick
                && t.TerminalIndex == __instance.SpawnNode.m_zone.TerminalsSpawnedInZone.Count
            );

            if (i == -1) return;

            var _override = terminalPositionOverride[i];

            if (_override.Position.ToVector3() != UnityEngine.Vector3.zeroVector)
            {
                __instance.transform.position = _override.Position.ToVector3();
                __instance.transform.rotation = _override.Rotation.ToQuaternion();
            }

            Logger.Debug($"TerminalPositionOverride: {_override.LocalIndex}, {_override.LayerType}, {_override.DimensionIndex}, TerminalIndex {_override.TerminalIndex}");
        }
    }
}
