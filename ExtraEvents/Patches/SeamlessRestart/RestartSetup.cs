using CellMenu;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LEGACY.ExtraEvents.Patches.SeamlessRestart
{
    [HarmonyPatch]
    internal static class RestartSetup
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(MainMenuGuiLayer), nameof(MainMenuGuiLayer.Setup))]
        private static void Post_(MainMenuGuiLayer __instance, Transform root, string name)
        {
            RestartPage.Current.Setup(root);
        }
    }
}
