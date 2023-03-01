//using BepInEx.Unity.IL2CPP.Utils;
//using GameData;
//using LevelGeneration;
//using Player;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics.CodeAnalysis;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;

//namespace LEGACY.PlayerPortal
//{
//    internal class PlayerPortalManager
//    {
//        private class Zone 
//        {
//            internal eDimensionIndex DimensionIndex {get;set;}
//            internal LG_LayerType Layer { get; set; }
//            internal eLocalZoneIndex LocalIndex { get; set; }

//            public override bool Equals(object obj) => this.Equals(obj as Zone);

//            public bool Equals(Zone that)
//            {
//                if (that is null)
//                {
//                    return false;
//                }

//                // Optimization for a common success case.
//                if (ReferenceEquals(this, that))
//                {
//                    return true;
//                }

//                // If run-time types are not exactly the same, return false.
//                if (this.GetType() != that.GetType())
//                {
//                    return false;
//                }

//                // Return true if the fields match.
//                // Note that the base class is not invoked because it is
//                // System.Object, which defines Equals as reference equality.
//                return this.DimensionIndex == that.DimensionIndex && this.Layer == that.Layer && this.LocalIndex == that.LocalIndex;
//            }

//            public override int GetHashCode() => (DimensionIndex, Layer, LocalIndex).GetHashCode();
            
//        }

//        internal static readonly PlayerPortalManager Current;

//        private HashSet<Zone> portalZones = new();

//        private Coroutine playerZoneListener;

//        static PlayerPortalManager()
//        {
//            Current = new();
//        }

//        internal bool ActivatePortalZone(eDimensionIndex dimensionIndex, LG_LayerType layer, eLocalZoneIndex localIndex)
//        {
//            if(portalZones.Count <= 0)
//            {
//                playerZoneListener = CoroutineManager.StartCoroutine();
//            }

//            var portalZoneToAdd = new Zone()
//            {
//                DimensionIndex = dimensionIndex,
//                Layer = layer,
//                LocalIndex = localIndex
//            };
            
//            return portalZones.Add(portalZoneToAdd);
//        }

//        internal bool DeactivatePortalZone(eDimensionIndex dimensionIndex, LG_LayerType layer, eLocalZoneIndex localIndex)
//        {
//            if (portalZones.Count == 1)
//            {
//                CoroutineManager.StopCoroutine(playerZoneListener);
//                playerZoneListener = null;
//            }

//            var portalZone = new Zone()
//            {
//                DimensionIndex = dimensionIndex,
//                Layer = layer,
//                LocalIndex = localIndex
//            };

//            return portalZones.Remove(portalZone);
//        }


//        private 
//    }
//}
