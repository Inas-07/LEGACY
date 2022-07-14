using HarmonyLib;
using LevelGeneration;
using Localization;

namespace LEGACY
{
    [HarmonyPatch]
    class Patch_LockSecurityDoor_FixCustomText
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_SecurityDoor_Locks), nameof(LG_SecurityDoor_Locks.Setup), new System.Type[] { typeof(LG_SecurityDoor) })]
        private static void Post_LG_SecurityDoor_Locks_Setup(LG_SecurityDoor door, LG_SecurityDoor_Locks __instance)
        {
            LocalizedText text = door.LinkedToZoneData.ProgressionPuzzleToEnter.CustomText;
            __instance.m_lockedWithNoKeyInteractionText = text;
        }
    }
}
