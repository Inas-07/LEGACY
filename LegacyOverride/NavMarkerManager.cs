using GTFO.API;
using LEGACY.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace LEGACY.LegacyOverride
{
    internal class NavMarkerManager
    {
        public static NavMarkerManager Current { get; private set; } = new();

        private Dictionary<string, GameObject> markerVisuals = new();

        private Dictionary<string, NavMarker> navMarkers = new();

        private List<GameObject> arbitraryNavMarkers = new();

        private GameObject InstantiateMarkerVisual()
        {
            var marker = Object.Instantiate(Assets.ObjectiveMarker);
            float height = 0.6f / 3.7f;
            marker.transform.localPosition += Vector3.up * height;

            return marker;
        }

        public void EnableMarkerAt(string markerName, GameObject target, float scale)
        {
            if (navMarkers.ContainsKey(markerName))
            {
                navMarkers[markerName].SetVisible(true);
            }
            else
            {
                NavMarker navMarker = GuiManager.NavMarkerLayer.PrepareGenericMarker(target);
                if (navMarker != null)
                {
                    navMarker.SetColor(new Color(0.855f, 0.482f, 0.976f));
                    navMarker.SetStyle(eNavMarkerStyle.LocationBeaconNoText);
                    navMarker.SetVisible(true);
                    navMarkers[markerName] = navMarker;
                }
                else
                {
                    LegacyLogger.Error("EnableMarkerAt: got null nav marker");
                }
            }

            GameObject markerVisual = null;
            if (markerVisuals.ContainsKey(markerName))
            {
                markerVisual = markerVisuals[markerName];
            }
            else
            {
                markerVisual = InstantiateMarkerVisual();
                markerVisual.transform.localScale = new Vector3(scale, scale, scale);
                markerVisual.transform.SetPositionAndRotation(target.transform.position, Quaternion.identity);
                markerVisuals[markerName] = markerVisual;
            }

            CoroutineManager.BlinkIn(markerVisual);

            // TODO: where to put this shit?
            //switch (objectType)
            //{
            //    case 0: // terminal 
            //        break;
            //    case 1:
            //        var core = target.GetComponent<CarryItemPickup_Core>();
            //        var sync = core.m_sync.Cast<LG_PickupItem_Sync>();
            //        sync.OnSyncStateChange += new System.Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>((_, _, _, _) => {
            //            if (core.CanWarp)
            //            {
            //                DisableMakrer(markerName);
            //            }
            //            else
            //            {
            //                EnableMarkerAt(markerName, target, scale, objectType);
            //            }
            //        });
            //        break;
            //}
            LegacyLogger.Debug($"EnableMarker: marker {markerName} enabled");
        }

        public void EnableArbitraryMarkerAt(string markerName, Vector3 Position)
        {
            if (navMarkers.ContainsKey(markerName))
            {
                navMarkers[markerName].SetVisible(true);
            }
            else
            {
                GameObject arbitraryMarkerGO = new GameObject(markerName);
                arbitraryMarkerGO.transform.SetPositionAndRotation(Position, Quaternion.identity);
                arbitraryNavMarkers.Add(arbitraryMarkerGO);

                NavMarker navMarker = GuiManager.NavMarkerLayer.PrepareGenericMarker(arbitraryMarkerGO);
                if (navMarker != null)
                {
                    navMarker.SetColor(new Color(0.855f, 0.482f, 0.976f));
                    navMarker.SetStyle(eNavMarkerStyle.LocationBeaconNoText);
                    navMarker.SetVisible(true);
                    navMarkers[markerName] = navMarker;
                }
                else
                {
                    LegacyLogger.Error("EnableMarkerAt: got null nav marker");
                }
            }

            LegacyLogger.Debug($"EnableMarker: marker {markerName} enabled");
        }



        internal (GameObject markerVisual, NavMarker navMakrer) GetMarkerVisuals(string markerName) => 
            markerVisuals.TryGetValue(markerName, out var markerVisual) && navMarkers.TryGetValue(markerName, out var navMarker) ? (markerVisual, navMarker) : (null, null);

        public void DisableMakrer(string markerName)
        {
            if (navMarkers.ContainsKey(markerName))
            {
                navMarkers[markerName].SetVisible(false);
            }

            if (!markerVisuals.ContainsKey(markerName))
            {
                return;
            }

            var marker = markerVisuals[markerName];
            if(marker.active)
            {
                CoroutineManager.BlinkOut(marker);
            }
            LegacyLogger.Debug($"DisableMakrer: marker {markerName} disabled");
        }

        public void Clear()
        {
            foreach (var marker in markerVisuals.Values)
            {
                Object.Destroy(marker);
            }

            arbitraryNavMarkers.ForEach(Object.Destroy);

            markerVisuals.Clear();
            navMarkers.Clear();
            arbitraryNavMarkers.Clear();
        }

        private NavMarkerManager()
        {
            LevelAPI.OnBuildStart += Clear;
            LevelAPI.OnLevelCleanup += Clear;
        }
    }
}
