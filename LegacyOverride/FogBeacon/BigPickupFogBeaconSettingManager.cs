﻿using GTFO.API.Utilities;
using System.IO;
using System.Collections.Generic;
using LEGACY.Utils;
using GTFO.API;

namespace LEGACY.LegacyOverride.FogBeacon
{
    internal class BigPickupFogBeaconSettingManager
    {
        public static BigPickupFogBeaconSettingManager Current { get; private set; }

        private Dictionary<uint, BigPickupFogBeaconSetting> fogBeaconSettings = new();

        private LiveEditListener liveEditListener;

        private static readonly string CONFIG_PATH = Path.Combine(LegacyOverrideManagers.LEGACY_CONFIG_PATH, "FogBeaconSetting");

        public static readonly BigPickupFogBeaconSetting DEDFAULT_SETTING = new BigPickupFogBeaconSetting();

        internal BigPickupFogBeaconSetting SettingForCurrentLevel { private set; get; } = DEDFAULT_SETTING;

        private void AddOverride(BigPickupFogBeaconSetting _override)
        {
            if (_override == null) return;

            if (fogBeaconSettings.ContainsKey(_override.MainLevelLayout))
            {
                LegacyLogger.Warning("Replaced MainLevelLayout {0}", _override.MainLevelLayout);
                fogBeaconSettings[_override.MainLevelLayout] = _override;
            }
            else
            {
                fogBeaconSettings.Add(_override.MainLevelLayout, _override);
            }
        }

        public void Init()
        {
            if (!Directory.Exists(CONFIG_PATH))
            {
                Directory.CreateDirectory(CONFIG_PATH);
                var file = File.CreateText(Path.Combine(CONFIG_PATH, "Template.json"));
                file.WriteLine(Json.Serialize(new BigPickupFogBeaconSetting()));
                file.Flush();
                file.Close();

                return;
            }

            foreach (string confFile in Directory.EnumerateFiles(CONFIG_PATH, "*.json", SearchOption.AllDirectories))
            {
                BigPickupFogBeaconSetting conf;
                Json.Load(confFile, out conf);

                AddOverride(conf);
            }

            LevelAPI.OnBuildStart += UpdateSetting;

            liveEditListener = LiveEdit.CreateListener(CONFIG_PATH, "*.json", true);
            liveEditListener.FileChanged += FileChanged;
        }

        private void FileChanged(LiveEditEventArgs e)
        {
            LegacyLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                BigPickupFogBeaconSetting conf = Json.Deserialize<BigPickupFogBeaconSetting>(content);
                AddOverride(conf);

                if(GameStateManager.IsInExpedition)
                {
                    UpdateSetting();
                }
            });
        }

        private void UpdateSetting()
        {
            uint mainLevelLayout = RundownManager.ActiveExpedition.LevelLayoutData;
            SettingForCurrentLevel = fogBeaconSettings.ContainsKey(mainLevelLayout) ? fogBeaconSettings[mainLevelLayout] : DEDFAULT_SETTING;
            LegacyLogger.Debug($"FogBeaconSettingManager: updated setting for level with main level layout id {mainLevelLayout}");
        }

        private BigPickupFogBeaconSettingManager() { }

        static BigPickupFogBeaconSettingManager()
        {
            Current = new();
        }
    }
}
