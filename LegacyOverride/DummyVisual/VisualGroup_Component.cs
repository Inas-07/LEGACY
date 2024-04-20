//using BepInEx.Unity.IL2CPP.Utils.Collections;
//using ExtraObjectiveSetup;
//using FloLib.Networks.Replications;
//using GTFO.API;
//using LEGACY.LegacyOverride.DummyVisual.VisualGOAnimation;
//using LEGACY.Utils;
//using SNetwork;
//using System.Collections.Generic;
//using System.Linq;
//using TMPro;
//using UnityEngine;

//namespace LEGACY.LegacyOverride.DummyVisual
//{
//    public class VisualGroup
//    {
//        public VisualGroupDefinition def { get; private set; }

//        private List<GameObject> Visuals { get; } = new();

//        private List<DirectionalAnimation> DirectionalAnimations { get; } = new();

//        private List<BlinkAnimation> BlinkAnimations { get; } = new();

//        public IEnumerable<GameObject> GameObjects => Visuals;

//        public StateReplicator<VisualGroupState> StateReplicator { get; set; }


//        private void OnStateChanged(VisualGroupState oldState, VisualGroupState newState, bool isRecall)
//        {
//            if(oldState.visualType != newState.visualType) 
//            {
//                LegacyLogger.Debug($"VisualGroup OnStateChange: {oldState.visualType} -> {newState.visualType}");
//            }

//            if (!isRecall) return;
//            ChangeToStateUnsynced(newState);
//        }

//        public void ChangeToState(VisualGroupState state)
//        {
//            ChangeToStateUnsynced(state);
//            if(SNet.IsMaster)
//            {
//                StateReplicator.SetState(state);
//            }
//        }

//        private void ChangeToStateUnsynced(VisualGroupState state)
//        {
//            switch (state.visualType)
//            {
//                case VisualType.OFF:
//                    BlinkAnimations.ForEach(ani => ani.SetPlayAnimation(false));
//                    DirectionalAnimations.ForEach(ani => ani.SetPlayAnimation(false));
//                    Visuals.ForEach(go => go.SetActive(false));
//                    break;

//                case VisualType.ON:
//                    Visuals.ForEach(go => go.SetActive(true));
//                    BlinkAnimations.ForEach(ani => ani.SetPlayAnimation(false));
//                    DirectionalAnimations.ForEach(ani => ani.SetPlayAnimation(false));
//                    break;

//                case VisualType.DIRECTIONAL:
//                case VisualType.REVERSE_DIRECTIONAL:
//                    Visuals.ForEach(go => go.SetActive(true));
//                    BlinkAnimations.ForEach(ani => ani.SetPlayAnimation(false));

//                    var anis = state.visualType == VisualType.DIRECTIONAL ? DirectionalAnimations :
//                        DirectionalAnimations.Reverse<DirectionalAnimation>();

//                    float curDelay = 0f;
//                    foreach (var ani in anis)
//                    {
//                        ani.InitialDelay = curDelay;
//                        ani.ShowHideTime = def.DirectionalConfig.ShowHideTime;
//                        ani.ShowCylinder = def.DisplayCylinder;
//                        curDelay += def.DirectionalConfig.DelayPerGO;
//                        ani.SetPlayAnimation(true);
//                    }

//                    break;

//                case VisualType.BLINK:
//                    Visuals.ForEach(go => go.SetActive(true));
//                    DirectionalAnimations.ForEach(ani => ani.SetPlayAnimation(false));

//                    foreach (var ani in BlinkAnimations)
//                    {
//                        ani.ShowHideTime = def.BlinkConfig.ShowHideTime;
//                        ani.ShowCylinder = def.DisplayCylinder;
//                        ani.SetPlayAnimation(true);
//                    }

//                    break;

//                default:
//                    LegacyLogger.Error($"VisualGroup: Visual Type '{state.visualType}' is undefined!");
//                    break;
//            }
//        }

