using GameData;
using LevelGeneration;
using System;
using GTFO.API;
using LEGACY.Utils;
using SNetwork;
using GTFO.API.Utilities;

namespace LEGACY.Reactor
{
    internal class ReactorConfigManager
    {
        public static readonly ReactorConfigManager Current;

        private LG_WardenObjective_Reactor[] reactors = new LG_WardenObjective_Reactor[3];

        private LiveEditListener listener = null;

        public LG_WardenObjective_Reactor FindReactor(LG_LayerType layer)
        {
            if (reactors[(int)layer] != null) return reactors[(int)layer];

            LG_WardenObjective_Reactor reactor = null;
            foreach (var keyvalue in WardenObjectiveManager.Current.m_wardenObjectiveItem)
            {
                if (keyvalue.Key.Layer != layer)
                    continue;

                reactor = keyvalue.Value.TryCast<LG_WardenObjective_Reactor>();
                if (reactor == null)
                    continue;

                break;
            }

            reactors[(int)layer] = reactor;
            return reactor;
        }

        public LG_ComputerTerminal FindTerminalWithTerminalSerial(string itemKey, eDimensionIndex dim, LG_LayerType layer, eLocalZoneIndex localIndex)
        {
            LG_Zone zone = null;
            if (Builder.CurrentFloor.TryGetZoneByLocalIndex(dim, layer, localIndex, out zone) && zone != null)
            {
                foreach (var terminal in zone.TerminalsSpawnedInZone)
                {
                    if (terminal.ItemKey.Equals(itemKey, StringComparison.InvariantCultureIgnoreCase))
                        return terminal;
                }
            }

            return null;
        }

        public TerminalLogFileData GetLocalLog(LG_ComputerTerminal terminal, string logName)
        {
            var localLogs = terminal.GetLocalLogs();
            logName = logName.ToUpperInvariant();
            foreach (var log in localLogs)
            {
                Logger.Warning(log.Key);
            }

            return localLogs.ContainsKey(logName) ? localLogs[logName] : null;
        }

        public void Init(string configPath)
        {
            listener = LiveEdit.CreateListener(EntryPoint.LEGACY_CUSTOM_PATH, "*.json", true);
        }

        // ISSUE: Log is not synced, which means dropping an on-going session could lead to log desync
        public void MoveVerifyLog(WardenObjectiveEventData e)
        {
            // use WorldEventObjectFilter to specify reactor layer
            // e.Layer to specify target layer

            LG_LayerType reactorLayer = LG_LayerType.MainLayer;
            
            switch(e.WorldEventObjectFilter.ToUpper())
            {
                case "MAINLAYER": reactorLayer = LG_LayerType.MainLayer; break;
                case "SECONDARYLAYER": reactorLayer = LG_LayerType.SecondaryLayer; break;
                case "THIRDLAYER": reactorLayer = LG_LayerType.ThirdLayer; break;
                default:
                    Logger.Error("Invalid `WorldEventObjectFilter`, must be one of `MainLayer`, `SecondaryLayer`, `ThirdLayer`");
                    return;
            }

            WardenObjectiveDataBlock data;
            if (!WardenObjectiveManager.Current.TryGetActiveWardenObjectiveData(reactorLayer, out data) || data == null)
            {
                Logger.Error("MoveVerifyLog: Cannot get WardenObjectiveDataBlock");
                return;
            }

            if (data.Type != eWardenObjectiveType.Reactor_Startup)
            {
                Logger.Error($"MoveVerifyLog: {reactorLayer} is not ReactorStartup. MoveVerifyLog is invalid.");
                return;
            }

            LG_WardenObjective_Reactor reactor = FindReactor(reactorLayer);

            if (reactor == null)
            {
                Logger.Error($"MoveVerifyLog: Cannot find reactor in {reactorLayer}.");
                return;
            }

            // e.Count: 1-based, actual wave number
            int waveIndex = e.Count;
            if(waveIndex >= data.ReactorWaves.Count || waveIndex < 0)
            {
                Logger.Error($"MoveVerifyLog: invalid wave index: {e.Count}.");
                return;
            }

            var waveData = data.ReactorWaves[waveIndex];
            if (!waveData.VerifyInOtherZone)
            {
                Logger.Error($"MoveVerifyLog: waveIndex {waveIndex} -> reactor wave doesn't have any verify log.");
                return;
            }

            LG_ComputerTerminal logTerminal = FindTerminalWithTerminalSerial(waveData.VerificationTerminalSerial, reactor.SpawnNode.m_dimension.DimensionIndex, reactor.SpawnNode.LayerType, waveData.ZoneForVerification);
            if(logTerminal == null)
            {
                Logger.Error($"MoveVerifyLog: Cannot find wave log terminal. Wave index: {waveIndex}");
                return;
            }

            LG_Zone moveToZone;
            if(!Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out moveToZone) || moveToZone == null)
            {
                Logger.Error($"MoveVerifyLog: Cannot find target zone {e.LocalIndex}, {e.Layer}, {e.DimensionIndex}.");
                return;
            }

