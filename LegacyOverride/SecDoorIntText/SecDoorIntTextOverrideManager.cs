using GTFO.API.Utilities;
using System.IO;
using System.Collections.Generic;
using LEGACY.Utils;
using GTFO.API;

namespace LEGACY.LegacyOverride.SecDoorIntText
{
    internal class SecDoorIntTextOverrideManager
    {
        public static SecDoorIntTextOverrideManager Current;

        private Dictionary<uint, LevelSecDoorIntTextOverride> SecDoorIntTextOverrides = new();

        private LiveEditListener liveEditListener;

        private static readonly string CONFIG_PATH = Path.Combine(LegacyOverrideManagers.LEGACY_CONFIG_PATH, "SecDoorIntText");

        internal LevelSecDoorIntTextOverride SettingForCurrentLevel { private set; get; } = null;

        private void AddOverride(LevelSecDoorIntTextOverride _override)
        {
            if (_override == null) return;

            if (SecDoorIntTextOverrides.ContainsKey(_override.MainLevelLayout))
            {
                Logger.Warning("Replaced MainLevelLayout {0}", _override.MainLevelLayout);
                SecDoorIntTextOverrides[_override.MainLevelLayout] = _override;
            }
            else
            {
                SecDoorIntTextOverrides.Add(_override.MainLevelLayout, _override);
            }
        }

        public void Init()
        {

            if (!Directory.Exists(CONFIG_PATH))
            {
                Directory.CreateDirectory(CONFIG_PATH);
                var file = File.CreateText(Path.Combine(CONFIG_PATH, "Template.json"));
                file.WriteLine(Json.Serialize(new LevelSecDoorIntTextOverride()));
                file.Flush();
                file.Close();

                return;
            }

            foreach (string confFile in Directory.EnumerateFiles(CONFIG_PATH, "*.json", SearchOption.AllDirectories))
            {
                LevelSecDoorIntTextOverride conf;
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
                LevelSecDoorIntTextOverride conf = Json.Deserialize<LevelSecDoorIntTextOverride>(content);
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
            SettingForCurrentLevel = SecDoorIntTextOverrides.ContainsKey(mainLevelLayout) ? SecDoorIntTextOverrides[mainLevelLayout] : null;
        }

        private SecDoorIntTextOverrideManager() { }

        static SecDoorIntTextOverrideManager()
        {
            Current = new();
        }
    }
}
