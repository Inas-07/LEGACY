using GTFO.API;
using LEGACY.Utils;
using System.Collections.Generic;
using ExtraObjectiveSetup.BaseClasses;
using GTFO.API.Utilities;
using UnityEngine;
using Il2CppSystem.Runtime.Remoting.Messaging;
using FloLib.Networks.Replications;
using ExtraObjectiveSetup;
using SNetwork;

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

        public const int MAX_PLAYING_MUSIC_COUNT = 5;

        public bool MCSStateLocked { get; internal set; } = false;

        private Dictionary<string, MusicOverride> Musics { get; } = new();

        private Dictionary<uint, MusicOverride> musicStartID = new();

        private HashSet<string> musicPlaying = new();

        public StateReplicator<MusicSyncStruct> StateReplicator { get; private set; }

        internal void PlayMusic(string worldEventObjectFilter)
        {
            if (musicPlaying.Count >= MAX_PLAYING_MUSIC_COUNT)
            {
                LegacyLogger.Error("PlayMusic: There're 5 music playing now, cannot start playing more!");
                return;
            }

            if(!Musics.ContainsKey(worldEventObjectFilter)) 
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
            var musicSetting = Musics[worldEventObjectFilter];
            Sound.Post(musicSetting.StartID, true);
            CellSound.SetGlobalRTPCValue(musicSetting.VolumeRTPC, CellSettingsManager.SettingsData.Audio.MusicVolume.Value * 100f); // TODO: test
            MusicManager.Current.m_machine.ChangeState(MUS_State.Silence);

            if(SNet.IsMaster)
            {
                var newState = new MusicSyncStruct(StateReplicator.State);
                if (newState.Id0 == 0)      newState.Id0 = musicSetting.StartID;
                else if (newState.Id1 == 0) newState.Id1 = musicSetting.StartID;
                else if (newState.Id2 == 0) newState.Id2 = musicSetting.StartID;
                else if (newState.Id3 == 0) newState.Id3 = musicSetting.StartID;
                else if (newState.Id4 == 0) newState.Id4 = musicSetting.StartID;
                StateReplicator.SetState(newState);
            }
        }

        internal void StopMusic(string worldEventObjectFilter)
        {
            if (!Musics.ContainsKey(worldEventObjectFilter))
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
            var musicSetting = Musics[worldEventObjectFilter];
            Sound.Post(musicSetting.StopID, true);

            if (SNet.IsMaster)
            {
                var newState = new MusicSyncStruct(StateReplicator.State);
                if (newState.Id0 == musicSetting.StartID) newState.Id0 = 0;
                if (newState.Id1 == musicSetting.StartID) newState.Id1 = 0;
                if (newState.Id2 == musicSetting.StartID) newState.Id2 = 0;
                if (newState.Id3 == musicSetting.StartID) newState.Id3 = 0;
                if (newState.Id4 == musicSetting.StartID) newState.Id4 = 0;
                StateReplicator.SetState(newState);
            }
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
                var musicSetting = Musics[worldEventObjectFilter];
                CellSound.SetGlobalRTPCValue(musicSetting.VolumeRTPC, multi * 100f);  // TODO: test
            }
        }

        internal void OnMusicVolumeSettingChange()
        {
            foreach (var worldEventObjectFilter in musicPlaying)
            {
                var musicSetting = Musics[worldEventObjectFilter];
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

            Musics.Clear();
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

                Musics[setting.WorldEventObjectFilter] = setting;
                musicStartID[setting.StartID] = setting;
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

        private void OnStateChanged(MusicSyncStruct oldState, MusicSyncStruct newState, bool isRecall) 
        {
            if (!isRecall) return;

            void TryPlay(uint StartID)
            {
                if(StartID != 0 && musicStartID.TryGetValue(StartID, out var music))
                {
                    PlayMusic(music.WorldEventObjectFilter);
                }
            }

            TryPlay(newState.Id0);
            TryPlay(newState.Id1);
            TryPlay(newState.Id2);
            TryPlay(newState.Id3);
            TryPlay(newState.Id4);
        }

        public override void Init()
        {
            base.Init();
            EventAPI.OnAssetsLoaded += () =>
            {
                if (StateReplicator != null) return;

                uint id = EOSNetworking.AllotForeverReplicatorID();
                if (id == EOSNetworking.INVALID_ID)
                {
                    LegacyLogger.Error("PlayMusic: failed to setup state replicator!");
                }

                StateReplicator = StateReplicator<MusicSyncStruct>.Create(id, new(), LifeTimeType.Forever);
                StateReplicator.OnStateChanged += OnStateChanged;
            };
        }

        private MusicStateOverrider(): base() 
        {
            UpdateOverrideSetting();

            LevelAPI.OnBuildStart += Clear_Leveled;
            LevelAPI.OnLevelCleanup += Clear_Leveled;
        }
    }
}