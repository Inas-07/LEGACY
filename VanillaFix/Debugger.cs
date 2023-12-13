using GameData;
using GTFO.API;
using HarmonyLib;
using LEGACY.Utils;
using LevelGeneration;
using Localization;
using System;
using System.Collections.Generic;

namespace LEGACY.VanillaFix
{
    [HarmonyPatch]
    internal class Debugger
    {
        public static Debugger Current { get; private set; } = new();

        public bool DEBUGGING { get; private set; } = false;

        private Debugger()
        {

        }

        internal void Init()
        {
            if (!DEBUGGING) return;

        }
    }
}
