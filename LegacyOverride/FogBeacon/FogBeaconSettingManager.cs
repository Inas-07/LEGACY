using GTFO.API.Utilities;
using LEGACY.LegacyOverride.ElevatorCargo;
using System.IO;
using System.Collections.Generic;
using LEGACY.Utils;
using GTFO.API;

namespace LEGACY.LegacyOverride.FogBeacon
{
    internal class FogBeaconSettingManager
    {
        public static readonly FogBeaconSettingManager Current;

        private Dictionary<uint, FogBeaconSetting> fogBeaconSettings = new();

        private LiveEditListener liveEditListener;

        private static readonly string CONFIG_PATH = Path.Combine(LegacyOverrideManagers.LEGACY_CONFIG_PATH, "FogBeaconSetting");

        internal FogBeaconSetting SettingForCurrentLevel { private set; get; } = default;

        private void AddOverride(FogBeaconSetting _override)
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
                file.WriteLine(Json.Serialize(new FogBeaconSetting()));
                file.Flush();
                file.Close();

                return;
            }

            foreach (string confFile in Directory.EnumerateFiles(CONFIG_PATH, "*.json", SearchOption.AllDirectories))
            {
                FogBeaconSetting conf;
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
                FogBeaconSetting conf = Json.Deserialize<FogBeaconSetting>(content);
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
            SettingForCurrentLevel = fogBeaconSettings.ContainsKey(mainLevelLayout) ? fogBeaconSettings[mainLevelLayout] : default(FogBeaconSetting);
            LegacyLogger.Debug($"FogBeaconSettingManager: updated setting for level with main level layout id {mainLevelLayout}");
        }

        private FogBeaconSettingManager() { }

        static FogBeaconSettingManager()
        {
            Current = new();
        }
    }
}
