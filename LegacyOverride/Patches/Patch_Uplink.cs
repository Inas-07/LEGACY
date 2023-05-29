using ChainedPuzzles;
using HarmonyLib;
using LEGACY.LegacyOverride.Terminal;
using LEGACY.Utils;
using LevelGeneration;
using Localization;
using GameData;
using SNetwork;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal class Patch_Uplink
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.Update))]
        private static void Post_LG_ComputerTerminal_Update(LG_ComputerTerminal __instance)
        {
            if (!__instance.m_isWardenObjective && __instance.UplinkPuzzle != null)
                __instance.UplinkPuzzle.UpdateGUI();
        }

        // neglected check:
        // null UplinkPuzzle check
        // null UplinkConfig check (if no config, then this method should not be called)

        // normal uplink: rewrite the method to do more things
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.TerminalUplinkConnect))]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_TerminalUplinkConnect(LG_ComputerTerminalCommandInterpreter __instance, string param1, string param2, ref bool __result)
        {
            var uplinkTerminal = __instance.m_terminal;

            if (uplinkTerminal.m_isWardenObjective) return true; // vanilla uplink

            if (LG_ComputerTerminalManager.OngoingUplinkConnectionTerminalId != 0U && LG_ComputerTerminalManager.OngoingUplinkConnectionTerminalId != uplinkTerminal.SyncID)
            {
                __instance.AddOngoingUplinkOutput();
                return false;
            }

            // custom uplink build
            var uplinkConfig = TerminalUplinkOverrideManager.Current.GetUplinkConfig(uplinkTerminal);

            if (!uplinkConfig.UseUplinkAddress)
            {
                param1 = __instance.m_terminal.UplinkPuzzle.TerminalUplinkIP;
            }

            if (!uplinkConfig.UseUplinkAddress || param1 == __instance.m_terminal.UplinkPuzzle.TerminalUplinkIP)
            {
                __instance.m_terminal.TrySyncSetCommandRule(TERM_Command.TerminalUplinkConnect, TERM_CommandRule.OnlyOnce);
                if (__instance.m_terminal.ChainedPuzzleForWardenObjective != null)
                {
                    __instance.m_terminal.ChainedPuzzleForWardenObjective.OnPuzzleSolved += new System.Action(() => __instance.StartTerminalUplinkSequence(param1));
                    __instance.AddOutput("");
                    __instance.AddOutput(Text.Get(3268596368));
                    __instance.AddOutput(Text.Get(3041541194));
                    if(SNet.IsMaster)
                    {
                        __instance.m_terminal.ChainedPuzzleForWardenObjective.AttemptInteract(eChainedPuzzleInteraction.Activate);
                    }
                }
                else
                    __instance.StartTerminalUplinkSequence(param1);
                __result = true;
            }
            else
            {
                __instance.AddUplinkWrongAddressError(param1);
                __result = false;
            }

            return false;
        }

        // rewrite the method to do more things
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.TerminalCorruptedUplinkConnect))]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_TerminalCorruptedUplinkConnect(LG_ComputerTerminalCommandInterpreter __instance, string param1, string param2, ref bool __result)
        {
            var sender = __instance.m_terminal;
            if (sender.m_isWardenObjective) return true; // vanilla uplink

            __result = false; // this method always return false

            var receiver = sender.CorruptedUplinkReceiver;
            if (receiver == null)
            {
                LegacyLogger.Error("TerminalCorruptedUplinkConnect() critical failure because terminal does not have a CorruptedUplinkReceiver.");
                return false;
            }

            if (LG_ComputerTerminalManager.OngoingUplinkConnectionTerminalId != 0U && LG_ComputerTerminalManager.OngoingUplinkConnectionTerminalId != sender.SyncID)
            {
                __instance.AddOngoingUplinkOutput();
                __result = false;
                return false;
            }

            LG_ComputerTerminalManager.OngoingUplinkConnectionTerminalId = sender.SyncID;

            var uplinkConfig = TerminalUplinkOverrideManager.Current.GetUplinkConfig(sender);
            if (uplinkConfig.UseUplinkAddress)
            {
                param1 = param1.ToUpper();
                LegacyLogger.Debug($"TerminalCorruptedUplinkConnect, param1: {param1}, TerminalUplink: {sender.UplinkPuzzle.ToString()}");
            }
            else
            {
                param1 = sender.UplinkPuzzle.TerminalUplinkIP.ToUpper();
                LegacyLogger.Debug($"TerminalCorruptedUplinkConnect, not using uplink address, TerminalUplink: {sender.UplinkPuzzle.ToString()}");
            }

            if (!uplinkConfig.UseUplinkAddress || param1 == sender.UplinkPuzzle.TerminalUplinkIP)
            {
                if (receiver.m_command.HasRegisteredCommand(TERM_Command.TerminalUplinkConfirm))
                {
                    sender.m_command.AddUplinkCorruptedOutput();
                }
                else
                {
                    sender.m_command.AddUplinkCorruptedOutput();
                    sender.m_command.AddOutput("");
                    sender.m_command.AddOutput(TerminalLineType.ProgressWait, string.Format(Text.Get(3492863045), receiver.PublicName), 3f);
                    sender.m_command.AddOutput(TerminalLineType.Normal, Text.Get(2761366063), 0.6f);
                    sender.m_command.AddOutput("");
                    sender.m_command.AddOutput(TerminalLineType.Normal, Text.Get(3435969025), 0.8f);
                    receiver.m_command.AddCommand(
                        TERM_Command.TerminalUplinkConfirm, 
                        "UPLINK_CONFIRM", 
                        new LocalizedText() { UntranslatedText = Text.Get(112719254), Id = 0 }, 
                        TERM_CommandRule.OnlyOnceDelete);
                    receiver.m_command.AddOutput(TerminalLineType.Normal, string.Format(Text.Get(1173595354), sender.PublicName));
                }
            }
            else
            {
                sender.m_command.AddUplinkWrongAddressError(param1);
            }

            return false;
        }

        // rewrite the method to do more things
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.TerminalCorruptedUplinkConfirm))]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_TerminalCorruptedUplinkConfirm(LG_ComputerTerminalCommandInterpreter __instance, string param1, string param2, ref bool __result)
        {
            // invoked on receiver side
            // sender, receiver references are 'flipped'
            var receiver = __instance.m_terminal;
            var sender = __instance.m_terminal.CorruptedUplinkReceiver;

            if (sender == null)
            {
                LegacyLogger.Error("TerminalCorruptedUplinkConfirm() critical failure because terminal does not have a CorruptedUplinkReceiver (sender).");
                __result = false;
                return false;
            }

            if (sender.m_isWardenObjective) return true; // vanilla uplink

            // TODO: config is unused here, prolly add more stuff
            var uplinkConfig = TerminalUplinkOverrideManager.Current.GetUplinkConfig(sender);
            
            receiver.m_command.AddOutput(TerminalLineType.Normal, string.Format(Text.Get(2816126705), sender.PublicName));
            // vanilla code in this part is totally brain-dead
            if (sender.ChainedPuzzleForWardenObjective != null)
            {
                sender.ChainedPuzzleForWardenObjective.OnPuzzleSolved += new System.Action(() => receiver.m_command.StartTerminalUplinkSequence(string.Empty, true));
                sender.m_command.AddOutput("");
                sender.m_command.AddOutput(Text.Get(3268596368));
                sender.m_command.AddOutput(Text.Get(2277987284));

                receiver.m_command.AddOutput("");
                receiver.m_command.AddOutput(Text.Get(3268596368));
                receiver.m_command.AddOutput(Text.Get(2277987284));

                if(SNet.IsMaster)
                {
                    sender.ChainedPuzzleForWardenObjective.AttemptInteract(eChainedPuzzleInteraction.Activate);
                }
            }
            else
            {
                receiver.m_command.StartTerminalUplinkSequence(string.Empty, true);
            }

            __result = true;
            return false;
        }

        // rewrite is indispensable
        // both uplink and corruplink call this method
        // uplink calls on uplink terminal
        // corruplink calls on receiver side
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.StartTerminalUplinkSequence))]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_StartTerminalUplinkSequence(LG_ComputerTerminalCommandInterpreter __instance, string uplinkIp, bool corrupted)
        {
            // normal uplink
            if (!corrupted)
            {
                var uplinkTerminal = __instance.m_terminal;
                if (uplinkTerminal.m_isWardenObjective) return true; // vanilla uplink
                var uplinkConfig = TerminalUplinkOverrideManager.Current.GetUplinkConfig(uplinkTerminal);

                uplinkTerminal.m_command.AddOutput(TerminalLineType.ProgressWait, string.Format(Text.Get(2583360288), uplinkIp), 3f);
                __instance.TerminalUplinkSequenceOutputs(uplinkTerminal, false);

                uplinkTerminal.m_command.OnEndOfQueue = new System.Action(() =>
                {
                    LegacyLogger.Debug("UPLINK CONNECTION DONE!");
                    uplinkTerminal.UplinkPuzzle.Connected = true;
                    uplinkTerminal.UplinkPuzzle.CurrentRound.ShowGui = true;
                    uplinkTerminal.UplinkPuzzle.OnStartSequence();
                    uplinkConfig.EventsOnCommence.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));

                    int i = uplinkConfig.RoundOverrides.FindIndex(o => o.RoundIndex == 0);
                    UplinkRound firstRoundOverride = i != -1 ? uplinkConfig.RoundOverrides[i] : null;
                    firstRoundOverride?.EventsOnRound.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.OnStart, false));
                });
            }

            // corruplink
            else
            {
                // corruplink
                var receiver = __instance.m_terminal;
                var sender = __instance.m_terminal.CorruptedUplinkReceiver;

                if (sender.m_isWardenObjective) return true; // vanilla uplink

                var uplinkConfig = TerminalUplinkOverrideManager.Current.GetUplinkConfig(sender);

                sender.m_command.AddOutput(TerminalLineType.ProgressWait, string.Format(Text.Get(2056072887), sender.PublicName), 3f);
                sender.m_command.AddOutput("");
                receiver.m_command.AddOutput(TerminalLineType.ProgressWait, string.Format(Text.Get(2056072887), sender.PublicName), 3f);
                receiver.m_command.AddOutput("");

                receiver.m_command.TerminalUplinkSequenceOutputs(sender, false);
                receiver.m_command.TerminalUplinkSequenceOutputs(receiver, true);

                receiver.m_command.OnEndOfQueue = new System.Action(() =>
                {
                    LegacyLogger.Debug("UPLINK CONNECTION DONE!");
                    sender.UplinkPuzzle.Connected = true;
                    sender.UplinkPuzzle.CurrentRound.ShowGui = true;
                    sender.UplinkPuzzle.OnStartSequence();
                    uplinkConfig.EventsOnCommence.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));

                    int i = uplinkConfig.RoundOverrides.FindIndex(o => o.RoundIndex == 0);
                    UplinkRound firstRoundOverride = i != -1 ? uplinkConfig.RoundOverrides[i] : null;
                    firstRoundOverride?.EventsOnRound.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.OnStart, false));
                });
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.TerminalUplinkSequenceOutputs))]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_TerminalUplinkSequenceOutputs(LG_ComputerTerminal terminal, bool corrupted)
        {
            if (terminal.m_isWardenObjective) return true; // vanilla uplink

            // `terminal` is either sender or receiver 
            var uplinkConfig = TerminalUplinkOverrideManager.Current.GetUplinkConfig(terminal);
            if(uplinkConfig == null)
            {
                if(terminal.CorruptedUplinkReceiver != null)
                {
                    uplinkConfig = TerminalUplinkOverrideManager.Current.GetUplinkConfig(terminal.CorruptedUplinkReceiver);
                    if (uplinkConfig == null || uplinkConfig.DisplayUplinkWarning) return true;
                }
                else
                {
                    return true;
                }
            }

            terminal.m_command.AddOutput(TerminalLineType.SpinningWaitNoDone, Text.Get(3418104670), 3f);
            terminal.m_command.AddOutput("");

            if (!corrupted)
            {
                terminal.m_command.AddOutput(string.Format(Text.Get(947485599), terminal.UplinkPuzzle.CurrentRound.CorrectPrefix));
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.TerminalUplinkVerify))]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_TerminalUplinkVerify(LG_ComputerTerminalCommandInterpreter __instance, string param1, string param2, ref bool __result)
        {
            if (__instance.m_terminal.m_isWardenObjective) return true; // vanilla uplink
            // corr log is sent in TerminalUplinkPuzzle.TryGoToNextRound
            
            var uplinkPuzzle = __instance.m_terminal.UplinkPuzzle;
            var uplinkConfig = TerminalUplinkOverrideManager.Current.GetUplinkConfig(__instance.m_terminal);
            
            int CurrentRoundIndex = uplinkPuzzle.m_roundIndex;
            int i = uplinkConfig.RoundOverrides.FindIndex(o => o.RoundIndex == CurrentRoundIndex);
            UplinkRound roundOverride = i != -1 ? uplinkConfig.RoundOverrides[i] : null;
            TimeSettings timeSettings = i != -1 ? roundOverride.OverrideTimeSettings : uplinkConfig.DefaultTimeSettings;

            float timeToStartVerify = timeSettings.TimeToStartVerify >= 0f ? timeSettings.TimeToStartVerify : uplinkConfig.DefaultTimeSettings.TimeToStartVerify;
            float timeToCompleteVerify = timeSettings.TimeToCompleteVerify >= 0f ? timeSettings.TimeToCompleteVerify : uplinkConfig.DefaultTimeSettings.TimeToCompleteVerify;
            float timeToRestoreFromFail = timeSettings.TimeToRestoreFromFail >= 0f ? timeSettings.TimeToRestoreFromFail : uplinkConfig.DefaultTimeSettings.TimeToRestoreFromFail;

            if (uplinkPuzzle.Connected)
            {
                // Attempting uplink verification
                __instance.AddOutput(TerminalLineType.SpinningWaitNoDone, Text.Get(2734004688), timeToStartVerify); 

                // correct verification 
                if (!uplinkPuzzle.Solved && uplinkPuzzle.CurrentRound.CorrectCode.ToUpper() == param1.ToUpper())
                {
                    // Verification code {0} correct
                    __instance.AddOutput(string.Format(Text.Get(1221800228), uplinkPuzzle.CurrentProgress));
                    if (uplinkPuzzle.TryGoToNextRound()) // Goto next round
                    {
                        int newRoundIndex = uplinkPuzzle.m_roundIndex;
                        int j = uplinkConfig.RoundOverrides.FindIndex(o => o.RoundIndex == newRoundIndex);
                        UplinkRound newRoundOverride = j != -1 ? uplinkConfig.RoundOverrides[j] : null;


                        roundOverride?.EventsOnRound.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.OnMid, false));

                        if (roundOverride != null && roundOverride.ChainedPuzzleToEndRoundInstance != null) 
                        {
                            TextDataBlock block = GameDataBlockBase<TextDataBlock>.GetBlock("InGame.UplinkTerminal.ScanRequiredToProgress");
                            if (block != null)
                            {
                                __instance.AddOutput(TerminalLineType.ProgressWait, Text.Get(block.persistentID));
                            }

                            roundOverride.ChainedPuzzleToEndRoundInstance.OnPuzzleSolved += new System.Action(() => {
                                __instance.AddOutput(TerminalLineType.ProgressWait, Text.Get(27959760), timeToCompleteVerify); // "Building uplink verification signature"
                                __instance.AddOutput("");
                                __instance.AddOutput(string.Format(Text.Get(4269617288), uplinkPuzzle.CurrentProgress, uplinkPuzzle.CurrentRound.CorrectPrefix));
                                __instance.OnEndOfQueue = new System.Action(() =>
                                {
                                    LegacyLogger.Log("UPLINK VERIFICATION GO TO NEXT ROUND!");
                                    uplinkPuzzle.CurrentRound.ShowGui = true;
                                    newRoundOverride?.EventsOnRound.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.OnStart, false));
                                });
                            });

                            if (SNet.IsMaster)
                            {
                                roundOverride.ChainedPuzzleToEndRoundInstance.AttemptInteract(eChainedPuzzleInteraction.Activate);
                            }
                        }
                        else
                        {
                            __instance.AddOutput(TerminalLineType.ProgressWait, Text.Get(27959760), timeToCompleteVerify); // "Building uplink verification signature"
                            __instance.AddOutput("");
                            __instance.AddOutput(string.Format(Text.Get(4269617288), uplinkPuzzle.CurrentProgress, uplinkPuzzle.CurrentRound.CorrectPrefix));

                            __instance.OnEndOfQueue = new System.Action(() =>
                            {
                                LegacyLogger.Log("UPLINK VERIFICATION GO TO NEXT ROUND!");
                                uplinkPuzzle.CurrentRound.ShowGui = true;
                                newRoundOverride?.EventsOnRound.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.OnStart, false));
                            });
                        }
                    }
                    else // uplink done
                    {
                        __instance.AddOutput(TerminalLineType.SpinningWaitNoDone, Text.Get(1780488547), 3f);
                        __instance.AddOutput("");
                        __instance.OnEndOfQueue = new System.Action(() =>
                        {
                            roundOverride?.EventsOnRound.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.OnMid, false));

                            if (roundOverride != null && roundOverride.ChainedPuzzleToEndRoundInstance != null)
                            {
                                roundOverride.ChainedPuzzleToEndRoundInstance.OnPuzzleSolved += new System.Action(() => {
                                    __instance.AddOutput(TerminalLineType.Normal, string.Format(Text.Get(3928683780), uplinkPuzzle.TerminalUplinkIP), 2f); // establish succeed
                                    __instance.AddOutput("");

                                    __instance.OnEndOfQueue = new System.Action(() =>
                                    {
                                        LegacyLogger.Error("UPLINK VERIFICATION SEQUENCE DONE!");
                                        LG_ComputerTerminalManager.OngoingUplinkConnectionTerminalId = 0U;
                                        uplinkPuzzle.Solved = true;

                                        // Tested, it's save to do this
                                        uplinkPuzzle.OnPuzzleSolved?.Invoke();
                                    });
                                });

                                if (SNet.IsMaster)
                                {
                                    roundOverride.ChainedPuzzleToEndRoundInstance.AttemptInteract(eChainedPuzzleInteraction.Activate);
                                }
                            }
                            else
                            {
                                __instance.AddOutput(TerminalLineType.Normal, string.Format(Text.Get(3928683780), uplinkPuzzle.TerminalUplinkIP), 2f); // establish succeed
                                __instance.AddOutput("");

                                LegacyLogger.Error("UPLINK VERIFICATION SEQUENCE DONE!");
                                LG_ComputerTerminalManager.OngoingUplinkConnectionTerminalId = 0U;
                                uplinkPuzzle.Solved = true;

                                // Tested, it's save to do this
                                uplinkPuzzle.OnPuzzleSolved?.Invoke(); // EventsOnComplete and other stuff
                            }
                        });
                    }
                }
                else if (uplinkPuzzle.Solved) // already solved
                {
                    __instance.AddOutput("");
                    __instance.AddOutput(TerminalLineType.Fail, Text.Get(4080876165)); // failed, already done
                    __instance.AddOutput(TerminalLineType.Normal, Text.Get(4104839742), 6f); // "Returning to root.."
                }
                else // incorrect verification
                {
                    __instance.AddOutput("");
                    __instance.AddOutput(TerminalLineType.Fail, string.Format(Text.Get(507647514), uplinkPuzzle.CurrentRound.CorrectPrefix)); //"Verfication failed! Use public key <color=orange>" + + "</color> to obtain the code");
                    __instance.AddOutput(TerminalLineType.Normal, Text.Get(4104839742), timeToRestoreFromFail);
                }
            }
            else // unconnected
            {
                __instance.AddOutput("");
                __instance.AddOutput(Text.Get(403360908));
            }

            __result = false;
            return false;
        }
    }
}
