using GameData;
using HarmonyLib;
using LEGACY.Utils;
using LEGACY.LegacyOverride.SecDoorIntText;
using LevelGeneration;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal class Patch_Customize_SecDoor_Interaction_Text
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_SecurityDoor_Locks), nameof(LG_SecurityDoor_Locks.OnDoorState))]
        private static void Post_Customize_SecDoor_Interaction_Text(pDoorState state, LG_SecurityDoor_Locks __instance)
        {

            var levelSetting = SecDoorIntTextOverrideManager.Current.SettingForCurrentLevel;
            if (levelSetting == null) return;

            if (state.status != eDoorStatus.Unlocked && state.status != eDoorStatus.Closed_LockedWithChainedPuzzle) return;

            int i = levelSetting.doorToZones.FindIndex((door) =>
                door.DimensionIndex == __instance.m_door.Gate.DimensionIndex &&
                door.LayerType == __instance.m_door.LinksToLayerType &&
                door.LocalIndex == __instance.m_door.LinkedToZoneData.LocalIndex);

            if (i == -1) return;

            var setting = levelSetting.doorToZones[i];

            LG_SecurityDoor door = __instance.m_door;
            Interact_Timed intOpenDoor = __instance.m_intOpenDoor;

            string Prefix = setting.Prefix;
            string Postfix = setting.Postfix;
            string TextToReplace = setting.TextToReplace;

            if (string.IsNullOrEmpty(Prefix)) Prefix = string.Empty;
            if (string.IsNullOrEmpty(Postfix)) Postfix = string.Empty;
            if (string.IsNullOrEmpty(TextToReplace)) TextToReplace = intOpenDoor.InteractionMessage;

            intOpenDoor.InteractionMessage = Prefix + "\n" + TextToReplace + "\n" + Postfix;

            Logger.Debug($"SecDoorIntTextOverride: Override IntText. {setting.LocalIndex}, {setting.LayerType}, {setting.DimensionIndex}");
        }
    }
}
