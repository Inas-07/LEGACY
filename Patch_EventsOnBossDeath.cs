using HarmonyLib;
using Enemies;
using LevelGeneration;
using GameData;
using AIGraph;
using Globals;
using System.Collections.Generic;
using LEGACY.Utilities;
namespace LEGACY.Patch
{
    [HarmonyPatch]
    internal class Patch_EventsOnBossDeath
    {
        private static Dictionary<ushort, Il2CppSystem.Collections.Generic.List<WardenObjectiveEventData>> bossesWithDeathEvents = null;
        private static HashSet<uint> bossPID = new HashSet<uint>() { 29, 36, 37 };

        private static bool LogOnClient = true;

        private static void checkInit()
        {
            if (bossesWithDeathEvents != null) return;

            bossesWithDeathEvents = new Dictionary<ushort, Il2CppSystem.Collections.Generic.List<WardenObjectiveEventData>>();

            Logger.Debug("Patch_EventsOnBossDeath initialized");
        }

        private static bool hasDeathEvent(EnemyAgent enemyBoss)
        {
            return bossesWithDeathEvents != null && bossesWithDeathEvents.ContainsKey(enemyBoss.GlobalID);
        }

        private static bool isBoss(uint persistantID)
        {
            return bossPID != null && bossPID.Contains(persistantID);
        }

        private static Il2CppSystem.Collections.Generic.List<WardenObjectiveEventData> getEventsOnBossDeathForZone(LG_Zone zone)
        {
            if (zone == null)
            {
                Logger.Error("WTF why are you passing in a null LG_Zone?");
                return null;
            }

            return zone.m_settings.m_zoneData.EventsOnBossDeath;
        }


        // called on both host and client side
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemySync), nameof(EnemySync.OnSpawn))]
        private static void Post_SpawnEnemy(EnemySync __instance, pEnemySpawnData spawnData) // 原生怪的mode == hibernate
        {
            AIG_CourseNode node = null;

            if(spawnData.courseNode.TryGet(out node) == false || node == null)
            {
                Logger.Error("Failed to get spawnnode for a boss! Skipped EventsOnBossDeath for it");
            }

            if (!isBoss(__instance.m_agent.EnemyData.persistentID)) return;

            EnemyAgent boss = __instance.m_agent;

            //may take into good use: spawnData.mode
            //if (__instance.m_enemyStateData.agentMode != Agents.AgentMode.Hibernate && __instance.m_enemyStateData.agentMode != Agents.AgentMode.Off) return;
            //Logger.Log("spawnData.mode == {0}", spawnData.mode);
            if (spawnData.mode != Agents.AgentMode.Hibernate) return;

            LG_Zone spawnedZone = node.m_zone;

            Il2CppSystem.Collections.Generic.List<WardenObjectiveEventData> eventsOnBossDeath = getEventsOnBossDeathForZone(spawnedZone);

            if (eventsOnBossDeath == null || eventsOnBossDeath.Count == 0) return; // No events on boss death. 

            checkInit();

            bossesWithDeathEvents.Add(boss.GlobalID, eventsOnBossDeath);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyAgent), nameof(EnemyAgent.OnDead))]
        private static void Post_EnemyOnDeadExecuteDeathEvent(EnemyAgent __instance)
        {
            if (!hasDeathEvent(__instance)) return;

            if (GameStateManager.IsInExpedition == false) return;

            EnemyAgent boss = __instance;

            Il2CppSystem.Collections.Generic.List<WardenObjectiveEventData> eventsOnBossDeath = null;

            if (bossesWithDeathEvents.TryGetValue(boss.GlobalID, out eventsOnBossDeath) == false || eventsOnBossDeath == null) return;

            WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(eventsOnBossDeath, eWardenObjectiveEventTrigger.None, true, 0.0f);
            //Utils.CheckAndExecuteEventsOnTrigger(eventsOnBossDeath, eWardenObjectiveEventTrigger.None, true);
            bossesWithDeathEvents.Remove(boss.GlobalID);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GS_AfterLevel), nameof(GS_AfterLevel.CleanupAfterExpedition))]
        private static void Post_CleanupAfterExpedition()
        {
            if (bossesWithDeathEvents != null)
            {
                bossesWithDeathEvents.Clear();
                bossesWithDeathEvents = null;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Global),  nameof(Global.OnApplicationQuit))]
        private static void Post_OnApplicationQuit()
        {
            bossPID = null;
            if (bossesWithDeathEvents != null)
                bossesWithDeathEvents.Clear();
            bossesWithDeathEvents = null;
            Logger.Log("Clean Patch_EventsOnBossDeath for the game");
        }
    }
}
