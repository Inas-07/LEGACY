using GTFO.API;
using GTFO.API.Utilities;
using LEGACY.Utils;
using System.IO;
using System.Collections.Generic;
using Gear;
using RootMotion.FinalIK;
using AK;
using UnityEngine;

namespace LEGACY.LegacyOverride.ExtraExpeditionSettings
{
    internal class ExpeditionSettingsManager
    {
        public static ExpeditionSettingsManager Current;

        private Dictionary<uint, ExpeditionSettings> expSettings = new();

        private LiveEditListener liveEditListener;

        private static readonly string CONFIG_PATH = Path.Combine(LegacyOverrideManagers.LEGACY_CONFIG_PATH, "ExpeditionSettings");

        private void AddOverride(ExpeditionSettings _override)
        {
            if (_override == null) return;

            // because we have chained puzzles, sorting is necessary to preserve chained puzzle instance order.

            if (expSettings.ContainsKey(_override.MainLevelLayout))
            {
                LegacyLogger.Warning("Replaced MainLevelLayout {0}", _override.MainLevelLayout);
                expSettings[_override.MainLevelLayout] = _override;
            }
            else
            {
                expSettings.Add(_override.MainLevelLayout, _override);
            }
        }

        public void Init()
        {
            if (!Directory.Exists(CONFIG_PATH))
            {
                Directory.CreateDirectory(CONFIG_PATH);
                var file = File.CreateText(Path.Combine(CONFIG_PATH, "Template.json"));
                file.WriteLine(Json.Serialize(new ExpeditionSettings()));
                file.Flush();
                file.Close();

                return;
            }

            foreach (string confFile in Directory.EnumerateFiles(CONFIG_PATH, "*.json", SearchOption.AllDirectories))
            {
                ExpeditionSettings conf;
                Json.Load(confFile, out conf);

                AddOverride(conf);
            }

            liveEditListener = LiveEdit.CreateListener(CONFIG_PATH, "*.json", true);
            liveEditListener.FileChanged += FileChanged;
        }

        private void FileChanged(LiveEditEventArgs e)
        {
            LegacyLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                ExpeditionSettings conf = Json.Deserialize<ExpeditionSettings>(content);
                AddOverride(conf);
            });
        }

        private void OnBuildDone()
        {
            if (!expSettings.ContainsKey(RundownManager.ActiveExpedition.LevelLayoutData)) return;

            var setting = expSettings[RundownManager.ActiveExpedition.LevelLayoutData];

        }

        private void OnBuildStart()
        {

        }

        private void OnLevelCleanup()
        {

        }

        private void OnEnterLevel()
        {

        }

        private ExpeditionSettingsManager() { }

        static ExpeditionSettingsManager()
        {
            Current = new();
            LevelAPI.OnBuildStart += Current.OnBuildStart;
            LevelAPI.OnEnterLevel += Current.OnEnterLevel;
            LevelAPI.OnBuildDone += Current.OnBuildDone;
            LevelAPI.OnLevelCleanup += Current.OnLevelCleanup;
        }
    }
}