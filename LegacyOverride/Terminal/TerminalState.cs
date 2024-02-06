using ExtraObjectiveSetup;
using FloLib.Networks.Replications;
using GTFO.API;
using LEGACY.Utils;
using LevelGeneration;
using Player;
using System;
using System.Collections.Generic;

namespace LEGACY.LegacyOverride.Terminal
{
    public struct TerminalState
    {
        public bool Enabled = true;

        public TerminalState() { }

        public TerminalState(bool Enabled) { this.Enabled = Enabled; }

        public TerminalState(TerminalState o) { Enabled = o.Enabled; }
    }
}
