using HarmonyLib;
using Enemies;
using SNetwork;
using LevelGeneration;
using GameData;
using LEGACY.Utils;

namespace LEGACY.ExtraEvents
{
    [HarmonyPatch]
    class Patch_EventsOnZoneScoutScream
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ES_ScoutScream), nameof(ES_ScoutScream.CommonUpdate))]
        private static bool Pre_ES_ScoutScream_CommonUpdate(ES_ScoutScream __instance)
        {
            if (__instance.m_state != ES_ScoutScream.ScoutScreamState.Response) return true;

            if (__instance.m_stateDoneTimer >= Clock.Time) return true;

            var EventsOnPortalWarp = __instance.m_enemyAgent.m_courseNode.m_zone.m_settings.m_zoneData.EventsOnPortalWarp;
            if (EventsOnPortalWarp != null && EventsOnPortalWarp.Count > 0)
            {
                LegacyLogger.Warning("EventsOnZoneScoutScream: executing events in EventsOnPortalWarp (would skip level scout wave)!");
                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(EventsOnPortalWarp, eWardenObjectiveEventTrigger.None, true);
            }

            else // use default scout wave settings.
            {
                if (SNet.IsMaster)
                {
                    if (__instance.m_enemyAgent.CourseNode != null)
                    {
                        if (RundownManager.ActiveExpedition.Expedition.ScoutWaveSettings > 0U && RundownManager.ActiveExpedition.Expedition.ScoutWavePopulation > 0U)
                            Mastermind.Current.TriggerSurvivalWave(__instance.m_enemyAgent.CourseNode, RundownManager.ActiveExpedition.Expedition.ScoutWaveSettings, RundownManager.ActiveExpedition.Expedition.ScoutWavePopulation, out ushort _);
                        else
                            UnityEngine.Debug.LogError("ES_ScoutScream, a scout is screaming but we can't spawn a wave because the the scout settings are not set for this expedition! ScoutWaveSettings: " + RundownManager.ActiveExpedition.Expedition.ScoutWaveSettings + " ScoutWavePopulation: " + RundownManager.ActiveExpedition.Expedition.ScoutWavePopulation);
                    }
                }
            }

            if (SNet.IsMaster)
            {
                __instance.m_enemyAgent.AI.m_behaviour.ChangeState(EB_States.InCombat);
            }

            __instance.m_machine.ChangeState((int)ES_StateEnum.PathMove);
            __instance.m_state = ES_ScoutScream.ScoutScreamState.Done;

            return false;
        }
    }
}
