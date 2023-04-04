using AIGraph;
using Enemies;
using System.Collections.Generic;
using UnityEngine;
using SNetwork;
using Player;
using AK;
using FX_EffectSystem;

namespace LEGACY.LegacyOverride.EnemyTagger
{
    // unsynced state
    public enum eEnemyTaggerState
    {
        Uninitialized,
        Inactive,
        Active_Warmup,
        Active_Tagging
    }

    public class EnemyTaggerComponent : MonoBehaviour
    {
        private const float INACTIVE_SOUND_UPDATE_INTERVAL = 5f;

        internal int MaxTagPerScan = 12;
        internal float TagInterval = 3.0f;
        internal float TagRadius = 12f;
        internal float WarmupTime = 3f;
        private eEnemyTaggerState CurrentState = eEnemyTaggerState.Uninitialized;

        public string DebugName { set; get; } = "EnemyTagger";

        internal CarryItemPickup_Core Parent { set; get; } = null;
        internal PlayerAgent PickedByPlayer { set; get; } = null;

        private CellSoundPlayer m_sound = new();

        private List<EnemyAgent> TaggableEnemies = new();

        private float UpdateTime = 0.0f;

        internal Vector3 Position => Parent == null ? Vector3.zero : PickedByPlayer == null ? Parent.transform.position : PickedByPlayer.transform.position;

        public void ChangeState(eEnemyTaggerState newState)
        {
            if(CurrentState == newState) return;

            switch (newState)
            {
                case eEnemyTaggerState.Uninitialized:
                    Utils.Logger.Error("Enemy Tagger changed to state 'uninitialized'?");
                    return;
                case eEnemyTaggerState.Inactive:
                    UpdateTime = 0.0f;
                    TaggableEnemies.Clear();
                    m_sound.Post(EVENTS.BULKHEAD_BUTTON_CLOSE);
                    break;
                case eEnemyTaggerState.Active_Warmup:
                    UpdateTime = 0.0f;
                    m_sound.Post(EVENTS.BUTTONGENERICDEACTIVATE);
                    break;
                case eEnemyTaggerState.Active_Tagging:
                    if (CurrentState == eEnemyTaggerState.Active_Warmup) break;
                    UpdateTime = 0.0f;
                    break;
                default:
                    Utils.Logger.Error($"Enemy Tagger: Undefined state {CurrentState}");
                    return;
            }
            CurrentState = newState;
        }

        private bool UpdateTaggableEnemies()
        {
            TaggableEnemies.Clear();
            bool hasEnemyInProximity = false;
            var enemies = AIG_CourseGraph.GetReachableEnemiesInNodes(PickedByPlayer == null ? Parent.m_courseNode : PickedByPlayer.CourseNode, 2);
            foreach (var enemy in enemies)
            {
                if (!enemy.Alive) continue;

                float distance = (enemy.transform.position - Position).magnitude;

                if (distance > TagRadius) continue;
                hasEnemyInProximity = true;
                if (!enemy.IsTagged)
                    TaggableEnemies.Add(enemy);

                if (TaggableEnemies.Count >= MaxTagPerScan) break;
            }

            return hasEnemyInProximity;
        }

        private void StartTagging()
        {
            if (UpdateTime >= TagInterval)
            {
                if (SNet.IsMaster)
                {
                    UpdateTaggableEnemies();
                    foreach (var enemy in TaggableEnemies)
                    {
                        ToolSyncManager.WantToTagEnemy(enemy);
                    }
                }

                m_sound.Post(EVENTS.MARKERGUNACTIVATE);
                UpdateTime = 0f;
            }
        }

        private void StopTagging()
        {
            if (UpdateTime >= INACTIVE_SOUND_UPDATE_INTERVAL)
            {
                m_sound.Post(EVENTS.BUTTONGENERICDEACTIVATE);
                UpdateTime = 0f;
            }
        }

        private void Warmup()
        {
            if (UpdateTime >= WarmupTime)
            {
                ChangeState(eEnemyTaggerState.Active_Tagging);
                UpdateTime = TagInterval;
            }
        }

        void Update()
        {
            if (CurrentState == eEnemyTaggerState.Uninitialized) return;

            if (Parent == null)
            {
                Utils.Logger.Error("EnemyTagger: null parent");
                return;
            }

            UpdateTime += Time.deltaTime;
            m_sound.UpdatePosition(Position);
            switch (CurrentState)
            {
                case eEnemyTaggerState.Active_Warmup:
                    Warmup(); 
                    break;

                case eEnemyTaggerState.Active_Tagging:
                    StartTagging();
                    break;

                case eEnemyTaggerState.Inactive:
                    StopTagging();
                    break;
            }
        }

        private void OnDestroy()
        {
            StopTagging();
            TaggableEnemies.Clear();
            TaggableEnemies = null;
            if (m_sound != null)
            {
                m_sound.Stop();
                m_sound.Recycle();
                m_sound = null;
            }

            Parent = null;
        }
    }
}
