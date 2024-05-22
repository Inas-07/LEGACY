//using GameData;
//using HarmonyLib;
//using LEGACY.Utils;
//using LEGACY.LegacyOverride.SecDoorIntText;
//using LevelGeneration;
//using PlayFab.AuthenticationModels;

//namespace LEGACY.LegacyOverride.Patches
//{
//    [HarmonyPatch]
//    internal class CustomizeSecDoorInteractionText
//    {
//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(LG_SecurityDoor_Locks), nameof(LG_SecurityDoor_Locks.OnDoorState))]
//        private static void Post_Customize_SecDoor_Interaction_Text(pDoorState state, LG_SecurityDoor_Locks __instance)
//        {
//            LG_SecurityDoor door = __instance.m_door;
//            var dim = door.Gate.DimensionIndex;
//            var layer = door.LinksToLayerType;
//            var localIndex = door.LinkedToZoneData.LocalIndex;
//            var setting = SecDoorIntTextOverrideManager.Current.GetDefinition(dim, layer, localIndex);

//            if (setting == null || setting.GlitchMode != GlitchMode.None) return;

//            //if (state.status != eDoorStatus.Unlocked && state.status != eDoorStatus.Closed_LockedWithChainedPuzzle) return;

//            Interact_Timed intOpenDoor = __instance.m_intOpenDoor;

//            string Prefix = setting.Prefix;
//            string Postfix = setting.Postfix;
//            string TextToReplace = setting.TextToReplace;

//            if (string.IsNullOrEmpty(Prefix)) Prefix = string.Empty;
//            else Prefix += "\n";
//            if (string.IsNullOrEmpty(Postfix)) Postfix = string.Empty;
//            else Postfix += "\n";
//            if (string.IsNullOrEmpty(TextToReplace)) TextToReplace = intOpenDoor.InteractionMessage;
//            else TextToReplace += "\n";

//            intOpenDoor.InteractionMessage = Prefix + TextToReplace + Postfix;

//            LegacyLogger.Debug($"SecDoorIntTextOverride: Override IntText. {setting.LocalIndex}, {setting.LayerType}, {setting.DimensionIndex}");
//        }
//    }
//}
