using GTFO.API.Utilities;
using LEGACY.LegacyOverride;
using LEGACY.LegacyOverride.ElevatorCargo;
using System.IO;
using System.Collections.Generic;
using LEGACY.Utils;
using GTFO.API;

namespace LEGACY.LegacyOverride.EnemyTagger
{
    internal class EnemyTaggerSettingManager
    {
        public static EnemyTaggerSettingManager Current;

        private Dictionary<uint, EnemyTaggerSetting> enemyTaggerSettingSettings = new();

        private LiveEditListener liveEditListener;

        private static readonly string CONFIG_PATH = Path.Combine(LegacyOverrideManagers.LEGACY_CONFIG_PATH, "EnemyTaggerSetting");

        internal EnemyTaggerSetting SettingForCurrentLevel { private set; get; } = default;

        private void AddOverride(EnemyTaggerSetting _override)
        {
            if (_override == null) return;

            if (enemyTaggerSettingSettings.ContainsKey(_override.MainLevelLayout))
            {
                Logger.Warning("Replaced MainLevelLayout {0}", _override.MainLevelLayout);
                enemyTaggerSettingSettings[_override.MainLevelLayout] = _override;
            }
            else
            {
                enemyTaggerSettingSettings.Add(_override.MainLevelLayout, _override);
            }
        }

        public void Init()
        {
            if (!Directory.Exists(CONFIG_PATH))
            {
                Directory.CreateDirectory(CONFIG_PATH);
                var file = File.CreateText(Path.Combine(CONFIG_PATH, "Template.json"));
                file.WriteLine(Json.Serialize(new EnemyTaggerSetting()));
                file.Flush();
                file.Close();

                return;
            }

            foreach (string confFile in Directory.EnumerateFiles(CONFIG_PATH, "*.json", SearchOption.AllDirectories))
            {
                EnemyTaggerSetting conf;
                Json.Load(confFile, out conf);

                AddOverride(conf);
            }

            LevelAPI.OnBuildStart += UpdateSetting;

            liveEditListener = LiveEdit.CreateListener(CONFIG_PATH, "*.json", true);
            liveEditListener.FileChanged += FileChanged;
        }

        private void FileChanged(LiveEditEventArgs e)
        {
            Logger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                EnemyTaggerSetting conf = Json.Deserialize<EnemyTaggerSetting>(content);
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
            SettingForCurrentLevel = enemyTaggerSettingSettings.ContainsKey(mainLevelLayout) ? enemyTaggerSettingSettings[mainLevelLayout] : default;
            Logger.Debug($"EnemyTaggerSettingManager: updated setting for level with main level layout id {mainLevelLayout}");
        }

        private EnemyTaggerSettingManager() { }

        static EnemyTaggerSettingManager()
        {
            Current = new();
        }
    }
}
