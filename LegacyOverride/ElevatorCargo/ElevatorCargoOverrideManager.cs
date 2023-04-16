using GTFO.API.Utilities;
using System.Collections.Generic;
using System.IO;
using LEGACY.Utils;

namespace LEGACY.LegacyOverride.ElevatorCargo
{
    internal class ElevatorCargoOverrideManager
    {
        public static ElevatorCargoOverrideManager Current;

        private Dictionary<uint, LevelElevatorCargo> elevatorCargos = new();

        private LiveEditListener liveEditListener;

        private static readonly string CONFIG_PATH = Path.Combine(LegacyOverrideManagers.LEGACY_CONFIG_PATH, "ElevatorCargoOverride");

        private void AddOverride(LevelElevatorCargo _override)
        {
            if (_override == null) return;

            if (elevatorCargos.ContainsKey(_override.MainLevelLayout))
            {
                Logger.Warning("Replaced MainLevelLayout {0}", _override.MainLevelLayout);
                elevatorCargos[_override.MainLevelLayout] = _override;
            }
            else
            {
                elevatorCargos.Add(_override.MainLevelLayout, _override);
            }
        }

        public void Init()
        {
            if (!Directory.Exists(CONFIG_PATH))
            {
                Directory.CreateDirectory(CONFIG_PATH);
                var file = File.CreateText(Path.Combine(CONFIG_PATH, "Template.json"));
                file.WriteLine(Json.Serialize(new LevelElevatorCargo()));
                file.Flush();
                file.Close();

                return;
            }

            foreach (string confFile in Directory.EnumerateFiles(CONFIG_PATH, "*.json", SearchOption.AllDirectories))
            {
                LevelElevatorCargo conf;
                Json.Load(confFile, out conf);

                AddOverride(conf);
            }

            liveEditListener = LiveEdit.CreateListener(CONFIG_PATH, "*.json", true);
            liveEditListener.FileChanged += FileChanged;
        }

        private void FileChanged(LiveEditEventArgs e)
        {
            Logger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                LevelElevatorCargo conf = Json.Deserialize<LevelElevatorCargo>(content);
                AddOverride(conf);
            });
        }

        internal LevelElevatorCargo GetLevelElevatorCargoItems(uint MainLevelLayout) => elevatorCargos.ContainsKey(MainLevelLayout) ? elevatorCargos[MainLevelLayout] : null;

        private ElevatorCargoOverrideManager() { }

        static ElevatorCargoOverrideManager()
        {
            Current = new();
        }
    }

}
