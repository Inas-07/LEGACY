using GameData;
using LEGACY.Utils;
using ExtraObjectiveSetup.ExtendedWardenEvents;

namespace LEGACY.ExtraEvents
{
    enum EventType
    {
        // ==== misc ====
        CloseSecurityDoor_Custom = 100,
        SetTimerTitle = 102,

        // ==== alert enemies ====
        AlertEnemiesInZone = 107,
        AlertEnemiesInArea = 108,

        // ==== terminal ====
        Terminal_ShowTerminalInfoInZone = 130,
        Terminal_ToggleState = 131, 

        // ==== kill enemies ====
        KillEnemiesInArea = 140,
        KillEnemiesInZone = 141,
        KillEnemiesInDimension = 142,

        // ==== generator cluster =====
        PlayGCEndSequence = 180,

        // ==== chained puzzle =====
        ChainedPuzzle_AddReqItem = 200,
        ChainedPuzzle_RemoveReqItem,

        // ==== spawn hibernate =====
        SpawnHibernate = 170,
        Info_ZoneHibernate = 250,
        Info_LevelHibernate = 251,
        Output_LevelHibernateSpawnEvent = 252,

        // ==== warp ====
        TP_WarpTeams = 160,
        TP_WarpPlayersInRange = 161,
        TP_WarpItemsInZone = 162,

        // ==== force level failed ====
        FF_ToggleFFCheck = 210,  // do not use
        FF_AddPlayersInRangeToCheck = 211,
        FF_AddPlayersOutOfRangeToCheck = 212,
        FF_ToggleCheckOnGroup = 213,
        FF_Reset = 214,
        FF_ResetGroup = 215,
        FF_SetExpeditionFailedText = 216,
        FF_ResetExpeditionFailedText = 217,

        // ==== misc ====
        SetNavMarker = 220,
        ToggleDummyVisual = 221,
        ToggleLSFBState = 222,

        // ==== custom play sound ====
        PlayMusic = 260,
        StopMusic = 261,

        // event scan
        ToggleEventScanState = 270,
    }

    internal static partial class LegacyExtraEvents
    {
        private static bool initialized = false;

        internal static void Init()
        {
            if (initialized) return;

            LegacyLogger.Log("Adding Legacy warden event definitions...");

            // ==== misc ====
            EOSWardenEventManager.Current.AddEventDefinition(EventType.CloseSecurityDoor_Custom.ToString(), (uint)EventType.CloseSecurityDoor_Custom, CloseSecurityDoor);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.SetTimerTitle.ToString(), (uint)EventType.SetTimerTitle, SetTimerTitle);

            // ==== alert enemies ====
            EOSWardenEventManager.Current.AddEventDefinition(EventType.AlertEnemiesInArea.ToString(), (uint)EventType.AlertEnemiesInArea, AlertEnemiesInArea);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.AlertEnemiesInZone.ToString(), (uint)EventType.AlertEnemiesInZone, AlertEnemiesInZone);

