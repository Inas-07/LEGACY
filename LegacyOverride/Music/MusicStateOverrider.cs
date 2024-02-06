using GTFO.API;
using LEGACY.Utils;
using System.Collections.Generic;
using ExtraObjectiveSetup.BaseClasses;
using GTFO.API.Utilities;
using UnityEngine;
using Il2CppSystem.Runtime.Remoting.Messaging;

namespace LEGACY.LegacyOverride.Music
{
    /**
     * MUS_State doc.:
     * -> Silence: no music
     * -> Theme: used on elevator drop
     * -> Startup: game launched, lobby created 
     * -> MainMenu: game launched. rarely used.
     * 
     * -> Exploration: no active enemy or enemy wave, no enemy in the same node
     * -> Tension, TensionMax - no active wave, stealthing, no alerted enemy
     *    Hidden  - (alerted) enemies are in different node;
     *    Regular - (alerted) enemies are in the same node; 
     * -> Encounter: no active wave, but several (not all) enemies in the node are alerted/
     * -> Combat: no active wave, enemies in the same node are all alerted
     * -> Survival: has active enemy wave
     *    SurvivalExtreme, SurvivalEpicMoment: unused 
     */

    // NOTE: overrider for un-looped sound is not fully implemented

    // TODO: make sound volume in accordance with MusicVolume setting!!! -> WWise RTPC value
    public class MusicStateOverrider: GenericExpeditionDefinitionManager<MusicOverride>
    {
        public static MusicStateOverrider Current { get; private set; } = new();

        protected override string DEFINITION_NAME => "MusicState";

        private CellSoundPlayer Sound => MusicManager.Current.m_machine.Sound;

        public const uint DEF_ID = 0;

        public bool MCSStateLocked { get; internal set; } = false;

        //// start ID
        //private Dictionary<uint, MusicOverride> soundIDStarts = new();

        //private Dictionary<uint, MusicOverride> soundIDStops = new();

        //private HashSet<uint> soundIDsPlaying = new();

        private Dictionary<string, MusicOverride> musics = new();

        private HashSet<string> musicPlaying = new();

        internal void PlayMusic(string worldEventObjectFilter)
        {
            if(!musics.ContainsKey(worldEventObjectFilter)) 
            {
                LegacyLogger.Error($"Music {worldEventObjectFilter} is not config-ed.");
                return;
            }

            if (musicPlaying.Contains(worldEventObjectFilter)) 
            {
                LegacyLogger.Error($"Music {worldEventObjectFilter} is already playing");
                return; 
            }

            DoPlayMusic(worldEventObjectFilter);
            LegacyLogger.Debug($"Play music {worldEventObjectFilter}");
        }

        private void DoPlayMusic(string worldEventObjectFilter)
        {
            musicPlaying.Add(worldEventObjectFilter);
            var musicSetting = musics[worldEventObjectFilter];
            Sound.Post(musicSetting.StartID, true);
            CellSound.SetGlobalRTPCValue(musicSetting.VolumeRTPC, CellSettingsManager.SettingsData.Audio.MusicVolume.Value * 100f); // TODO: test
            MusicManager.Current.m_machine.ChangeState(MUS_State.Silence);
        }

        internal void StopMusic(string worldEventObjectFilter)
        {
            if (!musics.ContainsKey(worldEventObjectFilter))
            {
                LegacyLogger.Error($"Music {worldEventObjectFilter} is not config-ed.");
                return;
            }

            if (!musicPlaying.Contains(worldEventObjectFilter))
            {
                LegacyLogger.Error($"Music {worldEventObjectFilter} is not playing");
                return;
            }

            DoStopMusic(worldEventObjectFilter);
            LegacyLogger.Debug($"Stop music {worldEventObjectFilter}");
        }

        private void DoStopMusic(string worldEventObjectFilter)
        {
            musicPlaying.Remove(worldEventObjectFilter);
            var musicSetting = musics[worldEventObjectFilter];
            Sound.Post(musicSetting.StopID, true);
        }

        public bool ShouldOverrideMusicState(MUS_State currentState)
        {
            if(GameStateManager.CurrentStateName != eGameStateName.InLevel) return false;

            return musicPlaying.Count > 0;
        }

        internal void OnApplicationFocus(bool focus)
        {
            float multi = focus ? CellSettingsManager.SettingsData.Audio.MusicVolume.Value : 0f;
            foreach(var worldEventObjectFilter in musicPlaying)
            {
                var musicSetting = musics[worldEventObjectFilter];
                CellSound.SetGlobalRTPCValue(musicSetting.VolumeRTPC, multi * 100f);  // TODO: test
            }
        }

        internal void OnMusicVolumeSettingChange()
        {
            foreach (var worldEventObjectFilter in musicPlaying)
            {
                var musicSetting = musics[worldEventObjectFilter];
                CellSound.SetGlobalRTPCValue(musicSetting.VolumeRTPC, CellSettingsManager.SettingsData.Audio.MusicVolume.Value * 100f);  // TODO: test
            }
        }

        private void UpdateOverrideSetting()
        {
            if(!definitions.ContainsKey(DEF_ID))
            {
                LegacyLogger.Error($"Didn't find global definition, ('MainLevelLayout' {DEF_ID})");
                return;
            }

            musics.Clear();
            var settings = definitions[DEF_ID].Definitions;
            foreach(var setting in settings)
            {
                if (setting.WorldEventObjectFilter?.Length == 0)
                {
                    LegacyLogger.Debug($"WorldEventObjectFilter(Music name) unspecified, won't load");
                    continue;
                }

                if (setting.StartID == 0)
                {
                    LegacyLogger.Debug($"StartID unspecified (could not be 0)");
                    continue;
                }

                if(setting.StopID == 0)
                {
                    LegacyLogger.Error("StopID == 0: MusicStateOverrider doesn't implement un-looped sound!");
                }

                musics[setting.WorldEventObjectFilter] = setting;
                //if (soundIDStarts.ContainsKey(setting.StartID))
                //{
                //    LegacyLogger.Error($"Find duplicate StartID {setting.StartID}, will override!");
                //}

                //soundIDStarts[setting.StartID] = setting;
                //LegacyLogger.Debug($"Added StartID {setting.StartID}!");

                //if (setting.StopID != 0) 
                //{
                //    if (soundIDStops.ContainsKey(setting.StopID))
                //    {
                //        LegacyLogger.Error($"Find duplicate Stop {setting.StopID}, will override!");
                //    }
                //    soundIDStops[setting.StopID] = setting;
                //    LegacyLogger.Debug($"Added StopID {setting.StopID}!");
                //}
                //else
                //{
                //    LegacyLogger.Error("MusicStateOverrider doesn't implement un-looped sound.");
                //}
            }
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            base.FileChanged(e);
            UpdateOverrideSetting();
        }

        private void Clear_Leveled()
        {
            MCSStateLocked = false;
            musicPlaying.Clear();
        }

        private MusicStateOverrider(): base() 
        {
            UpdateOverrideSetting();

            LevelAPI.OnBuildStart += Clear_Leveled;
            LevelAPI.OnLevelCleanup += Clear_Leveled;
        }
    }
}