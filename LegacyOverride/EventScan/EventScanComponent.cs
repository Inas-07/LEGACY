using ExtraObjectiveSetup;
using FloLib.Networks.Replications;
using GTFO.API;
using Il2CppInterop.Runtime.Injection;
using LEGACY.Utils;
using Player;
using ScanPosOverride.Managers;
using SNetwork;
using System;
using TMPro;
using UnityEngine;

namespace LEGACY.LegacyOverride.EventScan
{
    // TODO: replicate checkpoint bug
    internal class EventScanComponent : MonoBehaviour
    {
        public const float UPDATE_INTERVAL = 0.3f;

        public const string VANILLA_CP_PREFAB_PATH = "Assets/AssetPrefabs/Complex/Generic/ChainedPuzzles/CP_Bioscan_sustained_RequireAll.prefab";

        public GameObject Cylinder => gameObject.transform.GetChild(0).GetChild(0).gameObject;

        public GameObject Visual => gameObject.transform.GetChild(0).GetChild(1).gameObject;
        
        public GameObject Information => gameObject.transform.GetChild(0).GetChild(2).gameObject;

        public GameObject TextMeshProGO => Information.transform.GetChild(0).gameObject;

        public Renderer VisualRenderer { get; private set; }

        public TextMeshPro DisplayText { get; private set; }

        private Vector3 Position => gameObject.transform.position;

        public StateReplicator<EventScanStatus> StateReplicator { get; private set; }

        private float time = float.NaN;

        public EventScanDefinition def { get; internal set; }

        public Color Color_Waiting { get; private set; } 

        public Color Color_Active { get; private set; }

        private float m_colorLerpDelta = 0.0f;


        private const float LERP_DURATION = 0.5f;

        public void Setup()
        {
            if(def == null)
            {
                LegacyLogger.Error("EventScan Setup: assign a EventScanDefinition before calling Setup()!");
                return;
            }

            gameObject.transform.SetPositionAndRotation(def.Position.ToVector3(), Quaternion.identity);

            gameObject.SetActiveRecursively(true);

            var vanillaCP = AssetAPI.GetLoadedAsset<GameObject>(VANILLA_CP_PREFAB_PATH);
            if (vanillaCP != null)
            {
                var templateGO = vanillaCP.transform.GetChild(0).GetChild(1).gameObject;
                var newGO = Instantiate(templateGO.gameObject);

                newGO.transform.SetParent(gameObject.transform, false);
                newGO.transform.localScale = new Vector3(1 / def.Radius, 1 / def.Radius, 1 / def.Radius);
                DisplayText = newGO.GetComponentInChildren<TextMeshPro>();
                if(DisplayText != null)
                {
                    DisplayText.SetText(def.DisplayText);
                    DisplayText.ForceMeshUpdate();
                }
                else
                {
                    LegacyLogger.Error("EventScan: instantiation error - cannot find TMPPro from vanilla CP!");
                }
            }
            else
            {
                LegacyLogger.Error("EventScan: instantiation error - cannot instantiate vanilla CP!");
            }

            var _cWaiting = def.ColorSetting.Waiting;
            var _cActive = def.ColorSetting.Active;
            Color_Waiting = new Color(_cWaiting.x, _cWaiting.y, _cWaiting.z);
            Color_Active = new Color(_cActive.x, _cActive.y, _cActive.z);

            float height = 0.6f / 3.7f;
            gameObject.transform.localScale = new Vector3(def.Radius, def.Radius, def.Radius);
            gameObject.transform.localPosition += Vector3.up * height;

            VisualRenderer = Visual.gameObject.GetComponentInChildren<Renderer>();
            VisualRenderer.material.SetColor("_ColorA", Color_Waiting);

            uint id = EOSNetworking.AllotReplicatorID();
            if(id == EOSNetworking.INVALID_ID) 
            {
                LegacyLogger.Error("EventScan: Replicator ID depleted, cannot setup");
                return;
            }

            StateReplicator = StateReplicator<EventScanStatus>.Create(id, new() { Status = EventScanState.Waiting }, LifeTimeType.Level);
            StateReplicator.OnStateChanged += OnStateChange;
        }

