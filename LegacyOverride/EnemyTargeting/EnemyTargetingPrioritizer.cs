using Agents;
using Enemies;
using Player;
using SNetwork;
using UnityEngine;
using Il2CppInterop.Runtime.Injection;
using LEGACY.Utils;

namespace LEGACY.LegacyOverride.EnemyTargeting
{
    internal class EnemyTargetingPrioritizer: MonoBehaviour
    {
        public const float UPDATE_INTERVAL = 3f;

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
                //|| target.m_agent.CourseNode.m_zone.Pointer == enemy.CourseNode.m_zone.Pointer
                || PlayerManager.PlayerAgentsInLevel.Count <= 1 
                || PlayerManager.PlayerAgentsInLevel[0].CourseNode.m_dimension.DimensionIndex != enemy.CourseNode.m_dimension.DimensionIndex) return;

            //LegacyLogger.Warning($"Current Target: {target.name}");

            TryPrioritizeCloserTarget();
        }

        internal void TryPrioritizeCloserTarget()
        {
            var originalTarget = target;
            Agent newTarget = null;

            // find a target in the same node
            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                if (!player.Alive) continue;

                if (player.CourseNode.Pointer == enemy.CourseNode.Pointer)
                {
                    newTarget = player;
                    break;
                }
            }

            // evaluate if target and enemy are in the same zone
            if (newTarget == null)
            {
                foreach (var player in PlayerManager.PlayerAgentsInLevel)
                {
                    if (!player.Alive) continue;

                    if (player.CourseNode.m_zone.Pointer == enemy.CourseNode.m_zone.Pointer)
                    {
                        newTarget = player;
                        break;
                    }
                }
            }


            if (newTarget != null)
            {
                //LegacyLogger.Error($"new Target: {newTarget.name}");

                //damage.BulletDamage(
                //    0.0f,
                //    target_player, target_player.Position,
                //    target_player.Position - enemy.Position,
                //    Vector3.up,
                //    staggerMulti: 0f, precisionMulti: 0f);
                //damage.MeleeDamage(
                //    0.0f,
                //    target_player, target_player.Position,
                //    target_player.Position - enemy.Position,
                //    staggerMulti: 0f, precisionMulti: 0f, backstabberMulti:0f, 
                //    environmentMulti:0f, sleeperMulti:0f, 
                //    damageNoiseLevel: DamageNoiseLevel.Low, 
                //    skipLimbDestruction:true);

                enemy.AI.SetTarget(newTarget);

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
