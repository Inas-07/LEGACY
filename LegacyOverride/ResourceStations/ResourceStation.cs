﻿using LEGACY.Utils;
using Player;
using UnityEngine;
using Localization;
using LevelGeneration;
using AIGraph;
using GameData;
using ExtraObjectiveSetup;
using FloLib.Networks.Replications;
using SNetwork;
using System;
using BepInEx.Unity.IL2CPP.Utils.Collections;

namespace LEGACY.LegacyOverride.ResourceStations
{
    public abstract class ResourceStation
    {
        public const int UNLIMITED_USE_TIME = int.MaxValue;

        public virtual GameObject GameObject { get; protected set; }

        public GameObject InteractGO => GameObject?.transform.GetChild(3).gameObject ?? null;

        public GameObject StationMarkerGO => GameObject?.transform.GetChild(2).gameObject ?? null;

        public virtual Interact_Timed Interact { get; protected set; }

        public virtual ResourceStationDefinition def { get; protected set; }


        public RSTimer Timer { get; protected set; } 

        public StateReplicator<RSStateStruct> StateReplicator { get; protected set; }

        public RSStateStruct State => StateReplicator?.State ?? new();

        public LG_GenericTerminalItem TerminalItem { get; protected set; }

        public AIG_CourseNode SpawnNode { get => TerminalItem.SpawnNode; set => TerminalItem.SpawnNode = value; }

        protected int SerialNumber { get; private set; }

        public virtual string ItemKey => $"Resource_Station_{SerialNumber}";

        private Coroutine m_blinkMarkerCoroutine = null;

        public virtual void Destroy()
        {
            if (m_blinkMarkerCoroutine != null)
            {
                CoroutineManager.StopCoroutine(m_blinkMarkerCoroutine);
                m_blinkMarkerCoroutine = null;
            }

            GameObject.Destroy(GameObject);

            StateReplicator?.Unload();
            Interact = null;
            def = null;
            StateReplicator = null;
        }

        public bool Enabled => State.Enabled;

        protected virtual bool CanInteract() => Enabled && !InCooldown;
        
        protected virtual bool InCooldown => State.RemainingUseTime <= 0 && State.CurrentCooldownTime > 0;

        protected abstract void Replenish(PlayerAgent player);

        public virtual bool HasUnlimitedUseTime => def.AllowedUseTimePerCooldown == UNLIMITED_USE_TIME;

        protected virtual void SetInteractionText()
        {
            string button = Text.Format(827U, InputMapper.GetBindingName(Interact.InputAction));
            var additionalInfoTextDB = GameDataBlockBase<TextDataBlock>.GetBlock("InGame.OnAdditionalInteractionText.ResourceStation");
            string additionalInfo = additionalInfoTextDB == null ? "TO REPLENISH" : Text.Get(additionalInfoTextDB.persistentID);
            string remainingUseTimeText = HasUnlimitedUseTime ? string.Empty : $"({State.RemainingUseTime}/{def.AllowedUseTimePerCooldown})";
            GuiManager.InteractionLayer.SetInteractPrompt(Interact.InteractionMessage, $"{button}{additionalInfo}{remainingUseTimeText}", ePUIMessageStyle.Default);
        }

        protected virtual void OnTriggerInteraction(PlayerAgent player) 
        {
            var oldState = State;

            int remainingUseTime = HasUnlimitedUseTime ? UNLIMITED_USE_TIME : Math.Max(oldState.RemainingUseTime - 1, 0);
            int slotIdx = player.Owner.PlayerSlotIndex();
            if (slotIdx < 0 || slotIdx >= SNet.Slots.PlayerSlots.Count)
            {
                LegacyLogger.Error($"ResourceStation_OnTriggerInteraction: player {player.PlayerName} has invalid slot index: {slotIdx}");
                return;
            }

            StateReplicator?.SetState(new() {
                LastInteractedPlayer = slotIdx,
                RemainingUseTime = remainingUseTime,
                CurrentCooldownTime = remainingUseTime == 0 ? def.CooldownTime : 0f,
                Enabled = true,
            });
        }

        protected virtual void OnInteractionSelected(PlayerAgent agent, bool selected)
        {
            if(selected)
            {
                SetInteractionText();
            }
        }

        protected virtual void SetupInteraction()
        {
            Interact.InteractDuration = def.InteractDuration;
            Interact.ExternalPlayerCanInteract += new System.Func<PlayerAgent, bool>((_) => CanInteract());
            Interact.OnInteractionSelected += new System.Action<PlayerAgent, bool>(OnInteractionSelected);
            Interact.OnInteractionTriggered += new System.Action<PlayerAgent>(OnTriggerInteraction);
        }

        protected virtual void SetupTerminalItem()
        {
            if(!Builder.CurrentFloor.TryGetZoneByLocalIndex(def.DimensionIndex, def.LayerType, def.LocalIndex, out var zone) || zone == null)
            {
                LegacyLogger.Error($"ResourceStation: Cannot find spawn node!");
                return;
            }

            if(def.AreaIndex < 0 || def.AreaIndex >= zone.m_areas.Count) 
            {
                LegacyLogger.Error($"ResourceStation: Cannot find spawn node - Area index is invalid!");
                return;
            }

            TerminalItem.Setup(ItemKey, zone.m_areas[def.AreaIndex].m_courseNode);
            if (SpawnNode != null)
            {
                TerminalItem.FloorItemLocation = SpawnNode.m_zone.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore);
            }

