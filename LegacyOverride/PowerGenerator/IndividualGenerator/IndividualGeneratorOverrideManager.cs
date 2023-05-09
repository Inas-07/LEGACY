using GTFO.API.Utilities;
using LEGACY.Utils;
using System.IO;
using System.Collections.Generic;
using GTFO.API;
using LevelGeneration;
using Il2CppSystem.Text;
using GameData;

namespace LEGACY.LegacyOverride.PowerGenerator.IndividualGenerator
{
    internal class IndividualGeneratorOverrideManager
    {
        public static readonly IndividualGeneratorOverrideManager Current;

        private Dictionary<uint, LevelIndividualGenerators> levelPowerGenerators = new();

        private LiveEditListener liveEditListener;

        private static readonly string CONFIG_PATH = Path.Combine(PowerGeneratorOverrideManager.BASE_CONFIG_PATH, "IndividualGenerator");

        private InstanceManager<LG_PowerGenerator_Core> instanceManager = new(); 

        private HashSet<LG_PowerGenerator_Core> gcGenerators = new();

        private void AddOverride(LevelIndividualGenerators _override)
        {
            if (_override == null) return;

            if (levelPowerGenerators.ContainsKey(_override.MainLevelLayout))
            {
                LegacyLogger.Warning("Replaced MainLevelLayout {0}", _override.MainLevelLayout);
                levelPowerGenerators[_override.MainLevelLayout] = _override;
            }
            else
            {
                levelPowerGenerators.Add(_override.MainLevelLayout, _override);
            }
        }

        public void Init()
        {
            if (!Directory.Exists(CONFIG_PATH))
            {
                Directory.CreateDirectory(CONFIG_PATH);

                var file = File.CreateText(Path.Combine(CONFIG_PATH, "Template.json"));
                file.WriteLine(Json.Serialize(new LevelIndividualGenerators()));
                file.Flush();
                file.Close();
                return;
            }

            foreach (string confFile in Directory.EnumerateFiles(CONFIG_PATH, "*.json", SearchOption.AllDirectories))
            {
                LevelIndividualGenerators conf;
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
                LevelIndividualGenerators conf = Json.Deserialize<LevelIndividualGenerators>(content);
                AddOverride(conf);
            });
        }

        public List<ZoneGenerators> GetLevelPowerGeneratorOverride(uint MainLevelLayout) => levelPowerGenerators.ContainsKey(MainLevelLayout) ? levelPowerGenerators[MainLevelLayout].PowerGeneratorsInLevel : null;

        private void OutputLevelInstanceInfo()
        {
            StringBuilder s = new();
            s.AppendLine();

            foreach (var globalZoneIndex in instanceManager.RegisteredZones())
            {
                s.AppendLine($"{globalZoneIndex.Item3}, {globalZoneIndex.Item2}, Dim {globalZoneIndex.Item1}");

                List<LG_PowerGenerator_Core> PGInstanceInZone = instanceManager.GetInstanceInZone(globalZoneIndex);
                for (int instanceIndex = 0; instanceIndex < PGInstanceInZone.Count; instanceIndex++)
                {
                    var PGInstance = PGInstanceInZone[instanceIndex];
                    s.AppendLine($"GENERATOR_{PGInstance.m_serialNumber}. Instance index: {instanceIndex}");
                }

                s.AppendLine();
            }

            string msg = s.ToString();

            if (!string.IsNullOrWhiteSpace(msg))
                LegacyLogger.Debug(s.ToString());
        }

        private void OnEnterLevel()
        {
            OutputLevelInstanceInfo();
        }

        private void Clear()
        {
            instanceManager.Clear();
            gcGenerators.Clear();
        }

        public uint Register(LG_PowerGenerator_Core __instance) 
            => instanceManager.Register(
            __instance.SpawnNode.m_dimension.DimensionIndex, 
            __instance.SpawnNode.LayerType, 
            __instance.SpawnNode.m_zone.LocalIndex, 
            __instance);

        public void MarkAsGCGenerator(LG_PowerGenerator_Core __instance) => gcGenerators.Add(__instance);

        public bool IsGCGenerator(LG_PowerGenerator_Core __instance) => gcGenerators.Contains(__instance);

        public List<LG_PowerGenerator_Core> GetInstanceInZone((eDimensionIndex, LG_LayerType, eLocalZoneIndex) globalZoneIndex) => instanceManager.GetInstanceInZone(globalZoneIndex);

        public List<LG_PowerGenerator_Core> GetInstanceInZone(eDimensionIndex dimensionIndex, LG_LayerType layerType, eLocalZoneIndex localIndex) => instanceManager.GetInstanceInZone(dimensionIndex, layerType, localIndex); 

        private IndividualGeneratorOverrideManager() { }

        static IndividualGeneratorOverrideManager()
        {
            Current = new();
            LevelAPI.OnEnterLevel += Current.OnEnterLevel;
            LevelAPI.OnLevelCleanup += Current.Clear;
        }
    }
}
