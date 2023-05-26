using HarmonyLib;
using LevelGeneration;
using LEGACY.Utils;
using GameData;
using LEGACY.LegacyOverride.HSUActivators;
using SNetwork;
using Player;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    class Patch_LG_HSUActivator_Core
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_HSUActivator_Core), nameof(LG_HSUActivator_Core.SetupFromCustomGeomorph))]
        private static void Post_LG_HSUActivator_Core_SetupFromCustomGeomorph(LG_HSUActivator_Core __instance)
        {
            uint instanceIndex = HSUActivatorOverrideManager.Current.Register(__instance);

            HSUActivator config = HSUActivatorOverrideManager.Current.GetOverride(__instance.SpawnNode.m_dimension.DimensionIndex, __instance.SpawnNode.LayerType, __instance.SpawnNode.m_zone.LocalIndex, instanceIndex);

            if (config == null) return;
            if(__instance.m_isWardenObjective)
            {
                LegacyLogger.Error($"BuildCustomHSUActivator: the HSUActivator has been set up by vanilla! Aborting custom setup...");
                LegacyLogger.Error($"HSUActivator in {__instance.SpawnNode.m_zone.LocalIndex}, {__instance.SpawnNode.LayerType}, {__instance.SpawnNode.m_dimension.DimensionIndex}");
                return;
            }

            // LG_HSUActivator_Core.Setup
            __instance.m_linkedItemGoingIn = __instance.SpawnPickupItemOnAlign(config.ItemFromStart, __instance.m_itemGoingInAlign, false, -1);
            __instance.m_linkedItemComingOut = __instance.SpawnPickupItemOnAlign(config.ItemAfterActivation, __instance.m_itemComingOutAlign, false, -1);

            LG_LevelInteractionManager.DeregisterTerminalItem(__instance.m_linkedItemGoingIn.GetComponentInChildren<iTerminalItem>());
            LG_LevelInteractionManager.DeregisterTerminalItem(__instance.m_linkedItemComingOut.GetComponentInChildren<iTerminalItem>());
            __instance.m_linkedItemGoingIn.SetPickupInteractionEnabled(false);
            __instance.m_linkedItemComingOut.SetPickupInteractionEnabled(false);

            // reset, do nothing
            // do not interfere with warden objective
            __instance.m_insertHSUInteraction.OnInteractionSelected = new System.Action<PlayerAgent>((p) => { });

            __instance.m_sequencerInsertItem.OnSequenceDone = new System.Action(() => 
            {
                pHSUActivatorState state = __instance.m_stateReplicator.State;
                if (!state.isSequenceIncomplete)
                    LegacyLogger.Error(">>>>>> HSUInsertSequenceDone! Sequence was already complete");
                state.isSequenceIncomplete = false;
                __instance.m_stateReplicator.SetStateUnsynced(state);
                LegacyLogger.Error(">>>>>> HSUInsertSequenceDone!");
                if (__instance.m_triggerExtractSequenceRoutine != null)
                    __instance.StopCoroutine(__instance.m_triggerExtractSequenceRoutine);
                if (SNet.IsMaster)
                {
                    // activation scan is built OnBuildDone
                    var activationScan = HSUActivatorOverrideManager.Current.GetActivationScan(config.DimensionIndex, config.LayerType, config.LocalIndex, config.InstanceIndex);
                    if(activationScan == null)
                    {
                        __instance.m_triggerExtractSequenceRoutine = __instance.StartCoroutine(__instance.TriggerRemoveSequence());
                    }
                    else
                    {
                        activationScan.OnPuzzleSolved += new System.Action(() => {
                            __instance.StartCoroutine(__instance.TriggerRemoveSequence());
                            config.EventsOnActivationScanSolved.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));
                        });

                        if(SNet.IsMaster)
                        {
                            activationScan.AttemptInteract(ChainedPuzzles.eChainedPuzzleInteraction.Activate);
                        }
                    }
                }
            });

            __instance.m_sequencerExtractItem.OnSequenceDone = new System.Action(() => 
            {
                __instance.m_stateReplicator.SetStateUnsynced(__instance.m_stateReplicator.State with
                {
                    isSequenceIncomplete = true
                });
                if (SNet.IsMaster)
                {
                    __instance.AttemptInteract(new pHSUActivatorInteraction()
                    {
                        type = eHSUActivatorInteractionType.SetExtractDone
                    });
                }
            });

            LegacyLogger.Debug($"HSUActivator: {(config.DimensionIndex, config.LayerType, config.LocalIndex, config.InstanceIndex)}, custom setup complete");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LG_HSUActivator_Core), nameof(LG_HSUActivator_Core.SyncStatusChanged))]
        private static bool Pre_LG_HSUActivator_Core_SyncStatusChanged(LG_HSUActivator_Core __instance, ref pHSUActivatorState newState, bool isRecall)
        {
            if (__instance.m_isWardenObjective)
            {
                return true;
            }
            uint index = HSUActivatorOverrideManager.Current.GetIndex(__instance);
            if (index == uint.MaxValue)
            {
                LegacyLogger.Error("Pre_LG_HSUActivator_Core_SyncStatusChanged: HSUActivator unregistered!!");
                return true;
            }

            var _override = HSUActivatorOverrideManager.Current.GetOverride(__instance.SpawnNode.m_dimension.DimensionIndex, __instance.SpawnNode.LayerType, __instance.SpawnNode.m_zone.LocalIndex, index);
            if (_override == null) return true;

            if (__instance.m_triggerExtractSequenceRoutine != null)
                __instance.StopCoroutine(__instance.m_triggerExtractSequenceRoutine);
            LegacyLogger.Debug("LG_HSUActivator_Core.OnSyncStatusChanged " + newState.status);
            bool goingInVisibleForPostCulling = __instance.m_goingInVisibleForPostCulling;
            bool comingOutVisibleForPostCulling = __instance.m_comingOutVisibleForPostCulling;

            switch (newState.status)
            {
                case eHSUActivatorStatus.WaitingForInsert:
                    __instance.m_insertHSUInteraction.SetActive(true);
                    __instance.ResetItem(__instance.m_itemGoingInAlign, __instance.m_linkedItemGoingIn, false, false, true, ref goingInVisibleForPostCulling);
                    __instance.ResetItem(__instance.m_itemComingOutAlign, __instance.m_linkedItemComingOut, false, false, true, ref comingOutVisibleForPostCulling);
                    __instance.m_sequencerWaitingForItem.StartSequence();
                    __instance.m_sequencerInsertItem.StopSequence();
                    __instance.m_sequencerExtractItem.StopSequence();
                    __instance.m_sequencerExtractionDone.StopSequence();
                    break;
                case eHSUActivatorStatus.Inserting:
                    __instance.m_insertHSUInteraction.SetActive(false);
                    __instance.ResetItem(__instance.m_itemGoingInAlign, __instance.m_linkedItemGoingIn, true, false, true, ref goingInVisibleForPostCulling);
                    __instance.ResetItem(__instance.m_itemComingOutAlign, __instance.m_linkedItemComingOut, false, false, true, ref comingOutVisibleForPostCulling);
                    __instance.m_sequencerWaitingForItem.StopSequence();
                    __instance.m_sequencerInsertItem.StartSequence();
                    __instance.m_sequencerExtractItem.StopSequence();
                    __instance.m_sequencerExtractionDone.StopSequence();
                    _override?.EventsOnHSUActivation.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));

                    break;
                case eHSUActivatorStatus.Extracting:
                    __instance.m_insertHSUInteraction.SetActive(false);
                    __instance.ResetItem(__instance.m_itemGoingInAlign, __instance.m_linkedItemGoingIn, !__instance.m_showItemComingOut, false, true, ref goingInVisibleForPostCulling);
                    __instance.ResetItem(__instance.m_itemComingOutAlign, __instance.m_linkedItemComingOut, __instance.m_showItemComingOut, false, true, ref comingOutVisibleForPostCulling);
                    __instance.m_sequencerWaitingForItem.StopSequence();
                    __instance.m_sequencerInsertItem.StopSequence();
                    __instance.m_sequencerExtractItem.StartSequence();
                    __instance.m_sequencerExtractionDone.StopSequence();
                    break;
                case eHSUActivatorStatus.ExtractionDone:
                    __instance.m_insertHSUInteraction.SetActive(false);
                    __instance.ResetItem(__instance.m_itemGoingInAlign, __instance.m_linkedItemGoingIn, !__instance.m_showItemComingOut, false, true, ref goingInVisibleForPostCulling);
                    __instance.ResetItem(__instance.m_itemComingOutAlign, __instance.m_linkedItemComingOut, __instance.m_showItemComingOut, _override.TakeOutItemAfterActivation, false, ref comingOutVisibleForPostCulling);
                    __instance.m_sequencerWaitingForItem.StopSequence();
                    __instance.m_sequencerInsertItem.StopSequence();
                    __instance.m_sequencerExtractItem.StopSequence();
                    __instance.m_sequencerExtractionDone.StartSequence();
                    if (!newState.isSequenceIncomplete)
                        break;
                    __instance.HSUInsertSequenceDone();
                    break;
            }

            return false;
        }
    }
}
