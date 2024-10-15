using GTFO.API.Utilities;
using System.IO;
using System.Collections.Generic;
using LEGACY.Utils;
using GTFO.API;
using Il2CppInterop.Runtime.Injection;
using LevelGeneration;
using Player;
using UnityEngine;
using LEGACY.LegacyOverride.Patches;
using AssetShards;
using ThermalSights;

namespace LEGACY.LegacyOverride.EnemyTagger
{
    internal class EnemyTaggerSettingManager
    {
        public static readonly EnemyTaggerSettingManager Current;

        private Dictionary<uint, EnemyTaggerSetting> enemyTaggerSettingSettings = new();

        private LiveEditListener liveEditListener;

        private static readonly string CONFIG_PATH = Path.Combine(LegacyOverrideManagers.LEGACY_CONFIG_PATH, "EnemyTaggerSetting");

        private static readonly EnemyTaggerSetting DEFAULT = new();

        internal EnemyTaggerSetting SettingForCurrentLevel { private set; get; } = DEFAULT;

        private void AddOverride(EnemyTaggerSetting _override)
        {
            if (_override == null) return;

            if (enemyTaggerSettingSettings.ContainsKey(_override.MainLevelLayout))
            {
                LegacyLogger.Warning("Replaced MainLevelLayout {0}", _override.MainLevelLayout);
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
            LegacyLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
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
            SettingForCurrentLevel = enemyTaggerSettingSettings.ContainsKey(mainLevelLayout) ? enemyTaggerSettingSettings[mainLevelLayout] : DEFAULT;
            LegacyLogger.Debug($"EnemyTaggerSettingManager: updated setting for level with main level layout id {mainLevelLayout}");
        }

        private List<GameObject> obsVisuals = new();

        public IEnumerable<GameObject> OBSVisuals => obsVisuals;

        public void SetupAsObserver(LG_PickupItem __instance)
        {
            var setting = SettingForCurrentLevel;

            CarryItemPickup_Core core = __instance.m_root.GetComponentInChildren<CarryItemPickup_Core>();
            Interact_Pickup_PickupItem interact = core.m_interact.Cast<Interact_Pickup_PickupItem>();
            LG_PickupItem_Sync sync = core.m_sync.Cast<LG_PickupItem_Sync>();

            EnemyTaggerComponent tagger = core.gameObject.AddComponent<EnemyTaggerComponent>();

            tagger.Parent = core;
            tagger.gameObject.SetActive(true);

            interact.InteractDuration = setting.TimeToPickup;
            tagger.MaxTagPerScan = setting.MaxTagPerScan;
            tagger.TagInterval = setting.TagInterval;
            tagger.TagRadius = setting.TagRadius;
            tagger.WarmupTime = setting.WarmupTime;

            GameObject obsVisual = null;
            if (setting.UseVisual)
            {
                obsVisual = Object.Instantiate(Assets.OBSVisual, __instance.transform);
                obsVisual.transform.localScale = new Vector3(setting.TagRadius, setting.TagRadius, setting.TagRadius);
                obsVisual.SetActive(false);

                obsVisuals.Add(obsVisual);
            }

            sync.OnSyncStateChange += new System.Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>((status, placement, playerAgent, isRecall) =>
            {
                switch (status)
                {
                    case ePickupItemStatus.PlacedInLevel:
                        tagger.PickedByPlayer = null;
                        tagger.ChangeState(setting.TagWhenPlaced ? eEnemyTaggerState.Active_Warmup : eEnemyTaggerState.Inactive);
                        interact.InteractDuration = setting.TimeToPickup;
                        if (obsVisual != null)
                        {
                            obsVisual.gameObject.transform.SetPositionAndRotation(placement.position, placement.rotation);
                            if (isRecall)
                            {
                                if (core.CanWarp)
                                {
                                    CoroutineManager.BlinkIn(obsVisual, tagger.WarmupTime);
                                }
                            }
                            else
                            {
                                if (!obsVisual.active && setting.TagWhenPlaced)
                                {
                                    CoroutineManager.BlinkIn(obsVisual, tagger.WarmupTime);
                                }
                            }
                        }
                        break;

                    case ePickupItemStatus.PickedUp:
                        tagger.gameObject.SetActive(true);
                        tagger.PickedByPlayer = playerAgent;
                        tagger.ChangeState(setting.TagWhenHold ? eEnemyTaggerState.Active_Warmup : eEnemyTaggerState.Inactive);
                        interact.InteractDuration = setting.TimeToPlace;
                        if (obsVisual != null && obsVisual.active)
                        {
                            CoroutineManager.BlinkOut(obsVisual);
                        }
                        break;
                }
            });
        }

        private void AddOBSVisualRenderers()
        {
            foreach (var go in OBSVisuals)
            {
                var renderer = go.GetComponentInChildren<Renderer>();
                float intensity = renderer.material.GetFloat("_Intensity");
                float behindWallIntensity = -1.0f;
                TSAManager.Current.RegisterPuzzleVisual(new TSAManager.PuzzleVisualWrapper()
                {
                    GO = go,
                    Renderer = renderer,
                    Intensity = intensity,
                    BehindWallIntensity = behindWallIntensity
                });
            }
        }

        private void Clear()
        {
            obsVisuals.Clear();
        }

        private EnemyTaggerSettingManager() 
        {
            LevelAPI.OnBuildStart += Clear;
            LevelAPI.OnLevelCleanup += Clear;
            LevelAPI.OnEnterLevel += AddOBSVisualRenderers;
        }

        static EnemyTaggerSettingManager()
        {
            Current = new();
        }
    }
}
