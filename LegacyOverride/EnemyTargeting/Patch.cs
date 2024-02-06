//using Agents;
//using Enemies;
//using HarmonyLib;
//using LEGACY.Utils;
//using Player;


//namespace LEGACY.LegacyOverride.EnemyTargeting
//{
//    [HarmonyPatch]
//    internal class Patch
//    {
//        [HarmonyPostfix]
//        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Target), MethodType.Setter)]
//        private static void Post_EnemyTarget_Setter(EnemyAI __instance)
//        {
//            var enemy = __instance.m_enemyAgent;
//            var target = __instance.Target;

//            if (__instance.Mode != AgentMode.Agressive
//                || target == null
//                || target.m_agent.CourseNode.Pointer == enemy.CourseNode.Pointer
//                || target.m_agent.CourseNode.m_zone.Pointer == enemy.CourseNode.m_zone.Pointer
//                || PlayerManager.PlayerAgentsInLevel.Count < 1
//                || PlayerManager.PlayerAgentsInLevel[0].CourseNode.m_dimension.DimensionIndex != enemy.CourseNode.m_dimension.DimensionIndex) return;
            
//            LegacyLogger.Warning("Post_EnemyTarget_Setter");

//            var prioritizer = enemy.GetComponent<EnemyTargetingPrioritizer>();
//            if (prioritizer == null) return;

//            prioritizer.TryPrioritizeCloserTarget();
//        }
//    }
//}
