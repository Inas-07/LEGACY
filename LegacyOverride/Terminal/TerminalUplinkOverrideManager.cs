using GTFO.API;
using GTFO.API.Utilities;
using LEGACY.Utils;
using System.IO;
using System.Collections.Generic;
using LevelGeneration;
using ChainedPuzzles;
using GameData;
using Localization;
using System;
using GTFO.API.Extensions;

namespace LEGACY.LegacyOverride.Terminal
{
    internal class TerminalUplinkOverrideManager
    {
        public static TerminalUplinkOverrideManager Current;

        private Dictionary<uint, LevelTerminalUplinks> uplinks = new();

        private LiveEditListener liveEditListener;

        private static readonly string CONFIG_PATH = Path.Combine(LegacyOverrideManagers.LEGACY_CONFIG_PATH, "TerminalUplink");

        private TextDataBlock UplinkAddrLogContentBlock = null;

        private Dictionary<IntPtr, TerminalUplink> uplinkTerminalConfigs = new();

        private void AddOverride(LevelTerminalUplinks _override)
        {
            if (_override == null) return;

            // because we have chained puzzles, sorting is necessary to preserve chained puzzle instance order.
            _override.Uplinks.Sort((u1, u2) =>
            {
                if (u1.DimensionIndex != u2.DimensionIndex) return (int)u1.DimensionIndex < (int)u2.DimensionIndex ? -1 : 1;
                if (u1.LayerType != u2.LayerType) return (int)u1.LayerType < (int)u2.LayerType ? -1 : 1;
                if (u1.LocalIndex != u2.LocalIndex) return (int)u1.LocalIndex < (int)u2.LocalIndex ? -1 : 1;
                if (u1.TerminalIndex != u2.TerminalIndex) return u1.TerminalIndex < u2.TerminalIndex ? -1 : -1;
                return 0;
            });

            _override.Uplinks.ForEach(u => u.RoundOverrides.Sort((r1, r2) => r1.RoundIndex != r2.RoundIndex ? (r1.RoundIndex < r2.RoundIndex ? -1 : 1) : 0));

            if (uplinks.ContainsKey(_override.MainLevelLayout))
            {
                LegacyLogger.Warning("Replaced MainLevelLayout {0}", _override.MainLevelLayout);
                uplinks[_override.MainLevelLayout] = _override;
            }
            else
            {
                uplinks.Add(_override.MainLevelLayout, _override);
            }
        }

        public void Init()
        {
            if (!Directory.Exists(CONFIG_PATH))
            {
                Directory.CreateDirectory(CONFIG_PATH);
                var file = File.CreateText(Path.Combine(CONFIG_PATH, "Template.json"));
                file.WriteLine(Json.Serialize(new LevelTerminalUplinks()));
                file.Flush();
                file.Close();

                return;
            }

            foreach (string confFile in Directory.EnumerateFiles(CONFIG_PATH, "*.json", SearchOption.AllDirectories))
            {
                LevelTerminalUplinks conf;
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
                LevelTerminalUplinks conf = Json.Deserialize<LevelTerminalUplinks>(content);
                AddOverride(conf);
            });
        }

        internal List<TerminalUplink> GetLevelUplinkOverride(uint MainLevelLayout) => uplinks.ContainsKey(MainLevelLayout) ? uplinks[MainLevelLayout].Uplinks : null;

