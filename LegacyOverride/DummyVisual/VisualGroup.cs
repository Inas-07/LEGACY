using BepInEx.Unity.IL2CPP.Utils.Collections;
using ExtraObjectiveSetup;
using FloLib.Networks.Replications;
using GTFO.API;
using LEGACY.LegacyOverride.DummyVisual.VisualGOAnimation;
using LEGACY.LegacyOverride.DummyVisual.VisualGOAnimation.AnimationConfig;
using LEGACY.Utils;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public StateReplicator<VisualGroupState> StateReplicator { get; private set; }

        private List<Coroutine> coroutines = new();

        private void OnStateChanged(VisualGroupState oldState, VisualGroupState newState, bool isRecall)
        {
            if (oldState.VisualAnimationType != newState.VisualAnimationType)
            {
                LegacyLogger.Debug($"VisualGroup OnStateChange: {oldState.VisualAnimationType} -> {newState.VisualAnimationType}");
            }

            if (!isRecall) return;
            ChangeToStateUnsynced(newState);
        }

        public void ChangeToState(VisualGroupState state)
        {
            ChangeToStateUnsynced(state);
            if (SNet.IsMaster)
            {
                StateReplicator.SetState(state);
            }
        }
        private void ChangeToStateUnsynced(VisualGroupState state)
        {
            coroutines.ForEach(CoroutineManager.StopCoroutine);
            coroutines.Clear();
            Coroutine coroutine = null;
            switch (state.VisualAnimationType)
            {
                case VisualAnimationType.OFF:
                    Visuals.ForEach(go => go.SetActive(false));
                    break;

                case VisualAnimationType.ON:
                    Visuals.ForEach(go => go.SetActive(true));
                    break;

                case VisualAnimationType.DIRECTIONAL:
                case VisualAnimationType.REVERSE_DIRECTIONAL:
                    var gos = state.VisualAnimationType == VisualAnimationType.DIRECTIONAL ? Visuals : Visuals.Reverse<GameObject>().ToList();
                    coroutine = CoroutineManager.StartCoroutine(Directional(gos, def.AnimationConfig.Directional, def.InitialPlayDelay).WrapToIl2Cpp());
                    coroutines.Add(coroutine);

                    break;
                case VisualAnimationType.BLINK:
                    coroutine = CoroutineManager.StartCoroutine(Blink(Visuals, def.AnimationConfig.Blink, def.InitialPlayDelay).WrapToIl2Cpp());
                    coroutines.Add(coroutine);
                    break;

                default:
                    LegacyLogger.Error($"VisualGroup: Visual Type '{state.VisualAnimationType}' is undefined!");
                    break;
            }
        }

        private void BuildVisualGOs()
        {
            foreach (var vs in def.VisualSequences)
            {
                GameObject templateGO = null;
                switch (def.VisualType)
                {
                    case VisualTemplateType.SENSOR:
                        templateGO = Assets.DummySensor; break;

                    case VisualTemplateType.SCAN:
                        templateGO = Assets.DummyScan; break;

                    default:
                        LegacyLogger.Error($"Build DummyVisual: VisualType {def.VisualType} is not implemented.");
                        continue;
                }

                List<GameObject> sequenceGOs;
                switch(vs.SequenceType)
                {
                    case VSequenceType.DIRECTIONAL:
                        sequenceGOs = vs.DirectionalSequence.Generate(templateGO);
                        break;
                    case VSequenceType.CIRCULAR:
                        sequenceGOs = vs.CircularSequence.Generate(templateGO);
                        break;
                    default:
                        LegacyLogger.Error($"BuildVisualGOs: VSequenceType {vs.SequenceType} is not implemented");
                        continue;
                }

                foreach(var go in sequenceGOs)
                {
                    var cylinder = go.transform.GetChild(0).GetChild(0).gameObject;
                    cylinder.SetActive(def.DisplayCylinder);

                    var DisplayText = go.GetComponentInChildren<TextMeshPro>();
                    if (DisplayText != null)
                    {
                        DisplayText.SetText(def.Text);
                        DisplayText.ForceMeshUpdate();
                    }

                    float height = 0.6f / 3.7f;
                    go.transform.localScale = new Vector3(vs.VisualRadius, vs.VisualRadius, vs.VisualRadius);
                    go.transform.localPosition += Vector3.up * height;

                    go.transform.GetChild(0).GetChild(1)
                        .gameObject.GetComponentInChildren<Renderer>()
                        .material.SetColor("_ColorA", new Color(vs.VisualColor.x, vs.VisualColor.y, vs.VisualColor.z));
                }

                Visuals.AddRange(sequenceGOs);
            }
        }

        private void DestroyVisualGOs()
        {
            Visuals.ForEach(GameObject.Destroy);
            Visuals.Clear();
        }

        internal void ResetupOnDef(VisualGroupDefinition def)
        {
            this.def = def;
            DestroyVisualGOs();
            BuildVisualGOs();
            ChangeToState(new() { VisualAnimationType = def.InitialAnimation });
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

            StateReplicator = StateReplicator<VisualGroupState>.Create(id, new() { VisualAnimationType = VisualAnimationType.OFF }, LifeTimeType.Level);
            StateReplicator.OnStateChanged += OnStateChanged;

            LevelAPI.OnEnterLevel += OnEnterLevel;
            return true;
        }

        private void OnEnterLevel()
        {
            ChangeToState(new() { VisualAnimationType = def.InitialAnimation });
        }

        private IEnumerator Directional(List<GameObject> gos, DirectionalConfig conf, float InitialDelay = 0.0f)
        {
            if(InitialDelay >= 0f)
            {
                yield return new WaitForSeconds(InitialDelay);
            }

            // TODO: implement as sliding window?
            int goInWindow = conf.WindowSize;
            int leastValidIndex = -goInWindow + 1;

            LinkedList<int> windows = new();

            windows.AddFirst(leastValidIndex);

            void SetGOsInRange(int start, int end, bool active)
            {
                start = Math.Max(start, 0);
                end = Math.Min(end, gos.Count);
                for(int i = start; i < end; i++)
                    gos[i].SetActive(active);
            }

            while (true)
            {
                SetGOsInRange(0, gos.Count, false);  

                var n = windows.First;
                while (n != null)
                {
                    int i = n.Value;
                    SetGOsInRange(i, i + goInWindow, true);

                    n.Value = i + 1;
                    
                    if(n == windows.First && 1/*leastValidIndex + goInWindow*/ < n.Value - conf.WindowInterval)
                    {
                        windows.AddFirst(leastValidIndex);
                    }
                    else if(n == windows.Last && n.Value >= gos.Count)
                    {
                        windows.RemoveLast();
                    }
                    n = n.Next;
                }

                yield return new WaitForSeconds(conf.UpdateInterval);
            }
        }

        private IEnumerator Blink(List<GameObject> gos, BlinkConfig conf, float InitialDelay = 0.0f)
        {
            if (InitialDelay >= 0f)
            {
                yield return new WaitForSeconds(InitialDelay);
            }

            while (true)
            {
                foreach (var go in gos)
                {
                    bool active = go.active;
                    go.SetActive(!active);
                }
                yield return new WaitForSeconds(conf.ShowHideTime);
            }
        }

        public void Destroy()
        {
            LevelAPI.OnEnterLevel -= OnEnterLevel;
            DestroyVisualGOs();
            StateReplicator = null;
        }

        public VisualGroup(VisualGroupDefinition def)
        {
            this.def = def;
        }
    }
}
