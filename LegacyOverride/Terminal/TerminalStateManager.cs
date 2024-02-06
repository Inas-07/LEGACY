using ExtraObjectiveSetup;
using GTFO.API;
using LEGACY.Utils;
using LevelGeneration;
using System;
using System.Collections.Generic;

namespace LEGACY.LegacyOverride.Terminal
{
    public class TerminalStateManager
    {
        public static TerminalStateManager Current { get; private set; } = new();

        private Dictionary<IntPtr, TerminalWrapper> terminals = new();

        public void SetupTerminal(LG_ComputerTerminal terminal)
        {
            uint allotedID = EOSNetworking.AllotReplicatorID();
            if (allotedID == EOSNetworking.INVALID_ID)
            {
                LegacyLogger.Error($"TerminalStateManager: replicator ID depleted, cannot setup terminal...");
                return;
            }

            TerminalWrapper t = TerminalWrapper.Instantiate(terminal, allotedID);
            terminals[terminal.Pointer] = t;
        }

        public TerminalWrapper Get(LG_ComputerTerminal lgTerminal) => terminals.ContainsKey(lgTerminal.Pointer) ? terminals[lgTerminal.Pointer] : null;

        private void Clear()
        {
            terminals.Clear();
        }

        private TerminalStateManager()
        {
            LevelAPI.OnLevelCleanup += Clear;
            LevelAPI.OnBuildStart += Clear;
        }
    }
}
