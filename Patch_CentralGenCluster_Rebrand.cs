using System.Collections.Generic; // dont use System.Generic.Collections
using HarmonyLib;
using LevelGeneration;
using GameData;
using LEGACY.Utilities;

namespace LEGACY.Patch
{
    [HarmonyPatch]
    internal class Patch_CentralGenCluster_Rebrand
    {
        private static Dictionary<LG_LayerType, int> EventSteps = null;

        private static bool DEBUG = true;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_PowerGeneratorCluster), nameof(LG_PowerGeneratorCluster.OnBuildDone))]
        private static void Post_OnBuildDone(LG_PowerGeneratorCluster __instance)
        {
            LG_PowerGeneratorCluster GC = __instance;
            LG_LayerType layerType = GC.SpawnNode.LayerType;
            WardenObjectiveDataBlock objective = WardenObjectiveManager.ActiveWardenObjective(layerType);
            if(objective.OnActivateOnSolveItem == true)
            {
                if(DEBUG)
                {
                    Logger.Debug("OnActivateOnSolveItem == true, using vanilla implmentation instead");
                }
                return;
            }

            Il2CppSystem.Collections.Generic.List<WardenObjectiveEventData> EventsOnActivate = objective.EventsOnActivate;
            if(EventsOnActivate.Count < 1)
            {
                if(DEBUG)
                {
                    Logger.Debug("OnActivateOnSolveItem == false but no events in EventsOnActivate. Skipped");
                }
                return;
            }

            if (EventSteps == null)
            {
                EventSteps = new Dictionary<LG_LayerType, int>();
            }

            if (EventSteps.ContainsKey(layerType))
            {
                Logger.Error("Unimplemented behaviour: EventsOnActivate for Multiple Generator Clusters in a layer.");
                return;
            }

            EventSteps.Add(layerType, 0);

            foreach(var generator in GC.m_generators)
            {
                generator.OnSyncStatusChanged += new System.Action<ePowerGeneratorStatus>((status) => {
                    if (status != ePowerGeneratorStatus.Powered) return;

                    int EventStep = EventsOnActivate.Count;
                    if(EventSteps.TryGetValue(layerType, out EventStep) == false)
                    {
                        Logger.Error("Critical: Unregistered GeneratorCluster.");
                        return;
                    }

                    int NextEventStep = WardenObjectiveManager.CheckAndExecuteEventsWithBreaks(layerType, EventsOnActivate, eWardenObjectiveEventTrigger.None, EventStep, true);

                    EventSteps.Remove(layerType);
                    EventSteps.Add(layerType, NextEventStep);
                });

                if (DEBUG)
                {
                    Logger.Debug("Added OnSyncStatusChanged on Generator_{0} for GC in {1}", generator.m_serialNumber, layerType);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GS_AfterLevel), nameof(GS_AfterLevel.CleanupAfterExpedition))]
        private static void Post_CleanupAfterExpedition()
        {
            EventSteps = null;
        }
    }
}
