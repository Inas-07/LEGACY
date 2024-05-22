//using ExtraObjectiveSetup.Expedition.Gears;
//using LEGACY.Utils;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;

//namespace LEGACY.LegacyOverride.ThermalSightAdjustment
//{
//    internal class ThermalGearWrapper
//    {
//        public uint GearPID { get; private set; } 

//        public Renderer[] SightRenders { get; private set; }

//        public ItemEquippable GearItem { get; private set; }

//        public bool Modified { get; private set; } = false;

//        public bool IsGearDestroyed()
//        {
//            if (!HasThermalSight) return true;

//            try
//            {
//                bool active = SightRenders[0].gameObject.active;
//                SightRenders[0].gameObject.SetActive(active); // see if it's destroyed
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        private void GetSightRender()
//        {
//            if(GearItem == null)
//            {
//                LegacyLogger.Error($"GetSightRender: GearItem is null");
//                return;
//            }

//            SightRenders = GearItem.GetComponentsInChildren<Renderer>(true)
//                ?.Where(x => x.sharedMaterial != null && x.sharedMaterial.shader != null)
//                .Where(x => x.sharedMaterial.shader.name.ToLower().Contains("Thermal".ToLower()))
//                .ToArray() ?? null;
//        }

//        public bool HasThermalSight => SightRenders != null && SightRenders.Length > 0;

//        public void SetupOnGearItem(ItemEquippable gearItem)
//        {
//            if (gearItem.GearIDRange == null) 
//            {
//                LegacyLogger.Error("ThermalGearWrapper: trying to setup on a gear item without GearIDRange!");
//                return;
//            }

//            if(GearItem != null)
//            {
//                Clear();
//            }

//            GearItem = gearItem;
//            GearPID = ExpeditionGearManager.GetOfflineGearPID(gearItem.GearIDRange);

//            GetSightRender();
//        }

//        public void Clear()
//        {
//            GearItem = null;
//            SightRenders = null;
//            Modified = false;
//            GearPID = 0;
//        }

//        public ThermalGearWrapper() { }
//    }
//}
