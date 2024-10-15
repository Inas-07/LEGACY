using AK;
using GameData;
using GameEvent;
using Gear;
using LEGACY.Utils;
using Localization;
using Player;
using System;
using UnityEngine;

namespace LEGACY.LegacyOverride.ResourceStations
{
    public sealed class MediStation: ResourceStation
    {
        public const float VANILLA_MAX_HEALTH = 25f;

        public override string ItemKey => $"Health_Station_{SerialNumber}";

        protected override void SetupInteraction()
        {
            base.SetupInteraction();

            var interactTextDB = GameDataBlockBase<TextDataBlock>.GetBlock("InGame.InteractionPrompt.MediStation");
            Interact.InteractionMessage = interactTextDB == null ? "HEALTH STATION" : Text.Get(interactTextDB.persistentID);
        }

        protected override void Replenish(PlayerAgent player)
        {
            float curHealth = player.Damage.Health;
            float maxHealth = def.SupplyUplimit.Medi * VANILLA_MAX_HEALTH;

            if (curHealth >= maxHealth) return;

            player.GiveHealth(null, Math.Min(def.SupplyEfficiency.Medi, (maxHealth - curHealth) / VANILLA_MAX_HEALTH));
            player.Sound.Post(EVENTS.MEDPACK_APPLY, true);
        }

        public static MediStation Instantiate(ResourceStationDefinition def)
        {
            if(def.StationType != StationType.MEDI)
            {
                LegacyLogger.Error($"Trying to instantiate MediStation with def with 'StationType': {def.StationType}!");
                return null;
            }

            GameObject templateGO = GameObject.Instantiate(Assets.MediStation);
            var station = new MediStation(def, templateGO);
            return station;
        }

        private MediStation(ResourceStationDefinition def, GameObject GO) : base(def, GO) { }

        static MediStation() { }
    }
}