            // NOTE: unfortunately System.Func does not work as System.Action
            //TerminalItem.OnWantDetailedInfo = new System.Func<Il2cppStringList, Il2cppStringList>((defaultDetails) =>
            //    {
            //        var list = new List<string>();
            //        list.Add("----------------------------------------------------------------");
            //        list.Add("RESOURCE STATION");
            //        list.AddRange(defaultDetails.ToManagedList());

            //        return list.ToIl2Cpp();
            //    }
            //);

            TerminalItem.FloorItemStatus = eFloorInventoryObjectStatus.Normal;
        }

        protected virtual void SetEnabled(bool enabled)
        {
            Interact.SetActive(enabled);
            StationMarkerGO.SetActive(enabled);
        }

        protected virtual void OnStateChanged(RSStateStruct oldState, RSStateStruct newState, bool isRecall)
        {
            if (isRecall) return;

            if(Interact.IsSelected)
            {
                SetInteractionText();
            }

            if (SNet.IsMaster) 
            {

                if(newState.Enabled)
                {
                    SetEnabled(true);

                    if(!oldState.Enabled && Timer.HasOnGoingTimer)
                    {
                        if (m_blinkMarkerCoroutine == null)
                        {
                            m_blinkMarkerCoroutine = CoroutineManager.StartCoroutine(BlinkMarker().WrapToIl2Cpp());
                        }
                    }

                    int playerSlot = newState.LastInteractedPlayer;
                    if (oldState.RemainingUseTime > 0 && 0 <= playerSlot && playerSlot < SNet.Slots.PlayerSlots.Count)
                    {                        
                        var player = SNet.Slots.GetPlayerInSlot(playerSlot);
                        if (player != null)
                        {
                            Replenish(player.m_playerAgent.Cast<PlayerAgent>());
                            if (newState.RemainingUseTime == 0)
                            {
                                LegacyLogger.Warning($"ResourceStation OnStateChanged: cooldown timer starts!");
                                OnCoolDownStart();
                            }

                            LegacyLogger.Warning($"ResourceStation OnStateChanged: replenish for player {playerSlot}, remaining use time: {newState.RemainingUseTime}");
                        }
                        else
                        {
                            LegacyLogger.Error($"playerSlot_{playerSlot} has no player agent!");
                        }
                    }
                }
                else
                {
                    SetEnabled(false);
                    if(m_blinkMarkerCoroutine != null)
                    {
                        CoroutineManager.StopCoroutine(m_blinkMarkerCoroutine);
                        m_blinkMarkerCoroutine = null;
                    }
                }
            }
        }

        protected virtual void OnCoolDownStart()
        {
            Timer.StartTimer(def.CooldownTime);
            if (m_blinkMarkerCoroutine == null)
            {
                m_blinkMarkerCoroutine = CoroutineManager.StartCoroutine(BlinkMarker().WrapToIl2Cpp());
            }
        }

        protected virtual void OnCoolDownTimerProgress(float progress)
        {

        }

        protected virtual void OnCoolDownEnd()
        {
            LegacyLogger.Warning($"ResourceStation OnCoolDownEnd");
            if(m_blinkMarkerCoroutine != null)
            {
                CoroutineManager.StopCoroutine(m_blinkMarkerCoroutine);
                m_blinkMarkerCoroutine = null;
            }

            if (SNet.IsMaster)
            {
                LegacyLogger.Warning($"ResourceStation OnCoolDownEnd: master reset state!");
                StateReplicator.SetState(new RSStateStruct() { 
                    LastInteractedPlayer = -1,
                    RemainingUseTime = def.AllowedUseTimePerCooldown,
                    CurrentCooldownTime = 0,
                    Enabled = State.Enabled,
                });
            }
        }

        protected virtual void SetupReplicator()
        {
            if (StateReplicator != null) return;

            uint id = EOSNetworking.AllotReplicatorID();
            if (id == EOSNetworking.INVALID_ID)
            {
                LegacyLogger.Error("ResourceStation: replicatorID depleted, cannot setup replicator!");
                return;
            }

            StateReplicator = StateReplicator<RSStateStruct>.Create(id, new() { RemainingUseTime = def.AllowedUseTimePerCooldown, CurrentCooldownTime = -1f, Enabled = true }, LifeTimeType.Level);
            StateReplicator.OnStateChanged += OnStateChanged;
        }

        protected virtual void SetupRSTimer()
        {
            if(Timer == null)
            {
                Timer = RSTimer.Instantiate(OnCoolDownTimerProgress, OnCoolDownEnd);
            }
        }

        private System.Collections.IEnumerator BlinkMarker()
        {
            const float BLINK_INTERVAL = 0.5f;
            while(true)
            {
                StationMarkerGO.SetActive(!StationMarkerGO.active);
                yield return new WaitForSeconds(BLINK_INTERVAL);
            }
        }

        protected ResourceStation(ResourceStationDefinition def, GameObject GO) 
        { 
            this.def = def;
            GameObject = GO;
            GameObject.transform.SetPositionAndRotation(def.Position.ToVector3(), def.Rotation.ToQuaternion());
            Interact = InteractGO.GetComponent<Interact_Timed>();
            SerialNumber = SerialGenerator.GetUniqueSerialNo();

            if (Interact == null)
            {
                LegacyLogger.Error("ResourceStation: Interact Comp not found!");
            }
            else
            {
                SetupInteraction();
            }

            TerminalItem = GO.GetComponent<LG_GenericTerminalItem>();
            if (TerminalItem == null)
            {
                LegacyLogger.Error("ResourceStation: TerminalItem not found!");
            }
            else
            {
                SetupTerminalItem();
            }

            SetupReplicator();
            SetupRSTimer();
        }

        static ResourceStation () 
        {

        }
    }
}
