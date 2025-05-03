using CellMenu;
using FluffyUnderware.Curvy.Generator;
using Il2CppInterop.Runtime.Injection;
using LEGACY.Utils;
using PlayFab.AuthenticationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace LEGACY.LegacyOverride.Restart
{
    //public class CM_PageRestart: MonoBehaviour
    public static class CM_PageRestart
    {
        internal static TextMeshPro Text { get; private set; }

        internal static GameObject Reconnecting { get; private set; }

        //private void SetupPage()
        //{
        //    var movingContentHolder = gameObject.GetChild("MovingContentHolder");
        //    Reconnecting = movingContentHolder.GetChild("Reconnecting");

        //    var text = GuiManager.Current.m_mainMenuLayer.PageMap.m_mapDisconnected.transform.GetChild(0).gameObject;
        //    text = GameObject.Instantiate(text, Reconnecting.transform);
        //    Text = text.GetComponent<TextMeshPro>();
        //    Text.color = new Color(0f, 1f, 216f / 255f, 1f);
        //    Text.SetText("RECONNECTING");
        //    Text.ForceMeshUpdate();
        //}

        public static GameObject Page { get; private set; } = null;

        internal static void Setup()
        {
            if (Assets.RestartPage == null) return;
            if (Page != null)
            {
                LegacyLogger.Warning("Duplicate setup for CM_PageRestart!");
                try
                {
                    GameObject.Destroy(Page);
                }
                finally
                {
                    Page = null;
                }
            }

            var parentAlign = GuiManager.Current.m_mainMenuLayer.GuiLayerBase.transform;
            Page = GameObject.Instantiate(Assets.RestartPage, parentAlign.position, parentAlign.rotation, parentAlign);
            //var comp = Page.AddComponent<CM_PageRestart>();
            //comp.SetupPage();
            var pageBase = Page.GetComponent<CM_PageBase>();

            //var movingContentHolder = gameObject.GetChild("MovingContentHolder");
            var movingContentHolder = pageBase.m_movingContentHolder.gameObject;
            Reconnecting = movingContentHolder.GetChild("Reconnecting");

            var text = GuiManager.Current.m_mainMenuLayer.PageMap.m_mapDisconnected.transform.GetChild(0).gameObject;
            text = GameObject.Instantiate(text, Reconnecting.transform);
            Text = text.GetComponent<TextMeshPro>();
            Text.color = new Color(0f, 1f, 216f / 255f, 1f);
            Text.SetText("RECONNECTING");
            Text.ForceMeshUpdate();

            Page.GetComponent<CM_PageBase>().Setup(GuiManager.Current.m_mainMenuLayer);
        }

        public static void SetPageActive(bool active)
        {
            Page.GetComponent<CM_PageBase>().SetPageActive(active);
        }
    }
}
