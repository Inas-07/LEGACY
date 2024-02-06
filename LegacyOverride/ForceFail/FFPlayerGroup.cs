using ExtraObjectiveSetup;
using FloLib.Networks.Replications;
using GTFO.API;
using LEGACY.Utils;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace LEGACY.LegacyOverride.ForceFail
{
    public class FFPlayerGroup
    {
        public const int MAX_PLAYER_CNT = 4;

        public bool enabled { get; private set; } = false;

        public ImmutableList<bool> IsPlayerInGroup => isPlayerInGroup.ToImmutableList();

        private List<bool> isPlayerInGroup = new();

        private StateReplicator<FFReplicationStruct> stateReplicator;

        public bool PlayerInGroup(int playerSlotIndex)
        {
            if (playerSlotIndex < 0 || playerSlotIndex >= MAX_PLAYER_CNT)
            {
                LegacyLogger.Error($"Invalid player slot index, should be in [0, {MAX_PLAYER_CNT}), but got {playerSlotIndex}");
                return false;
            }

            return isPlayerInGroup[playerSlotIndex];
        }

        public int NumPlayersInGroup() => isPlayerInGroup.Sum(b => b ? 1 : 0);
        
        public void Toggle(bool enabled)
        {
            this.enabled = enabled;
            Sync();
        }

        public void SetPlayerInGroup(int playerSlotIndex, bool inGroup)
        {
            if(playerSlotIndex < 0 || playerSlotIndex >= MAX_PLAYER_CNT)
            {
                LegacyLogger.Error($"Invalid player slot index, should be in [0, {MAX_PLAYER_CNT}), but got {playerSlotIndex}");
                return;
            }

            int prev = NumPlayersInGroup();
            isPlayerInGroup[playerSlotIndex] = inGroup;
            int cur = NumPlayersInGroup();
            if (prev == 0 && cur > 0)
            {
                enabled = true;
            }
            else if (prev > 0 && cur == 0)
            {
                enabled = false;
            }

            Sync();
        }

        public void ResetSynced()
        {
            Reset();
            Sync();
        }

        public void ResetUnsynced()
        {
            Reset();
            stateReplicator.SetStateUnsynced(GetSyncStruct());
        }

        private void Reset()
        {
            enabled = false;
            for(int i = 0; i < isPlayerInGroup.Count; i++)
            {
                isPlayerInGroup[i] = false;
            }
        }

        private void Sync() => stateReplicator.SetState(GetSyncStruct());
        
        private void OnStateChanged(FFReplicationStruct oldState, FFReplicationStruct newState, bool isRecall)
        {
            if (!isRecall) return;

            enabled = newState.enabled;
            isPlayerInGroup[0] = newState.checkP1;
            isPlayerInGroup[1] = newState.checkP2;
            isPlayerInGroup[2] = newState.checkP3;
            isPlayerInGroup[3] = newState.checkP4;
        }

        internal FFReplicationStruct GetSyncStruct() => new() {
            enabled = this.enabled,
            checkP1 = isPlayerInGroup[0],
            checkP2 = isPlayerInGroup[1],
            checkP3 = isPlayerInGroup[2],
            checkP4 = isPlayerInGroup[3],
        };

        internal static FFPlayerGroup Instantiate()
        {
            uint allotedID = EOSNetworking.AllotForeverReplicatorID();
            if (allotedID == EOSNetworking.INVALID_ID)
            {
                LegacyLogger.Error("ForceFailManager: cannot instantiate replicator - id depleted");
                return null;
            }

            var g = new FFPlayerGroup();
            g.stateReplicator = StateReplicator<FFReplicationStruct>.Create(allotedID, new FFReplicationStruct(), LifeTimeType.Forever);
            g.stateReplicator.OnStateChanged += g.OnStateChanged;

            g.Setup();

            return g;
        }

        private void Setup()
        {
            for (int _ = 0; _ < MAX_PLAYER_CNT; _++)
            {
                isPlayerInGroup.Add(false);
            }

            LevelAPI.OnBuildStart += ResetUnsynced;
            LevelAPI.OnLevelCleanup += ResetUnsynced;
        }

        private FFPlayerGroup() {}
    }
}
