using AIGraph;
using Enemies;
using System.Collections.Generic;
using UnityEngine;
using SNetwork;
using Player;
using AK;
using FX_EffectSystem;
using LEGACY.LegacyOverride.EnemyTagger;

namespace LEGACY.Components
{
    // unsynced state
    public enum eEnemyTaggerState
    {
        Uninitialized,
        Inactive,
        Active
    }

    public class EnemyTagger : MonoBehaviour
    {
        
        private const float INACTIVE_SOUND_UPDATE_INTERVAL = 5f;

        private int MaxTagPerScan = 12;
        private float UpdateInterval = 3.0f;
        private float TagRadius = 12f;
        

        private eEnemyTaggerState CurrentState = eEnemyTaggerState.Uninitialized;

        public string DebugName { set; get; } = "EnemyTagger";


        internal CarryItemPickup_Core Parent { set; get; } = null;
        internal PlayerAgent PickedByPlayer { set; get; } = null;

        private CellSoundPlayer m_sound = new();

        private List<EnemyAgent> TaggableEnemies = new();

        private float UpdateTime = 0.0f;

        internal Vector3 Position => Parent == null ? Vector3.zero : (PickedByPlayer == null ? Parent.transform.position : PickedByPlayer.transform.position);

        internal void ApplySetting(EnemyTaggerSetting setting)
        {
            MaxTagPerScan = setting.MaxTagPerScan;
            UpdateInterval = setting.UpdateInterval;
            TagRadius = setting.TagRadius;
        }

        public void ChangeState(eEnemyTaggerState newState)
        {
            CurrentState = newState;
            switch (CurrentState) 
            {
                case eEnemyTaggerState.Uninitialized:
                    Utils.Logger.Error("Enemy Tagger changed to state 'uninitialized'?");
                    return;
                case eEnemyTaggerState.Inactive:
                    UpdateTime = 0.0f;
                    TaggableEnemies.Clear();
                    m_sound.Post(EVENTS.BUTTONGENERICDEACTIVATE);
                    break;
                case eEnemyTaggerState.Active: 
                    UpdateTime = 0.0f;
                    m_sound.Post(EVENTS.BULKHEAD_BUTTON_CLOSE);
                    break;
                default:
                    Utils.Logger.Error($"Enemy Tagger: Undefined state {CurrentState}");
                    return;
            }
        }

        private bool UpdateTaggableEnemies()
        {
            TaggableEnemies.Clear();
            bool hasEnemyInProximity = false;
            var enemies = AIG_CourseGraph.GetReachableEnemiesInNodes(PickedByPlayer == null ? Parent.m_courseNode : PickedByPlayer.CourseNode, 2);
            foreach (var enemy in enemies)
            {
                if (!enemy.Alive) continue;

                float distance = (enemy.transform.position - this.Position).magnitude;
                
                if(distance > TagRadius) continue;
                hasEnemyInProximity = true;
                if(!enemy.IsTagged)
                    TaggableEnemies.Add(enemy);

                if (TaggableEnemies.Count >= MaxTagPerScan) break;
            }

            return hasEnemyInProximity;
        }

        private void StartTagging()
        {
            if (UpdateTime >= UpdateInterval)
            {
                if (SNet.IsMaster)
                {
                    UpdateTaggableEnemies();
                    foreach (var enemy in TaggableEnemies)
                    {
                        ToolSyncManager.WantToTagEnemy(enemy);
                    }
                }
                // candidate sound 
                /*
                 * DECON_SYSTEM 
                 * DECON_UNIT_DETACH
                 * DECON_UNIT_EMITTER */
                m_sound.Post(EVENTS.BULKHEAD_BUTTON_OPEN);
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
                case eEnemyTaggerState.Active:
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
