using CellMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LEGACY.ExtraEvents.Patches.SeamlessRestart
{
    public class RestartPage
    {
        public static RestartPage Current { get; } = new();

        private CM_PageMap m_page;

        internal void Setup(Transform root)
        {
            var mainMenuGuiLayer = GuiManager.MainMenuLayer;

            string pageResourcePath = "CM_PageMap_CellUI";
            m_page = GOUtil.SpawnChildAndGetComp<CM_PageBase>(Resources.Load<GameObject>(pageResourcePath), mainMenuGuiLayer.GuiLayerBase.transform).Cast<CM_PageMap>();

            m_page.Setup(mainMenuGuiLayer);


        }

        private RestartPage()
        {

        }

        static RestartPage()
        {

        }
    }
}
