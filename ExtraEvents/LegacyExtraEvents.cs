using GameData;
using LEGACY.Utils;
using ExtraObjectiveSetup.ExtendedWardenEvents;
using LEGACY.VanillaFix;

namespace LEGACY.ExtraEvents
{
    enum EventType
    {
        // ==== misc ====
        CloseSecurityDoor_Custom = 100,
        SetTimerTitle = 102,
        ToggleBioTrackerState = 210,
        TP_WarpTeamsToArea = 160,

        // ==== alert enemies ====
        AlertEnemiesInZone = 107,
        AlertEnemiesInArea = 108,

        // ==== terminal
        Terminal_ShowTerminalInfoInZone = 130,
        Terminal_ToggleEnableDisable, // unimplemented

        // ==== kill enemies ====
        KillEnemiesInArea = 140,
        KillEnemiesInZone = 141,
        KillEnemiesInDimension = 142,

        // ==== reactor =====
        //Reactor_Startup = 150,
        //Reactor_CompleteCurrentVerify = 151,

        // ==== generator cluster =====
        PlayGCEndSequence = 180,

        // ==== chained puzzle =====
        ChainedPuzzle_AddReqItem = 200,
        ChainedPuzzle_RemoveReqItem,

        // ==== spawn hibernate =====
        SpawnHibernate = 170,
        Info_ZoneHibernate = 250,
        Info_LevelHibernate = 251,
        Output_LevelHibernateSpawnEvent = 252
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

            //EOSWardenEventManager.Current.AddEventDefinition(EventType.Terminal_ToggleEnableDisable.ToString(), (uint)EventType.Terminal_ToggleEnableDisable, ToggleEnableDisableTerminal);
            EOSWardenEventManager.Current.AddEventDefinition(EventType.Terminal_ShowTerminalInfoInZone.ToString(), (uint)EventType.Terminal_ShowTerminalInfoInZone, ShowTerminalInfoInZone);

            if (!Debugger.Current.DEBUGGING)
            {
                EOSWardenEventManager.Current.AddEventDefinition(eWardenObjectiveEventType.ActivateChainedPuzzle.ToString(), (uint)eWardenObjectiveEventType.ActivateChainedPuzzle, ActivateChainedPuzzle);
                EOSWardenEventManager.Current.AddEventDefinition(eWardenObjectiveEventType.SetTerminalCommand.ToString(), (uint)eWardenObjectiveEventType.SetTerminalCommand, SetTerminalCommand_Custom);
            }
            else
            {
                LegacyLogger.Error("Debugging active - vanilla event definition un-overwritten");
            }

            LegacyLogger.Log("Legacy warden event definitions setup completed");
            initialized = true;
        }
    }
}