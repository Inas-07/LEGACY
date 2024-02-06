using GTFO.API;
using UnityEngine;

namespace LEGACY
{
    internal static class Assets
    {
        public static GameObject OBSVisual { get; private set; }

        public static GameObject ObjectiveMarker { get; private set; }

        public static void Init()
        {
            OBSVisual = AssetAPI.GetLoadedAsset<GameObject>("Assets/SecuritySensor/OBSVisual.prefab");
            ObjectiveMarker = AssetAPI.GetLoadedAsset<GameObject>("Assets/SecuritySensor/ObjectiveMarker.prefab");
        }
    }
}
