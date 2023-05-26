using ChainedPuzzles;
using GameData;
using LevelGeneration;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LEGACY.LegacyOverride.Terminal
{
    public enum UplinkTerminal
    {
        SENDER,
        RECEIVER
    }

    public class Terminal
    {
        public eDimensionIndex DimensionIndex { get; set; }

        public LG_LayerType LayerType { get; set; }

        public eLocalZoneIndex LocalIndex { get; set; }

        public int TerminalIndex { get; set; } = -1;
    }

    public class UplinkRound 
    {
        public int RoundIndex { get; set; } = -1;

        public uint ChainedPuzzleToEndRound { get; set; } = 0u;

        public UplinkTerminal BuildChainedPuzzleOn { get; set; } = UplinkTerminal.SENDER;

        [JsonIgnore]
        public ChainedPuzzleInstance ChainedPuzzleToEndRoundInstance { get; set; } = null;

        public TimeSettings OverrideTimeSettings { get; set; } = new();

        // trigger is not ignored: 
        // 1 - OnUplinkRound StartWaitingForVerify
        // 2 - OnUplinkVerify Correct, building signature
        public List<WardenObjectiveEventData> EventsOnRound { get; set; } = new();
    }

    public class TimeSettings 
    {
        // using vanilla default value
        public float TimeToStartVerify { set; get; } = 5f;

        public float TimeToCompleteVerify { set; get; } = 6f;

        public float TimeToRestoreFromFail { set; get; } = 6f;
    }

    public class TerminalUplink
    {
        public eDimensionIndex DimensionIndex { get; set; }

        public LG_LayerType LayerType { get; set; }

        public eLocalZoneIndex LocalIndex { get; set; }

        public int TerminalIndex { get; set; } = -1;

        public bool DisplayUplinkWarning { get; set; } = true;

        public bool SetupAsCorruptedUplink { get; set; } = false;

        public Terminal CorruptedUplinkReceiver { get; set; } = new();

        public bool UseUplinkAddress { get; set; } = true;

        public Terminal UplinkAddressLogPosition { get; set; } = new();

        public uint ChainedPuzzleToStartUplink { set; get; } = 0u;

        public uint NumberOfVerificationRounds { get; set; } = 1u;

        public TimeSettings DefaultTimeSettings { get; set; } = new();

        public List<UplinkRound> RoundOverrides { get; set; } = new() { new() };

        // same as specifying OnStart event in RoundOverrides with RoundIndex 0
        public List<WardenObjectiveEventData> EventsOnCommence { set; get; } = new();

        // same as specifying OnMid event in RoundOverrides with RoundIndex -> last round
        public List<WardenObjectiveEventData> EventsOnComplete { set; get; } = new();
    }

    public class LevelTerminalUplinks
    {
        public uint MainLevelLayout { set; get; }

        public List<TerminalUplink> Uplinks { set; get; } = new() { new() };
    }
}