        private void OnStateChange(EventScanStatus oldState, EventScanStatus newState, bool isRecall)
        {
            LegacyLogger.Warning($"EventScan: {oldState.Status} => {newState.Status}");

            //if (oldState == newState) return; // TODO: comment out this line

            switch (newState.Status)
            {
                case EventScanState.Disabled:
                    m_colorLerpDelta = 0.0f;

                    DisplayText?.gameObject.SetActive(false);
                    Cylinder.SetActive(false);
                    CoroutineManager.BlinkOut(Visual);
                    break;
                case EventScanState.Waiting:
                    if (!Visual.active)
                    {
                        CoroutineManager.BlinkIn(Visual);
                        Cylinder.SetActive(true);
                        DisplayText?.gameObject.SetActive(true);
                    }

                    if(!isRecall) 
                    {
                        if (oldState.Status == EventScanState.Active)
                        {
                            def.EventsOnDeactivate.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, GameData.eWardenObjectiveEventTrigger.None, true));
                        }
                    }
                    break;
                case EventScanState.Active:
                    if (!Visual.active)
                    {
                        CoroutineManager.BlinkIn(Visual);
                        Cylinder.SetActive(true);
                        DisplayText?.gameObject.SetActive(true);
                    }

                    if(!isRecall)
                    {
                        if (oldState.Status == EventScanState.Waiting)
                        {
                            def.EventsOnActivate.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, GameData.eWardenObjectiveEventTrigger.None, true));
                        }
                    }
                    break;
            }
        }

        public void ChangeToState(EventScanState newState)
        {
            ChangedToStateUnsynced(newState);
            if(SNet.IsMaster)
            {
                StateReplicator.SetState(new() { Status = newState });
            }
        }

        private void ChangedToStateUnsynced(EventScanState newState)
        {

        }

        private void Update()
        {
            var curState = StateReplicator.State.Status;
            if (curState == EventScanState.Disabled) return;

            // handle color lerp
            float delta = Clock.Delta / LERP_DURATION;
            if(curState == EventScanState.Waiting) delta = -delta;
            m_colorLerpDelta = Mathf.Clamp01(m_colorLerpDelta + delta);
            
            var curColor = Color.Lerp(Color_Waiting, Color_Active, Mathf.Pow(m_colorLerpDelta, 5.0f));
            VisualRenderer.sharedMaterial.SetColor("_ColorA", curColor);

            // handle event scan logic
            if (!float.IsNaN(time) && Clock.Time < time + UPDATE_INTERVAL) { return; }
            time = Clock.Time + UPDATE_INTERVAL;

            if (def.ActiveCondition.RequiredPlayerCount == 0 && def.ActiveCondition.RequiredBigPickupIndices.Count == 0)
            {
                //LegacyLogger.Error("EventScan: 'ActiveCondition' is invalid, make sure it requires at least one of 'PlayerCount' and 'BigPickupItem'");
                return;
            }

            bool playerSatisfied = false;
            bool reqItemSatisfied = false;

            if (def.ActiveCondition.RequiredPlayerCount > 0)
            {
                int count = 0;
                foreach (var p in PlayerManager.PlayerAgentsInLevel)
                {
                    if ((Position - p.Position).magnitude < def.Radius)
                    {
                        count++;
                        if (count >= def.ActiveCondition.RequiredPlayerCount)
                        {
                            playerSatisfied = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                playerSatisfied = true;
            }

            if (playerSatisfied)
            {
                var reqItemIndices = def.ActiveCondition.RequiredBigPickupIndices;
                if (reqItemIndices.Count > 0)
                {
                    int count = 0;
                    foreach (int index in reqItemIndices)
                    {
                        var item = PuzzleReqItemManager.Current.GetBigPickupItem(index);
                        if (item == null)
                        {
                            count++;
                            continue;
                        }

                        var itemState = item.m_sync.GetCurrentState();
                        Vector3 pos = Vector3.zero;
                        switch (itemState.status)
                        {
                            case LevelGeneration.ePickupItemStatus.PlacedInLevel:
                                pos = item.transform.position;
                                break;

                            case LevelGeneration.ePickupItemStatus.PickedUp:
                                pos = item.PickedUpByPlayer.transform.position;
                                break;

                            default: LegacyLogger.Error($"Item has invalid state: {itemState.status}"); continue;
                        }

                        if ((Position - pos).magnitude < def.Radius)
                        {
                            count += 1;
                        }
                    }

                    reqItemSatisfied = count >= reqItemIndices.Count;
                }
                else
                {
                    reqItemSatisfied = true;
                }
            }

            switch(curState)
            {
                case EventScanState.Waiting:
                    if(playerSatisfied && reqItemSatisfied)
                    {
                        ChangeToState(EventScanState.Active);
                    }
                    break;
                case EventScanState.Active:
                    if (!playerSatisfied || !reqItemSatisfied)
                    {
                        ChangeToState(EventScanState.Waiting);
                    }
                    break;
            }
        }

        private void OnDestroy()
        {
            def = null;
            VisualRenderer = null;
            DisplayText = null;
            StateReplicator = null;
        }

        static EventScanComponent()
        {
            ClassInjector.RegisterTypeInIl2Cpp<EventScanComponent>();
        }
    }
}
