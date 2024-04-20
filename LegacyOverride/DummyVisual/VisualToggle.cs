using System.Collections;
using UnityEngine;
using IEnumerator = System.Collections.IEnumerator;
using GOEnumerable = System.Collections.Generic.IEnumerable<UnityEngine.GameObject>;
using BepInEx.Unity.IL2CPP.Utils.Collections;

namespace LEGACY.LegacyOverride.DummyVisual
{
    internal enum ToggleType
    {
        INSTANT,
        BLINK
    }

    internal static class ToggleFunctions
    {

        public static void Instant(GameObject go, bool active)
        {
            go.SetActive(active);
        }

        public static void Instant(GOEnumerable gos, bool active)
        {
            foreach(var go in gos)
            {
                Instant(go, active);
            }
        }

        public static void Blink(GameObject go, bool active)
        {
            if(active)
            {
                CoroutineManager.BlinkIn(go);
            }
            else
            {
                CoroutineManager.BlinkOut(go);
            }
        }

        public static void Blink(GOEnumerable gos, bool active)
        {
            foreach(var go in gos)
            {
                Blink(go, active);
            }
        }

        //public static void Lerp(GOEnumerable gos, bool active, float )
        //{

        //}


        //public static void Lerp(GameObject go, bool active)
        //{

        //}
    }
}