            if(moveToZone.TerminalsSpawnedInZone == null || moveToZone.TerminalsSpawnedInZone.Count < 0)
            {
                Logger.Error($"MoveVerifyLog: No spawned terminal found in target zone {e.LocalIndex}, {e.Layer}, {e.DimensionIndex}..");
                return;
            }

            int sessionSeed = Builder.SessionSeedRandom.Seed;
            if(sessionSeed < 0)
            {
                sessionSeed = sessionSeed != int.MinValue ? -sessionSeed : int.MaxValue;
            }

            int terminalIndex = sessionSeed % moveToZone.TerminalsSpawnedInZone.Count;
            LG_ComputerTerminal targetTerminal = moveToZone.TerminalsSpawnedInZone[terminalIndex];

            string logName = waveData.VerificationTerminalFileName.ToUpperInvariant();
            var verifyLog = GetLocalLog(logTerminal, logName);
            if(verifyLog == null)
            {
                Logger.Error("Cannot find reactor verify log on terminal.");
                return;
            }

            logTerminal.SetLogVisible(logName, false);
            targetTerminal.AddLocalLog(verifyLog, true);

            waveData.VerificationTerminalSerial = targetTerminal.ItemKey;
            logTerminal.m_command.ClearOutputQueueAndScreenBuffer();
            logTerminal.m_command.AddInitialTerminalOutput();
            targetTerminal.m_command.ClearOutputQueueAndScreenBuffer();
            targetTerminal.m_command.AddInitialTerminalOutput();

            Logger.Debug($"MoveVerifyLog: moved wave {waveIndex} verify log from {logTerminal.ItemKey} to {targetTerminal.ItemKey}");
        }

        internal void CompleteCurrentReactorWave(WardenObjectiveEventData e)
        {
            if (!SNet.IsMaster) return;

            WardenObjectiveDataBlock data;
            if (!WardenObjectiveManager.Current.TryGetActiveWardenObjectiveData(e.Layer, out data) || data == null)
            {
                Logger.Error("CompleteCurrentReactorWave: Cannot get WardenObjectiveDataBlock");
                return;
            }
            if (data.Type != eWardenObjectiveType.Reactor_Startup)
            {
                Logger.Error($"CompleteCurrentReactorWave: {e.Layer} is not ReactorStartup. CompleteCurrentReactorWave is invalid.");
                return;
            }

            LG_WardenObjective_Reactor reactor = FindReactor(e.Layer);

            if (reactor == null)
            {
                Logger.Error($"CompleteCurrentReactorWave: Cannot find reactor in {e.Layer}.");
                return;
            }

            switch (reactor.m_currentState.status)
            {
                case eReactorStatus.Inactive_Idle:
                    reactor.AttemptInteract(eReactorInteraction.Initiate_startup);
                    reactor.m_terminal.TrySyncSetCommandHidden(TERM_Command.ReactorStartup);
                    break;
                case eReactorStatus.Startup_complete:
                    Logger.Error($"CompleteCurrentReactorWave: Startup already completed for {e.Layer} reactor");
                    break;
                case eReactorStatus.Active_Idle:
                case eReactorStatus.Startup_intro:
                case eReactorStatus.Startup_intense:
                case eReactorStatus.Startup_waitForVerify:
                    if (reactor.m_currentWaveCount == reactor.m_waveCountMax)
                        reactor.AttemptInteract(eReactorInteraction.Finish_startup);
                    else
                        reactor.AttemptInteract(eReactorInteraction.Verify_startup);
                    break;
            }

            Logger.Debug($"CompleteCurrentReactorWave: Current reactor wave for {e.Layer} completed");
        }

        private void OnLevelCleanup()
        {
            for (int i = 0; i < reactors.Length; i++)
                reactors[i] = null;
        }

        private ReactorConfigManager() { }

        static ReactorConfigManager()
        {
            Current = new();
            LevelAPI.OnLevelCleanup += Current.OnLevelCleanup;
        }
    }
}
