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
    public sealed class AmmoStation: ResourceStation
    {
        public override string ItemKey => $"Ammunition_Station_{SerialNumber}";

        protected override void SetupInteraction()
        {
            base.SetupInteraction();

            var interactTextDB = GameDataBlockBase<TextDataBlock>.GetBlock("InGame.InteractionPrompt.AmmoStation");
            Interact.InteractionMessage = interactTextDB == null ? "AMMUNITION STATION" : Text.Get(interactTextDB.persistentID);
        }

        protected override void OnTriggerInteractionAction(PlayerAgent player)
        {
            PlayerBackpack backpack = PlayerBackpackManager.GetBackpack(player.Owner);
            PlayerAmmoStorage ammoStorage = backpack.AmmoStorage;
            float standard = Math.Max(0f, Math.Min(def.SupplyUplimit.AmmoStandard - 1.0f * ammoStorage.StandardAmmo.BulletClipSize / ammoStorage.StandardAmmo.BulletsMaxCap - ammoStorage.StandardAmmo.RelInPack, def.SupplyEfficiency.AmmoStandard));
            float special = Math.Max(0f, Math.Min(def.SupplyUplimit.AmmoSpecial - 1.0f * ammoStorage.SpecialAmmo.BulletClipSize / ammoStorage.SpecialAmmo.BulletsMaxCap - ammoStorage.SpecialAmmo.RelInPack, def.SupplyEfficiency.AmmoSpecial));

            player.GiveAmmoRel(player, standard, special, 0f);
            player.Sound.Post(EVENTS.AMMOPACK_APPLY, true);
        }

        public static AmmoStation Instantiate(ResourceStationDefinition def)
        {
            if(def.StationType != StationType.AMMO)
            {
                LegacyLogger.Error($"Trying to instantiate AmmoStation with def with 'StationType': {def.StationType}!");
                return null;
            }

            GameObject templateGO = GameObject.Instantiate(Assets.AmmoStation);
            var station = new AmmoStation(def, templateGO);
            return station;
        }

        private AmmoStation(ResourceStationDefinition def, GameObject GO) : base(def, GO) { }

        static AmmoStation () { }
    }
}