            // ==== kill enemies ====
            EOSWardenEventManager.Current.AddEventDefinition(EventType.KillEnemiesInArea.ToString(), (uint)EventType.KillEnemiesInArea, KillEnemiesInArea);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.KillEnemiesInZone.ToString(), (uint)EventType.KillEnemiesInZone, KillEnemiesInZone);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.KillEnemiesInDimension.ToString(), (uint)EventType.KillEnemiesInDimension, KillEnemiesInDimension);

            // ==== spawn hibernate ====
            EOSWardenEventManager.Current.AddEventDefinition(EventType.SpawnHibernate.ToString(), (uint)EventType.SpawnHibernate, SpawnHibernate);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.Info_ZoneHibernate.ToString(), (uint)EventType.Info_ZoneHibernate, Info_ZoneHibernate);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.Info_LevelHibernate.ToString(), (uint)EventType.Info_LevelHibernate, Info_LevelEnemies);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.Output_LevelHibernateSpawnEvent.ToString(), (uint)EventType.Output_LevelHibernateSpawnEvent, Output_LevelHibernateSpawnEvent);

            // ==== generator / generator cluster====
            EOSWardenEventManager.Current.AddEventDefinition(EventType.PlayGCEndSequence.ToString(), (uint)EventType.PlayGCEndSequence, PlayGCEndSequence);

            // ==== terminal ====
            EOSWardenEventManager.Current.AddEventDefinition(EventType.Terminal_ToggleState.ToString(), (uint)EventType.Terminal_ToggleState, ToggleTerminalState);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.Terminal_ShowTerminalInfoInZone.ToString(), (uint)EventType.Terminal_ShowTerminalInfoInZone, ShowTerminalInfoInZone);

            // ==== warpping ====
            EOSWardenEventManager.Current.AddEventDefinition(EventType.TP_WarpTeams.ToString(), (uint)EventType.TP_WarpTeams, WarpTeam);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.TP_WarpPlayersInRange.ToString(), (uint)EventType.TP_WarpPlayersInRange, WarpAlivePlayersAndItemsInRange);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.TP_WarpItemsInZone.ToString(), (uint)EventType.TP_WarpItemsInZone, WarpItemsInZone);

            // ==== force fail check ====
            EOSWardenEventManager.Current.AddEventDefinition(EventType.FF_ToggleFFCheck.ToString(), (uint)EventType.FF_ToggleFFCheck, ToggleFFCheckGroup);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.FF_AddPlayersInRangeToCheck.ToString(), (uint)EventType.FF_AddPlayersInRangeToCheck, AddPlayersInRangeToFFCheckGroup);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.FF_AddPlayersOutOfRangeToCheck.ToString(), (uint)EventType.FF_AddPlayersOutOfRangeToCheck, AddPlayersOutOfRangeToFFCheckGroup);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.FF_ToggleCheckOnGroup.ToString(), (uint)EventType.FF_ToggleCheckOnGroup, ToggleFFCheckOnGroup);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.FF_Reset.ToString(), (uint)EventType.FF_Reset, ResetFFCheck);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.FF_ResetGroup.ToString(), (uint)EventType.FF_ResetGroup, ResetFFCheckGroup);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.FF_SetExpeditionFailedText.ToString(), (uint)EventType.FF_SetExpeditionFailedText, SetExpeditionFailedText);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.FF_ResetExpeditionFailedText.ToString(), (uint)EventType.FF_ResetExpeditionFailedText, ResetExpeditionFailedText);
            
            // ==== misc ====
            EOSWardenEventManager.Current.AddEventDefinition(EventType.SetNavMarker.ToString(), (uint)EventType.SetNavMarker, SetNavMarker);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.ToggleDummyVisual.ToString(), (uint)EventType.ToggleDummyVisual, ToggleDummyVisual);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.ToggleLSFBState.ToString(), (uint)EventType.ToggleLSFBState, ToggleLevelSpawnedFogBeaconState);

            // ==== custom play sound ====
            EOSWardenEventManager.Current.AddEventDefinition(EventType.PlayMusic.ToString(), (uint)EventType.PlayMusic, PlayMusic);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.StopMusic.ToString(), (uint)EventType.StopMusic, StopMusic);

            // ==== event scan state toggle ====
            EOSWardenEventManager.Current.AddEventDefinition(EventType.ToggleEventScanState.ToString(), (uint)EventType.ToggleEventScanState, ToggleEventScanState);

            // ==== vanilla event override ====
            EOSWardenEventManager.Current.AddEventDefinition(eWardenObjectiveEventType.ActivateChainedPuzzle.ToString(), (uint)eWardenObjectiveEventType.ActivateChainedPuzzle, ActivateChainedPuzzle);
            EOSWardenEventManager.Current.AddEventDefinition(eWardenObjectiveEventType.SetTerminalCommand.ToString(), (uint)eWardenObjectiveEventType.SetTerminalCommand, SetTerminalCommand_Custom);

            //if (!Debugger.Current.DEBUGGING)
            //{
            //    EOSWardenEventManager.Current.AddEventDefinition(eWardenObjectiveEventType.ActivateChainedPuzzle.ToString(), (uint)eWardenObjectiveEventType.ActivateChainedPuzzle, ActivateChainedPuzzle);
            //    EOSWardenEventManager.Current.AddEventDefinition(eWardenObjectiveEventType.SetTerminalCommand.ToString(), (uint)eWardenObjectiveEventType.SetTerminalCommand, SetTerminalCommand_Custom);
            //}
            //else
            //{
            //    LegacyLogger.Error("Debugging active - vanilla event definition un-overwritten");
            //}

            LegacyLogger.Log("Legacy warden event definitions setup completed");
            initialized = true;
        }
    }
}