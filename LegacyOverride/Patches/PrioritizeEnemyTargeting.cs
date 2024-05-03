using Enemies;
using HarmonyLib;
using LEGACY.LegacyOverride.EnemyTargeting;
using LEGACY.Utils;
using Player;
using SNetwork;
using System;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal class PrioritizeEnemyTargeting
    {
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(EnemyAgent), nameof(EnemyAgent.Setup))]
        //private static void Post_Dam_SyncedDamageBase_Setup(EnemyAgent __instance)
        //{
        //    var prioritizer = __instance.gameObject.AddComponent<EnemyTargetingPrioritizer>();
        //    prioritizer.enemy = __instance;
        //}

        private static bool s_patch = true;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnemyCourseNavigation), nameof(EnemyCourseNavigation.UpdateTracking))]
        private static void UpdateTracking(EnemyCourseNavigation __instance)
        {
            if (!s_patch)
            {
                return;
            }

            int playerCnt = PlayerManager.PlayerAgentsInLevel.Count;
            if (!SNet.IsMaster || playerCnt <= 1)
            {
                return;
            }

            var enemy = __instance.m_owner;
            if (enemy.Locomotion.m_currentState.m_stateEnum == ES_StateEnum.Hibernate) return;

            var originalTarget = __instance.m_targetRef.m_agent;

            if (originalTarget.CourseNode.Pointer == enemy.CourseNode.Pointer) return;

            PlayerAgent newTarget = null;

            for (int i = UnityEngine.Random.RandomRangeInt(0, playerCnt),
                cnt = 0;
                cnt < playerCnt;
                i = (i + 1) % playerCnt, cnt++)
            {
                var player = PlayerManager.PlayerAgentsInLevel[i];
                if (player.Alive && player.CourseNode.Pointer == enemy.CourseNode.Pointer)
                {
                    newTarget = player;
                    break;
                }
            }

            if (newTarget != null)
            {
                s_patch = false;
                enemy.AI.SetTarget(newTarget); // SetTarget calls EnemyCourseNavigation.UpdateTracking,
                s_patch = true;
            }
        }
    }
}
