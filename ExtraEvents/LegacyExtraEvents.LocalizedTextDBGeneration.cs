using GameData;
using System.Collections.Generic;
using Il2cppEventList = Il2CppSystem.Collections.Generic.List<GameData.WardenObjectiveEventData>;
using Il2cppWorldEventList = Il2CppSystem.Collections.Generic.List<GameData.WorldEventFromSourceData>;
using Il2cppTerminalCmdList = Il2CppSystem.Collections.Generic.List<GameData.CustomTerminalCommand>;
using Il2cppTerminalLogList = Il2CppSystem.Collections.Generic.List<GameData.TerminalLogFileData>;
using System.Linq;
using LEGACY.Utils;
using ScanPosOverride.Managers;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private class MyTextDatablock
        {
            public bool SkipLocalization { get; set; } = false;

            public bool MachineTranslation { get; set; } = false;

            public string English { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;

            public int CharacterMetaData { get; set; } = 5;

            public int ExportVersion { get; set; } = 1;

            public int ImportVersion { get; set; } = 1;

            public string name { get; set; } = string.Empty;

            public bool internalEnabled { get; set; } = true;

            public string persistentID { get; set; } = string.Empty;

            public string datablock { get; set; } = "Text";
        }

        private static void GenerateLocalizedTextDatablockForActiveExpedition(WardenObjectiveEventData _)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel 
                && GameStateManager.CurrentStateName != eGameStateName.Lobby) return;

            var ActiveExpedition = RundownManager.ActiveExpedition;

            var MainLayout = GameDataBlockBase<LevelLayoutDataBlock>.GetBlock(ActiveExpedition.LevelLayoutData);
            var SecondaryLayout = ActiveExpedition.SecondaryLayerEnabled ?
                GameDataBlockBase<LevelLayoutDataBlock>.GetBlock(ActiveExpedition.SecondaryLayout) : null;
            var ThirdLayout = ActiveExpedition.ThirdLayerEnabled ? 
                GameDataBlockBase<LevelLayoutDataBlock>.GetBlock(ActiveExpedition.ThirdLayout) : null;

            var DimensionLayout = ActiveExpedition.DimensionDatas.ToManagedList()
                .ConvertAll(e => GameDataBlockBase<DimensionDataBlock>.GetBlock(e.DimensionData))
                .ConvertAll(e => e == null ? null : GameDataBlockBase<LevelLayoutDataBlock>.GetBlock(e.DimensionData.LevelLayoutData))
                .SkipWhile(e => e == null);

            var MainObjectiveDB = ActiveExpedition.MainLayerData.ChainedObjectiveData
                .ToManagedList().ConvertAll(e => GameDataBlockBase<WardenObjectiveDataBlock>.GetBlock(e.DataBlockId))
                .Prepend(GameDataBlockBase<WardenObjectiveDataBlock>.GetBlock(ActiveExpedition.MainLayerData.ObjectiveData.DataBlockId))
                .SkipWhile(e => e == null);

            var SecondaryObjectiveDB = !ActiveExpedition.SecondaryLayerEnabled ? null : ActiveExpedition.SecondaryLayerData.ChainedObjectiveData
                .ToManagedList().ConvertAll(e => GameDataBlockBase<WardenObjectiveDataBlock>.GetBlock(e.DataBlockId))
                .Prepend(GameDataBlockBase<WardenObjectiveDataBlock>.GetBlock(ActiveExpedition.MainLayerData.ObjectiveData.DataBlockId))
                .SkipWhile(e => e == null);

            var ThirdObjectiveDB = !ActiveExpedition.ThirdLayerEnabled ? null : ActiveExpedition.ThirdLayerData.ChainedObjectiveData
                .ToManagedList().ConvertAll(e => GameDataBlockBase<WardenObjectiveDataBlock>.GetBlock(e.DataBlockId))
                .Prepend(GameDataBlockBase<WardenObjectiveDataBlock>.GetBlock(ActiveExpedition.ThirdLayerData.ObjectiveData.DataBlockId))
                .SkipWhile(e => e == null);

            Dictionary<string, string> untranslatedText2StringPID = new();

            void ProcessWardenEvents(Il2cppEventList events)
            {
                foreach(var e in events)
                {
                    untranslatedText2StringPID[e.WardenIntel.UntranslatedText] = string.Empty;
                    untranslatedText2StringPID[e.CustomSubObjectiveHeader.UntranslatedText] = string.Empty;
                    untranslatedText2StringPID[e.CustomSubObjective] = string.Empty;
                }
            }

            void ProcessWorldEvents(Il2cppWorldEventList events)
            {
                foreach (var e in events)
                {
                    untranslatedText2StringPID[e.WardenIntel.UntranslatedText] = string.Empty;
                    untranslatedText2StringPID[e.CustomSubObjectiveHeader.UntranslatedText] = string.Empty;
                    untranslatedText2StringPID[e.CustomSubObjective] = string.Empty;
                }
            }

            void ProcessTerminalCommands(Il2cppTerminalCmdList commands)
            {
                foreach(var c in commands)
                {
                    untranslatedText2StringPID[c.CommandDesc.UntranslatedText] = string.Empty;
                    foreach(var outputLine in c.PostCommandOutputs)
                        untranslatedText2StringPID[outputLine.Output.UntranslatedText] = string.Empty;
                    
                    ProcessWardenEvents(c.CommandEvents);
                }
            }

            void ProcessTerminalLogs(Il2cppTerminalLogList logs)
            {
                foreach(var l in logs)
                {
                    untranslatedText2StringPID[l.FileContent.UntranslatedText] = string.Empty;
                }
            }

            void ProcessLevelLayoutData(LevelLayoutDataBlock db)
            {
                if(db == null) { return; }

                foreach(var zone in db.Zones)
                {
                    // EventsOnEnter is omitted cuz it doesnt accept localized text
                    ProcessWardenEvents(zone.EventsOnApproachDoor);
                    ProcessWardenEvents(zone.EventsOnUnlockDoor);
                    ProcessWardenEvents(zone.EventsOnDoorScanStart);
                    ProcessWardenEvents(zone.EventsOnDoorScanDone);
                    ProcessWardenEvents(zone.EventsOnOpenDoor);
                    ProcessWardenEvents(zone.EventsOnTerminalDeactivateAlarm);
                    ProcessWardenEvents(zone.EventsOnPortalWarp);
                    ProcessWorldEvents(zone.EventsOnTrigger); // Thankss to il2cpp I cant convert list of WorldEventFromSourceData to list of WardenObjectiveEventData :)
                
                    foreach(var t in zone.TerminalPlacements)
                    {
                        ProcessTerminalCommands(t.UniqueCommands);
                        ProcessTerminalLogs(t.LocalLogFiles);
                    }
                }
            }

            void ProcessWardenObjectiveDB(WardenObjectiveDataBlock db)
            {
                // header is omitted cuz it's useless
                untranslatedText2StringPID[db.MainObjective.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.FindLocationInfo.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.FindLocationInfoHelp.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.GoToZone.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.GoToZoneHelp.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.InZoneFindItem.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.InZoneFindItemHelp.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.SolveItem.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.SolveItemHelp.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.GoToWinCondition_Elevator.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.GoToWinConditionHelp_Elevator.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.GoToWinCondition_CustomGeo.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.GoToWinConditionHelp_CustomGeo.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.GoToWinCondition_ToMainLayer.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.GoToWinConditionHelp_ToMainLayer.UntranslatedText] = string.Empty;

                foreach(var w in db.WavesOnElevatorLand) 
                    untranslatedText2StringPID[w.IntelMessage.UntranslatedText] = string.Empty;
                
                ProcessWardenEvents(db.EventsOnElevatorLand);
            
                foreach(var w in db.WavesOnActivate)
                    untranslatedText2StringPID[w.IntelMessage.UntranslatedText] = string.Empty;

                ProcessWardenEvents(db.EventsOnActivate);

                foreach(var w in db.WavesOnGotoWin)
                    untranslatedText2StringPID[w.IntelMessage.UntranslatedText] = string.Empty;

                ProcessWardenEvents(db.EventsOnGotoWin);

                foreach (var w in db.ReactorWaves)
                    ProcessWardenEvents(w.Events);

                untranslatedText2StringPID[db.SpecialTerminalCommandDesc.UntranslatedText] = string.Empty;

                // PostCommandOutput is omitted cuz it's not localized text

                ProcessWardenEvents(db.ActivateHSU_Events);

                untranslatedText2StringPID[db.GatherTerminal_CommandHelp.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.GatherTerminal_DownloadingText.UntranslatedText] = string.Empty;
                untranslatedText2StringPID[db.GatherTerminal_DownloadCompleteText.UntranslatedText] = string.Empty;

                foreach(var elist in db.TimedTerminalSequence_EventsOnSequenceStart)
                    ProcessWardenEvents(elist);

                foreach (var elist in db.TimedTerminalSequence_EventsOnSequenceDone)
                    ProcessWardenEvents(elist);

                foreach (var elist in db.TimedTerminalSequence_EventsOnSequenceFail)
                    ProcessWardenEvents(elist);
            }

            ProcessLevelLayoutData(MainLayout);
            ProcessLevelLayoutData(SecondaryLayout);
            ProcessLevelLayoutData(ThirdLayout);
            foreach (var d in DimensionLayout) ProcessLevelLayoutData(d);
            
            foreach(var d in MainObjectiveDB) ProcessWardenObjectiveDB(d);
            foreach (var d in SecondaryObjectiveDB) ProcessWardenObjectiveDB(d);
            foreach (var d in ThirdObjectiveDB) ProcessWardenObjectiveDB(d);

            
        }
    }
}
