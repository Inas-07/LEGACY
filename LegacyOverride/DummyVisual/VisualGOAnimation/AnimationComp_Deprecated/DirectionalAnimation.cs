using Il2CppInterop.Runtime.Injection;
using LEGACY.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LEGACY.LegacyOverride.DummyVisual.VisualGOAnimation.AnimationComp_Deprecated
{
    internal class DirectionalAnimation : MonoBehaviour
    {
        internal GameObject VisualGO;

        internal float InitialDelay = 0.0f;

        internal float ShowHideTime = 0.0f;

        internal bool ShowCylinder = false;

        private GameObject Cylinder => VisualGO?.transform.GetChild(0).GetChild(0).gameObject;

        private GameObject Zone => VisualGO?.transform.GetChild(0).GetChild(1).gameObject;

        private GameObject Text => VisualGO?.transform.GetChild(0).GetChild(2).gameObject;

        private float NextUpdateTime = float.PositiveInfinity;

        private bool PlayAnimation = false;

        internal void SetPlayAnimation(bool play)
        {
            PlayAnimation = play;
            if (play)
            {
                NextUpdateTime = Clock.Time + InitialDelay;
            }
            else
            {
                NextUpdateTime = float.PositiveInfinity;
            }
        }

        private void Update()
        {
            if (VisualGO == null || !PlayAnimation) return;

            if (Clock.Time < NextUpdateTime) return;

            bool active = Zone.active;
            Zone.SetActive(!active);

            if (ShowCylinder)
            {
                Cylinder.SetActive(!active);
            }
            else
            {
                Cylinder.SetActive(false);
            }

            Text.SetActive(!active);
            NextUpdateTime = Clock.Time + ShowHideTime;
        }

        private void OnDestroy()
        {
            VisualGO = null;
        }

        static DirectionalAnimation()
        {
            ClassInjector.RegisterTypeInIl2Cpp<DirectionalAnimation>();
        }
    }
}
