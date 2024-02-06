using GameData;
using HarmonyLib;
using LEGACY.Utils;
using static CellSettingsData;

namespace LEGACY.LegacyOverride.Music
{
    [HarmonyPatch]
    internal class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MusicMachine), nameof(MusicMachine.ChangeState))]
        private static bool Pre_MusicMachine_ChangeState(MusicMachine __instance, ref MUS_State state)
        {
            if (MusicStateOverrider.Current.ShouldOverrideMusicState(state))
            {
                if(MusicStateOverrider.Current.MCSStateLocked) 
                {
                    return false;
                }
                else
                {
                    LegacyLogger.Debug("Music State overriden and locked");
                    state = MUS_State.Silence;
                    MusicStateOverrider.Current.MCSStateLocked = true;
                    return true;
                }
            }
            else
            {
                if(MusicStateOverrider.Current.MCSStateLocked)
                {
                    LegacyLogger.Debug("Music State un-overriden");
                }

                MusicStateOverrider.Current.MCSStateLocked = false;
                return true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CellSettingsManager), nameof(CellSettingsManager.OnApplicationFocus))]
        private static void Post_OnApplicationFocus(CellSettingsManager __instance, bool focus) 
        { 
            MusicStateOverrider.Current.OnApplicationFocus(focus);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CellSettings_Audio), nameof(CellSettings_Audio.ApplyAllValues))]
        private static void Post_ApplyAllValues(CellSettingsManager __instance)
        {
            MusicStateOverrider.Current.OnMusicVolumeSettingChange();
        }
    }
}
