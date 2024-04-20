using GTFO.API;
using UnityEngine;

namespace LEGACY
{
    internal static class Assets
    {
        public static GameObject CircleSensor { get; private set; }

        public static GameObject MovableSensor { get; private set; }

        public static GameObject OBSVisual { get; private set; }

        public static GameObject ObjectiveMarker { get; private set; }

        public static GameObject EventScan { get; private set; }

        internal static GameObject DummyScan { get; private set; }

        internal static GameObject DummySensor { get; private set; }

        public static void Init()
        {
            CircleSensor = AssetAPI.GetLoadedAsset<GameObject>("Assets/SecuritySensor/CircleSensor.prefab");
            MovableSensor = AssetAPI.GetLoadedAsset<GameObject>("Assets/SecuritySensor/MovableSensor.prefab");
            OBSVisual = AssetAPI.GetLoadedAsset<GameObject>("Assets/SecuritySensor/OBSVisual.prefab");
            ObjectiveMarker = AssetAPI.GetLoadedAsset<GameObject>("Assets/SecuritySensor/ObjectiveMarker.prefab");
            EventScan = AssetAPI.GetLoadedAsset<GameObject>("Assets/EventObjects/EventScan.prefab");
            DummyScan = AssetAPI.GetLoadedAsset<GameObject>("Assets/DummyVisual/DummyScan.prefab");
            DummySensor = AssetAPI.GetLoadedAsset<GameObject>("Assets/DummyVisual/DummySensor.prefab");
        }
    }
}