        private void Build(TerminalUplink config)
        {
            if (config.TerminalIndex < 0) return;

            LG_ComputerTerminal uplinkTerminal = Helper.FindTerminal(config.DimensionIndex, config.LayerType, config.LocalIndex, config.TerminalIndex);

            if(uplinkTerminal == null) return;

            if (uplinkTerminal.m_isWardenObjective && uplinkTerminal.UplinkPuzzle != null)
            {
                LegacyLogger.Error($"BuildUplinkOverride: Uplink already built by vanilla, aborting custom build!");
                return;
            }

            if (config.SetupAsCorruptedUplink)
            {
                LG_ComputerTerminal receiver = Helper.FindTerminal(
                    config.CorruptedUplinkReceiver.DimensionIndex,
                    config.CorruptedUplinkReceiver.LayerType,
                    config.CorruptedUplinkReceiver.LocalIndex,
                    config.CorruptedUplinkReceiver.TerminalIndex);

                if (receiver == null)
                {
                    LegacyLogger.Error("BuildUplinkOverride: SetupAsCorruptedUplink specified but didn't find the receiver terminal, will fall back to normal uplink instead");
                    return;
                }

                if (receiver == uplinkTerminal)
                {
                    LegacyLogger.Error("BuildUplinkOverride: Don't specified uplink sender and receiver on the same terminal");
                    return;
                }

                uplinkTerminal.CorruptedUplinkReceiver = receiver;
                receiver.CorruptedUplinkReceiver = uplinkTerminal; // need to set on both side
            }

            uplinkTerminal.UplinkPuzzle = new TerminalUplinkPuzzle();
            SetupUplinkPuzzle(uplinkTerminal.UplinkPuzzle, uplinkTerminal, config);
            uplinkTerminal.UplinkPuzzle.OnPuzzleSolved += new Action(() =>
            {
                config.EventsOnComplete.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));
            });


            uplinkTerminal.m_command.AddCommand(
                uplinkTerminal.CorruptedUplinkReceiver == null ? TERM_Command.TerminalUplinkConnect : TERM_Command.TerminalCorruptedUplinkConnect, 
                config.UseUplinkAddress ? "UPLINK_CONNECT" : "UPLINK_ESTABLISH", 
                new LocalizedText() {
                UntranslatedText = Text.Get(3914968919),
                Id = 3914968919
                });

            uplinkTerminal.m_command.AddCommand(TERM_Command.TerminalUplinkVerify, "UPLINK_VERIFY", new LocalizedText()
            {
                UntranslatedText = Text.Get(1728022075),
                Id = 1728022075
            });

            if (config.UseUplinkAddress)
            {
                LG_ComputerTerminal addressLogTerminal = null;

                LegacyLogger.Debug($"BuildUplinkOverride: UseUplinkAddress");
                addressLogTerminal = Helper.FindTerminal(config.UplinkAddressLogPosition.DimensionIndex, config.UplinkAddressLogPosition.LayerType, config.UplinkAddressLogPosition.LocalIndex, config.UplinkAddressLogPosition.TerminalIndex);
                if (addressLogTerminal == null)
                {
                    LegacyLogger.Error($"BuildUplinkOverride: didn't find the terminal to put the uplink address log, will put on uplink terminal");
                    addressLogTerminal = uplinkTerminal;
                }

                addressLogTerminal.AddLocalLog(new TerminalLogFileData()
                {
                    FileName = $"UPLINK_ADDR_{uplinkTerminal.m_serialNumber}.LOG",
                    FileContent = new LocalizedText() 
                    { 
                        UntranslatedText = string.Format(UplinkAddrLogContentBlock != null ? Text.Get(UplinkAddrLogContentBlock.persistentID) : "Available uplink address for TERMINAL_{0}: {1}", uplinkTerminal.m_serialNumber, uplinkTerminal.UplinkPuzzle.TerminalUplinkIP),
                        Id = 0
                    }
                }, true);

                addressLogTerminal.m_command.ClearOutputQueueAndScreenBuffer();
                addressLogTerminal.m_command.AddInitialTerminalOutput();
            }

            if (config.ChainedPuzzleToStartUplink != 0)
            {
                var block = GameDataBlockBase<ChainedPuzzleDataBlock>.GetBlock(config.ChainedPuzzleToStartUplink);
                if (block == null)
                {
                    LegacyLogger.Error($"BuildTerminalUplink: ChainedPuzzleToActive with id {config.ChainedPuzzleToStartUplink} is specified but no ChainedPuzzleDataBlock definition is found... Won't build");
                    uplinkTerminal.m_chainPuzzleForWardenObjective = null;
                }
                else
                {
                    uplinkTerminal.m_chainPuzzleForWardenObjective = ChainedPuzzleManager.CreatePuzzleInstance(
                        block,
                        uplinkTerminal.SpawnNode.m_area,
                        uplinkTerminal.m_wardenObjectiveSecurityScanAlign.position,
                        uplinkTerminal.m_wardenObjectiveSecurityScanAlign);
                }
            }

            foreach(var roundOverride in config.RoundOverrides)
            {
                if(roundOverride.ChainedPuzzleToEndRound != 0u)
                {
                    var block = GameDataBlockBase<ChainedPuzzleDataBlock>.GetBlock(roundOverride.ChainedPuzzleToEndRound);
                    if(block != null)
                    {
                        LG_ComputerTerminal t = null;
                        switch (roundOverride.BuildChainedPuzzleOn)
                        {
                            case UplinkTerminal.SENDER: t = uplinkTerminal; break;
                            case UplinkTerminal.RECEIVER: 
                                if(config.SetupAsCorruptedUplink && uplinkTerminal.CorruptedUplinkReceiver != null)
                                {
                                    t = uplinkTerminal.CorruptedUplinkReceiver;
                                }
                                else
                                {
                                    LegacyLogger.Error($"ChainedPuzzleToEndRound: {roundOverride.ChainedPuzzleToEndRound} specified to build on receiver but this is not a properly setup-ed corr-uplink! Will build ChainedPuzzle on sender side");
                                    t = uplinkTerminal;
                                }
                                break;

                            default: LegacyLogger.Error($"Unimplemented enum UplinkTerminal type {roundOverride.BuildChainedPuzzleOn}"); continue;
                        }

                        roundOverride.ChainedPuzzleToEndRoundInstance = ChainedPuzzleManager.CreatePuzzleInstance(
                            block,
                            t.SpawnNode.m_area,
                            t.m_wardenObjectiveSecurityScanAlign.position,
                            t.m_wardenObjectiveSecurityScanAlign);
                    }
                    else
                    {
                        LegacyLogger.Error($"ChainedPuzzleToEndRound: {roundOverride.ChainedPuzzleToEndRound} specified but didn't find its ChainedPuzzleDatablock definition! Will not build!");
                    }
                }
            }

            uplinkTerminalConfigs[uplinkTerminal.Pointer] = config;
            LegacyLogger.Debug($"BuildUplink: built on {(config.DimensionIndex, config.LayerType, config.LocalIndex, config.TerminalIndex)}");
        }

        public TerminalUplink GetUplinkConfig(LG_ComputerTerminal terminal) => uplinkTerminalConfigs.ContainsKey(terminal.Pointer) ? uplinkTerminalConfigs[terminal.Pointer] : null;

        private void BuildUplinkOverrides()
        {
            if (!uplinks.ContainsKey(RundownManager.ActiveExpedition.LevelLayoutData)) return;
            uplinks[RundownManager.ActiveExpedition.LevelLayoutData].Uplinks.ForEach(Build);
        }

        private void OnBuildDone()
        {
            if(UplinkAddrLogContentBlock == null)
            {
                UplinkAddrLogContentBlock = GameDataBlockBase<TextDataBlock>.GetBlock("InGame.UplinkTerminal.UplinkAddrLog");
            }
            BuildUplinkOverrides();
        }

        private void OnLevelCleanup()
        {
            foreach(var _override in uplinkTerminalConfigs.Values)
            {
                foreach(var roundOverride in _override.RoundOverrides)
                {
                    roundOverride.ChainedPuzzleToEndRoundInstance = null;
                }
            }

            uplinkTerminalConfigs.Clear();
        }

        private TerminalUplinkOverrideManager() { }

        static TerminalUplinkOverrideManager()
        {
            Current = new();
            LevelAPI.OnBuildDone += Current.OnBuildDone;
            LevelAPI.OnLevelCleanup += Current.OnLevelCleanup;
        }

        private void SetupUplinkPuzzle(TerminalUplinkPuzzle uplinkPuzzle, LG_ComputerTerminal terminal, TerminalUplink config)
        {
            uplinkPuzzle.m_rounds = new List<TerminalUplinkPuzzleRound>().ToIl2Cpp();
            uplinkPuzzle.TerminalUplinkIP = SerialGenerator.GetIpAddress();
            uplinkPuzzle.m_roundIndex = 0;
            uplinkPuzzle.m_lastRoundIndexToUpdateGui = -1;
            uplinkPuzzle.m_position = terminal.transform.position;
            uplinkPuzzle.IsCorrupted = config.SetupAsCorruptedUplink && terminal.CorruptedUplinkReceiver != null;
            uplinkPuzzle.m_terminal = terminal;
            uint verificationRound = Math.Max(config.NumberOfVerificationRounds, 1u);
            for (int i = 0; i < verificationRound; ++i)
            {
                int candidateWords = 6;
                TerminalUplinkPuzzleRound uplinkPuzzleRound = new TerminalUplinkPuzzleRound()
                {
                    CorrectIndex = Builder.SessionSeedRandom.Range(0, candidateWords, "NO_TAG"),
                    Prefixes = new string[candidateWords],
                    Codes = new string[candidateWords]
                };

                for (int j = 0; j < candidateWords; ++j)
                {
                    uplinkPuzzleRound.Codes[j] = SerialGenerator.GetCodeWord();
                    uplinkPuzzleRound.Prefixes[j] = SerialGenerator.GetCodeWordPrefix();
                }
                uplinkPuzzle.m_rounds.Add(uplinkPuzzleRound);
            }
        }
    }
}