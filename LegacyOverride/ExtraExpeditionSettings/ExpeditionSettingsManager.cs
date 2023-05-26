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

        private List<EnemyScanner> biotrackers = new();

        private ExpeditionSettings currentSettings = new();

        private bool CheckedMapperTracker = false;

        private MonoBehaviour MapperTrackerScript = null;

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

        internal void Register(EnemyScanner scanner) => biotrackers.Add(scanner);

        private void OnBuildDone()
        {
            if (!expSettings.ContainsKey(RundownManager.ActiveExpedition.LevelLayoutData)) return;

            var setting = expSettings[RundownManager.ActiveExpedition.LevelLayoutData];
            if(setting.DisableBioTracker)
            {
                ToggleBioTrackerState(false);
            }
        }

        public bool IsBioTrackerDisabled => currentSettings.DisableBioTracker;

        public void ToggleBioTrackerState(bool enabled)
        {
            currentSettings.DisableBioTracker = !enabled;
            biotrackers.ForEach(b => {
                if(enabled)
                {
                    b.m_graphics.m_display.enabled = true;
                    b.m_screen.SetNoTargetsText("");
                    b.m_screen.SetStatusText("Ready to tag");
                    b.m_screen.ResetGuixColor();
                    b.Sound.Post(EVENTS.BIOTRACKER_RECHARGED);
                    MapperTrackerScript?.gameObject.SetActive(true);
                }
                else
                {
                    b.m_screen.SetGuixColor(UnityEngine.Color.yellow);
                    b.m_graphics.m_display.enabled = false;
                    b.Sound.Post(EVENTS.BIOTRACKER_TOOL_LOOP_STOP);
                    MapperTrackerScript?.gameObject.SetActive(false);
                }
            });
            LegacyLogger.Warning($"Toggled {enabled}?");
        }

        private void CheckMapperTracker()
        {
            if (CheckedMapperTracker) return;

            var scripts = GameObject.FindObjectsOfType<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                var scriptType = script.GetType();
                LegacyLogger.Error(scriptType.Assembly.GetName().Name);
                var scope = scriptType.Namespace;
                if (scope != null && scope.Equals("MapperTracker"))
                {
                    LegacyLogger.Error("Found MapperTracker instance");
                    MapperTrackerScript = script;
                    break;
                }
            }

            if (MapperTrackerScript == null) LegacyLogger.Error("Didn find MapperTracker");
            CheckedMapperTracker = true;
        }

        private void OnBuildStart()
        {
            //CheckMapperTracker();
        }

        private void OnLevelCleanup()
        {
            biotrackers.Clear();
            currentSettings = new();
        }

        private void OnEnterLevel()
        {
            currentSettings = expSettings.ContainsKey(RundownManager.ActiveExpedition.LevelLayoutData) 
                ? expSettings[RundownManager.ActiveExpedition.LevelLayoutData] : new();
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