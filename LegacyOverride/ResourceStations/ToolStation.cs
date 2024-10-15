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
    public sealed class ToolStation: ResourceStation
    {
        public override string ItemKey => $"Tool_Station_{SerialNumber}";

        protected override void SetupInteraction()
        {
            base.SetupInteraction();

            var interactTextDB = GameDataBlockBase<TextDataBlock>.GetBlock("InGame.InteractionPrompt.ToolStation");
            Interact.InteractionMessage = interactTextDB == null ? "TOOL STATION" : Text.Get(interactTextDB.persistentID);
        }

        protected override void Replenish(PlayerAgent player)
        {
            PlayerBackpack backpack = PlayerBackpackManager.GetBackpack(player.Owner);
            PlayerAmmoStorage ammoStorage = backpack.AmmoStorage;
            //float _class = Math.Max(0f, Math.Min(def.SupplyUplimit.AmmoSpecial - 1.0f * ammoStorage.SpecialAmmo.BulletClipSize / ammoStorage.SpecialAmmo.BulletsMaxCap - ammoStorage.SpecialAmmo.RelInPack, def.SupplyEfficiency.AmmoSpecial));
            float _class = Math.Max(0f, Math.Min(def.SupplyUplimit.Tool - ammoStorage.ClassAmmo.RelInPack, def.SupplyEfficiency.Tool));

            player.GiveAmmoRel(null, 0f, 0f, _class);
            player.Sound.Post(EVENTS.AMMOPACK_APPLY, true);
        }

        public static ToolStation Instantiate(ResourceStationDefinition def)
        {
            if(def.StationType != StationType.TOOL)
            {
                LegacyLogger.Error($"Trying to instantiate MediStation with def with 'StationType': {def.StationType}!");
                return null;
            }

            GameObject templateGO = GameObject.Instantiate(Assets.ToolStation);
            var station = new ToolStation(def, templateGO);
            return station;
        }

        private ToolStation(ResourceStationDefinition def, GameObject GO) : base(def, GO) { }

        static ToolStation() { }
    }
}
