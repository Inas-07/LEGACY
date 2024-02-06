using GTFO.API;
using LEGACY.Utils;
using System.Collections.Generic;
using Localization;
using Player;

namespace LEGACY.LegacyOverride.ForceFail
{
    // NOTE: patching WardenObjectiveManager.CheckExpeditionFailed required 
    public class ForceFailManager
    {
        public const int MAX_FF_GROUP_CNT = 4;

        public static ForceFailManager Current { get; private set; } = new();

        private List<FFPlayerGroup> playerGroups = new();

        public void AddPlayerToGroup(int playerSlotIndex, int checkGroupIndex)
        {
            if (checkGroupIndex < 0 || checkGroupIndex >= playerGroups.Count)
            {
                LegacyLogger.Error($"FFManager: check group index should be non-negative and less than {playerGroups.Count} - got {checkGroupIndex}");
                return;
            }

            var pg = playerGroups[checkGroupIndex];
            pg.SetPlayerInGroup(playerSlotIndex, true);
        }

        public void ResetSynced()
        {
            playerGroups.ForEach(pg => pg.ResetSynced());
        }

        public void ResetUnSynced()
        {
            playerGroups.ForEach(pg => pg.ResetUnsynced());
        }

        public void ResetGroupSynced(int groupIndex)
        {
            if (groupIndex < 0 || groupIndex >= playerGroups.Count)
            {
                LegacyLogger.Error($"FFManager: check group index should be non-negative and less than {playerGroups.Count} - got {groupIndex}");
                return;
            }
            playerGroups[groupIndex].ResetSynced();
        }

        public void ToggleCheck(bool enabled) => playerGroups.ForEach(pg => pg.Toggle(enabled));

        public bool IsCheckEnabled()
        {
            foreach(var pg in playerGroups)
            {
                if(pg.enabled && pg.NumPlayersInGroup() > 0) return true;
            }

            return false;
        }

        public bool ToggleCheckOnGroup(int groupIndex, bool enabled)
        {
            if (groupIndex < 0 || groupIndex >= playerGroups.Count)
            {
                LegacyLogger.Error($"FFManager: group index should be non-negative and less than {playerGroups.Count} - got {groupIndex}");
                return false;
            }

            playerGroups[groupIndex].Toggle(enabled);
            return true;
        }

        public void SetExpeditionFailedText(string text)
        {
            MainMenuGuiLayer.Current.PageExpeditionFail.m_missionFailed_text.SetText(text);
        }

        public void ResetExpeditionFailedText()
        {
            string t = Text.Get(962);
            MainMenuGuiLayer.Current.PageExpeditionFail.m_missionFailed_text.SetText(t);
        }

        // this is invoked only on master side
        internal bool CheckLevelForceFailed()
        {
            bool levelFailed = false;
            foreach (var pg in playerGroups)
            {
                if (!pg.enabled) continue;
                
                bool anyCheckedPlayerAlive = false;
                int checkedCnt = 0;
                foreach (var player in PlayerManager.PlayerAgentsInLevel)
                {
                    if(pg.PlayerInGroup(player.PlayerSlotIndex))
                    {
                        anyCheckedPlayerAlive = player.Alive || anyCheckedPlayerAlive;
                        checkedCnt += 1;
                    }
                }

                if (anyCheckedPlayerAlive)
                {
                    if (checkedCnt == pg.NumPlayersInGroup()) // all target player checked, and one of them is alive
                    {
                        continue;
                    }
                    else // some target slot has no player (i.e. the player has left the lobby) - deem failed 
                    {
                        levelFailed = true;
                        break;
                    }
                }
                else  // all players in the group are dead
                {
                    levelFailed = true;
                    break;
                }
            }

            return levelFailed;
        }

        public void Init() { }

        private void Setup()
        {
            for (int i = 0; i < MAX_FF_GROUP_CNT; i++)
            {
                var g = FFPlayerGroup.Instantiate();
                if (g != null)
                {
                    playerGroups.Add(g);
                }
                else
                {
                    LegacyLogger.Error($"Instantiated player group num: {playerGroups.Count}");
                    break;
                }
            }
        }

        private ForceFailManager()
        {
            LevelAPI.OnBuildStart += ResetUnSynced;
            LevelAPI.OnLevelCleanup += ResetUnSynced;
            
            LevelAPI.OnBuildStart += ResetExpeditionFailedText;
            LevelAPI.OnBuildDone += ResetExpeditionFailedText;

            EventAPI.OnManagersSetup += Setup;
        }

        static ForceFailManager() { }
    }
}