//        private void BuildVisualGOs()
//        {
//            foreach (var vs in def.VisualSequences)
//            {
//                var startPos = vs.StartPosition.ToVector3();
//                var dir = vs.ExtendDirection.ToVector3();
//                if (startPos == Vector3.zero || dir == Vector3.zero || vs.Count < 1)
//                {
//                    LegacyLogger.Error($"Build DummyVisual: 'StartPosition' or 'ExtendDirection' is zero vector, or 'Count' is not a positive value, cannot build!");
//                    continue;
//                }

//                dir.Normalize();

//                GameObject visualGO = null;
//                switch (def.VisualType)
//                {
//                    case MaterialType.Sensor:
//                        visualGO = Assets.DummySensor; break;

//                    case MaterialType.Scan:
//                        visualGO = Assets.DummyScan; break;

//                    default:
//                        LegacyLogger.Error($"Build DummyVisual: VisualType {def.VisualType} is not implemented.");
//                        continue;
//                }

//                var curPos = startPos;
//                var rot = vs.Rotation.ToQuaternion();
//                for (int i = 0; i < vs.Count; i++)
//                {
//                    var go = Object.Instantiate(visualGO);
//                    go.transform.SetPositionAndRotation(curPos, rot);
//                    var cylinder = go.transform.GetChild(0).GetChild(0).gameObject;
//                    cylinder.SetActive(def.DisplayCylinder);

//                    go.SetActiveRecursively(true);
//                    var DisplayText = go.GetComponentInChildren<TextMeshPro>();
//                    if (DisplayText != null)
//                    {
//                        DisplayText.SetText(def.Text);
//                        DisplayText.ForceMeshUpdate();
//                    }

//                    float height = 0.6f / 3.7f;
//                    go.transform.localScale = new Vector3(vs.Radius, vs.Radius, vs.Radius);
//                    go.transform.localPosition += Vector3.up * height;

//                    go.transform.GetChild(0).GetChild(1)
//                        .gameObject.GetComponentInChildren<Renderer>()
//                        .material.SetColor("_ColorA", new Color(vs.Color.x, vs.Color.y, vs.Color.z));

//                    Visuals.Add(go);

//                    curPos += dir * vs.PlacementInterval;

//                    var dirAni = go.AddComponent<DirectionalAnimation>();
//                    dirAni.VisualGO = go;
//                    dirAni.SetPlayAnimation(false);
//                    DirectionalAnimations.Add(dirAni);

//                    var blkAni = go.AddComponent<BlinkAnimation>();
//                    blkAni.VisualGO = go;
//                    blkAni.SetPlayAnimation(false);
//                    BlinkAnimations.Add(blkAni);
//                }
//            }
//        }

//        private void DestroyVisualGOs()
//        {
//            Visuals.ForEach(Object.Destroy);
//            Visuals.Clear();
//        }

//        internal void ResetupOnDef(VisualGroupDefinition def)
//        {
//            this.def = def;
//            DestroyVisualGOs();
//            BuildVisualGOs();
//            ChangeToState(StateReplicator.State);
//        }

//        public bool Setup()
//        {
//            BuildVisualGOs();
//            uint id = EOSNetworking.AllotReplicatorID();
//            if (id == EOSNetworking.INVALID_ID)
//            {
//                LegacyLogger.Error("VisualGroup Setup: cannot setup replicator");
//                return false;
//            }

//            StateReplicator = StateReplicator<VisualGroupState>.Create(id, new() { visualType = VisualType.ON }, LifeTimeType.Level);
//            StateReplicator.OnStateChanged += OnStateChanged;

//            LevelAPI.OnEnterLevel += OnEnterLevel;
//            return true;
//        }

//        private void OnEnterLevel()
//        {
//            ChangeToState(new() { visualType = def.InitialVisual });
//        }

//        public void Destroy()
//        {
//            LevelAPI.OnEnterLevel -= OnEnterLevel;
//            DestroyVisualGOs();
//            StateReplicator = null;
//        }

//        public VisualGroup(VisualGroupDefinition def)
//        {
//            this.def = def;
//        }
//    }
//}
