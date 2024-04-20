using Agents;
using Enemies;
using Il2CppInterop.Runtime.Injection;
using LEGACY.Utils;
using Player;
using SNetwork;
using UnityEngine;

namespace LEGACY.LegacyOverride.EnemyTargeting
{
    internal class EnemyTargetingPrioritizer: MonoBehaviour
    {
        public const float UPDATE_INTERVAL = 1.5f;

        private float nextUpdateTime = float.NaN;

        internal EnemyAgent enemy = null;

        private Dam_EnemyDamageBase damage => enemy?.Damage;

        private AgentTarget target => enemy?.AI.Target;

        void Update()
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel || !SNet.IsMaster) { return; }

            if (float.IsNaN(nextUpdateTime))
            {
                nextUpdateTime = Clock.Time + UPDATE_INTERVAL;
                return;
            }
            else if (Clock.Time < nextUpdateTime)
            {
                return;
            }

            if (enemy == null)
            {
                return;
            }

            nextUpdateTime = Clock.Time + UPDATE_INTERVAL;
            if (enemy.AI.Mode != AgentMode.Agressive 
                || target == null // wait for vanilla to assign a target
                || target.m_agent.CourseNode.Pointer == enemy.CourseNode.Pointer
                || target.m_agent.CourseNode.m_zone.Pointer == enemy.CourseNode.m_zone.Pointer
                || PlayerManager.PlayerAgentsInLevel.Count < 1 
                || PlayerManager.PlayerAgentsInLevel[0].CourseNode.m_dimension.DimensionIndex != enemy.CourseNode.m_dimension.DimensionIndex) return;

            TryPrioritizeCloserTarget();
        }

        internal void TryPrioritizeCloserTarget()
        {
            // find a target in the same node
            PlayerAgent target_player = null;
            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                if (!player.Alive) continue;

                if (player.CourseNode.Pointer == enemy.CourseNode.Pointer)
                {
                    target_player = player;
                    break;
                }
            }

            // evaluate if target and enemy are in the same zone
            if (target_player == null)
            {
                foreach (var player in PlayerManager.PlayerAgentsInLevel)
                {
                    if (!player.Alive) continue;

                    if (player.CourseNode.m_zone.Pointer == enemy.CourseNode.m_zone.Pointer)
                    {
                        target_player = player;
                        break;
                    }
                }
            }

            if (target_player != null)
            {
                damage.BulletDamage(
                    0.0f,
                    target_player.Cast<Agent>(), target_player.Position,
                    target_player.Position - enemy.Position,
                    Vector3.up,
                    staggerMulti: 0f, precisionMulti: 0f);
                //LegacyLogger.Debug($"EnemyTargetingPrioritizer: enemy in {(enemy.CourseNode.m_zone.LocalIndex, enemy.CourseNode.LayerType, enemy.CourseNode.m_dimension.DimensionIndex)}, original target in {(otn.m_zone.LocalIndex, otn.LayerType, otn.m_dimension.DimensionIndex)}; Retargeting closer player...");
            }
        }

        void OnDestroy()
        {
            enemy = null;
        }

        static EnemyTargetingPrioritizer()
        {
            ClassInjector.RegisterTypeInIl2Cpp<EnemyTargetingPrioritizer>();
        }
    }
}
