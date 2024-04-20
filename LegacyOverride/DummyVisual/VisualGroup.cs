using BepInEx.Unity.IL2CPP.Utils.Collections;
using ExtraObjectiveSetup;
using FloLib.Networks.Replications;
using LEGACY.Utils;
using SNetwork;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using IEnumerator = System.Collections.IEnumerator;

namespace LEGACY.LegacyOverride.DummyVisual
{
    public class VisualGroup
    {
        public VisualGroupDefinition def { get; private set; }

        private List<GameObject> Visuals { get; } = new();

        public IEnumerable<GameObject> GameObjects => Visuals;

        public StateReplicator<VisualGroupState> StateReplicator { get; set; }

        private List<Coroutine> coroutines = new();

        private void OnStateChanged(VisualGroupState oldState, VisualGroupState newState, bool isRecall)
        {
            if(oldState.visualType != newState.visualType) 
            {
                LegacyLogger.Debug($"VisualGroup OnStateChange: {oldState.visualType} -> {newState.visualType}");
            }

            if (!isRecall) return;
            ChangeToStateUnsynced(newState);
        }

        public void ChangeToState(VisualGroupState state)
        {
            ChangeToStateUnsynced(state);
            if(SNet.IsMaster)
            {
                StateReplicator.SetState(state);
            }
        }

        private void ChangeToStateUnsynced(VisualGroupState state) 
        {
            coroutines.ForEach(CoroutineManager.StopCoroutine);
            coroutines.Clear();
            Coroutine coroutine = null;
            switch (state.visualType)
            {
                case VisualType.OFF:
                    Visuals.ForEach(go => go.SetActive(false));
                    break;

                case VisualType.ON:
                    Visuals.ForEach(go => go.SetActive(true));
                    break;

                case VisualType.DIRECTIONAL:
                case VisualType.REVERSE_DIRECTIONAL:
                    var gos = state.visualType == VisualType.DIRECTIONAL ? Visuals : new List<GameObject>(Visuals);

                    float curDelay = 0f;
                    foreach (var go in gos)
                    {
                        coroutine = CoroutineManager.StartCoroutine(Directional(go, def.DirectionalConfig.ShowHideTime, curDelay).WrapToIl2Cpp());
                        curDelay += def.DirectionalConfig.DelayPerGO;
                        coroutines.Add(coroutine);
                    }

                    break;
                case VisualType.BLINK:
                    coroutine = CoroutineManager.StartCoroutine(Blink(Visuals, def.BlinkConfig.ShowHideTime).WrapToIl2Cpp());
                    coroutines.Add(coroutine);
                    break;

                default:
                    LegacyLogger.Error($"VisualGroup: Visual Type '{state.visualType}' is undefined!");
                    break;
            }
        }

        private void BuildVisualGOs()
        {
            foreach (var vs in def.VisualSequences)
            {
                var startPos = vs.StartPosition.ToVector3();
                var dir = vs.ExtendDirection.ToVector3();
                if (startPos == Vector3.zero || dir == Vector3.zero || vs.Count < 1)
                {
                    LegacyLogger.Error($"Build DummyVisual: 'StartPosition' or 'ExtendDirection' is zero vector, or 'Count' is not a positive value, cannot build!");
                    continue;
                }

                dir.Normalize();

                GameObject visualGO = null;
                switch (def.VisualType)
                {
                    case MaterialType.Sensor:
                        visualGO = Assets.DummySensor; break;

                    case MaterialType.Scan:
                        visualGO = Assets.DummyScan; break;

                    default:
                        LegacyLogger.Error($"Build DummyVisual: VisualType {def.VisualType} is not implemented.");
                        continue;
                }

                var curPos = startPos;
                var rot = vs.Rotation.ToQuaternion();
                for (int i = 0; i < vs.Count; i++)
                {
                    var go = Object.Instantiate(visualGO);
                    go.transform.SetPositionAndRotation(curPos, rot);
                    var cylinder = go.transform.GetChild(0).GetChild(0).gameObject;
                    cylinder.SetActive(def.DisplayCylinder);

                    go.SetActiveRecursively(true);
                    var DisplayText = go.GetComponentInChildren<TextMeshPro>();
                    if (DisplayText != null)
                    {
                        DisplayText.SetText(def.Text);
                        DisplayText.ForceMeshUpdate();
                    }

                    float height = 0.6f / 3.7f;
                    go.transform.localScale = new Vector3(vs.Radius, vs.Radius, vs.Radius);
                    go.transform.localPosition += Vector3.up * height;

                    go.transform.GetChild(0).GetChild(1)
                        .gameObject.GetComponentInChildren<Renderer>()
                        .material.SetColor("_ColorA", new Color(vs.Color.x, vs.Color.y, vs.Color.z));

                    Visuals.Add(go);

                    curPos += dir * vs.PlacementInterval;
                }
            }
        }

        private void DestroyVisualGOs()
        {
            coroutines.ForEach(CoroutineManager.StopCoroutine);
            coroutines.Clear();
            Visuals.ForEach(Object.Destroy);
            Visuals.Clear();
        }

        internal void ResetupOnDef(VisualGroupDefinition def)
        {
            this.def = def;
            DestroyVisualGOs();
            BuildVisualGOs();
            ChangeToStateUnsynced(new() { visualType = def.InitialVisual });
        }

        public bool Setup()
        {
            BuildVisualGOs();
            uint id = EOSNetworking.AllotReplicatorID();
            if (id == EOSNetworking.INVALID_ID)
            {
                LegacyLogger.Error("VisualGroup Setup: cannot setup replicator");
                return false;
            }

            var initialState = new VisualGroupState() { visualType = VisualType.ON };
            StateReplicator = StateReplicator<VisualGroupState>.Create(id, initialState, LifeTimeType.Level);
            StateReplicator.OnStateChanged += OnStateChanged;
            ChangeToStateUnsynced(initialState);
            return true;
        }

        private IEnumerator Directional(GameObject go, float showHideTime, float startDelay)
        {
            yield return new WaitForSeconds(startDelay);
            while(true)
            {
                bool active = go.active;
                go.SetActive(!active);
                yield return new WaitForSeconds(showHideTime); 
            }
        }

        private IEnumerator Blink(List<GameObject> gos, float showHideTime)
        {
            while (true)
            {
                foreach(var go in gos)
                {
                    bool active = go.active;
                    go.SetActive(!active);
                }
                yield return new WaitForSeconds(showHideTime);
            }
        }

        public void Destroy()
        {
            DestroyVisualGOs();
            StateReplicator = null;
        }

        public VisualGroup(VisualGroupDefinition def)
        {
            this.def = def;
        }
    }
}
