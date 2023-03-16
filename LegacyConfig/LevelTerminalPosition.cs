using GameData;
using GTFO.API.Utilities;
using LevelGeneration;
using System.Collections.Generic;
using LEGACY.Utils;
using System.IO;

namespace LEGACY.LegacyConfig
{
    public class TerminalPosition
    {
        public eDimensionIndex DimensionIndex { get; set; }

        public LG_LayerType LayerType { get; set; }

        public eLocalZoneIndex LocalIndex { get; set; }

        public uint TerminalIndex { get; set; }

        public Vec3 Position { get; set; }
    }

    public class LevelTerminalPosition
    {
        public uint MainLevelLayout { set; get; }

        public List<TerminalPosition> TerminalPositions { set; get; } = new();
    }

    internal class TerminalPositionOverrideManager
    {
        public static TerminalPositionOverrideManager Current;

        private Dictionary<uint, LevelTerminalPosition> terminalPositions = new();

        private LiveEditListener liveEditListener;

        private static readonly string CONFIG_PATH = Path.Combine(LegacyConfigManager.LEGACY_CONFIG_PATH, "TerminalPositionOverride");

        private void AddOverride(LevelTerminalPosition _override)
        {
            if (_override == null) return;

            if (terminalPositions.ContainsKey(_override.MainLevelLayout))
            {
                Logger.Warning("Replaced MainLevelLayout {0}", _override.MainLevelLayout);
                terminalPositions[_override.MainLevelLayout] = _override;
            }
            else
            {
                terminalPositions.Add(_override.MainLevelLayout, _override);
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
                LevelTerminalPosition conf;
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
                LevelTerminalPosition conf = Json.Deserialize<LevelTerminalPosition>(content);
                AddOverride(conf);
            });
        }

        internal List<TerminalPosition> GetLevelTerminalPositionOverride(uint MainLevelLayout) => terminalPositions.ContainsKey(MainLevelLayout) ? terminalPositions[MainLevelLayout].TerminalPositions : null;

        private TerminalPositionOverrideManager() { }

        static TerminalPositionOverrideManager()
        {
            Current = new();
        }
    }

}
