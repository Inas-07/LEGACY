using LEGACY.Utils;
using Player;
using UnityEngine;
using Localization;
using LevelGeneration;
using AIGraph;
using GameData;
using ExtraObjectiveSetup;
using FloLib.Networks.Replications;
namespace LEGACY.LegacyOverride.ResourceStations
{
    public abstract class ResourceStation
    {
        public virtual GameObject GameObject { get; protected set; }

        public virtual GameObject InteractGO => GameObject?.transform.GetChild(3).gameObject ?? null;

        public virtual GameObject StationMarkerGO => GameObject?.transform.GetChild(2).gameObject ?? null;

        public virtual Interact_Timed Interact { get; protected set; }

        public virtual ResourceStationDefinition def { get; protected set; }

        //public RSTimer Timer { get; protected set; } = RSTimer.Instantiate();

        public StateReplicator<RSStateStruct> StateReplicator { get; protected set; }

        public LG_GenericTerminalItem TerminalItem { get; protected set; }

        public AIG_CourseNode SpawnNode { get => TerminalItem.SpawnNode; set => TerminalItem.SpawnNode = value; }

        protected int SerialNumber { get; private set; }

        public virtual string ItemKey => $"Resource_Station_{SerialNumber}";

        public virtual void Destroy()
        {
            GameObject.Destroy(GameObject);
            Interact = null;
            def = null;
        }

        protected virtual void OnTriggerInteractionAction(PlayerAgent player) { }

        protected virtual void OnInteractionSelected(PlayerAgent agent, bool selected)
        {
            if (selected)
            {
                string button = Text.Format(827U, InputMapper.GetBindingName(Interact.InputAction));
                var additionalInfoTextDB = GameDataBlockBase<TextDataBlock>.GetBlock("InGame.OnAdditionalInteractionText.ResourceStation");
                string additionalInfo = additionalInfoTextDB == null ? "TO REPLENISH" : Text.Get(additionalInfoTextDB.persistentID);

                GuiManager.InteractionLayer.SetInteractPrompt(Interact.InteractionMessage,
                   $"{button}{additionalInfo}", ePUIMessageStyle.Default);
            }
        }

        protected virtual void SetupInteraction()
        {
            Interact.InteractDuration = def.InteractDuration;
            Interact.OnInteractionSelected += new System.Action<PlayerAgent, bool>(OnInteractionSelected);
            Interact.OnInteractionTriggered += new System.Action<PlayerAgent>(OnTriggerInteractionAction);
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

        protected virtual void OnStateChanged(RSStateStruct oldState, RSStateStruct newState, bool isRecall)
        {
            //if (!isRecall) return;


        }

        protected virtual void SetupReplicator()
        {
            //uint id = EOSNetworking.AllotReplicatorID();
            //if (id == EOSNetworking.INVALID_ID)
            //{
            //    LegacyLogger.Error("ResourceStation: replicatorID depleted, cannot setup replicator!");
            //    return;
            //}

            //StateReplicator = StateReplicator<RSStateStruct>.Create(id, new() { RemainingUseTime = def.AllowedUseTimePerCooldown, CurrentCooldownTime = -1f, Enabled = true }, LifeTimeType.Level);
            //StateReplicator.OnStateChanged += OnStateChanged;
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
        }


        static ResourceStation () 
        {

        }
    }
}
