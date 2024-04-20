using GTFO.API;
using Il2CppInterop.Runtime.Injection;
using System.Collections.Generic;
using LevelGeneration;
using System;
using System.Text;
using UnityEngine;
using GameData;
using Localization;

namespace LEGACY.LegacyOverride.SecDoorIntText
{
    public class InteractGlitchManager : MonoBehaviour
    {
        public static InteractGlitchManager Current { get; }

        private const string HEX_CHARPOOL = "0123456789ABCDEF";

        private const string ERR_CHARPOOL = "$#/-01";

        internal bool Enabled { get; set; }

        internal bool CanInteract { get; set; }

        private StringBuilder _StrBuilder { get; } = new();

        internal GlitchMode Mode { get; set; }

        private Dictionary<IntPtr, LG_SecurityDoor> DoorLocks { get; } = new();

        private System.Random Random = new();

        private float _Timer;

        private void Update()
        {
            if (!Enabled || _Timer > Clock.Time || GuiManager.InteractionLayer == null || Mode == GlitchMode.None)
            {
                return;
            }

            switch (Mode)
            {
                case GlitchMode.Style1:
                    GuiManager.InteractionLayer.SetInteractPrompt(GetFormat1(),
                        CanInteract ? Text.Format(HOLD_TEXT_ID, InputMapper.GetBindingName(InputAction.Use)) : string.Empty,
                        //"Hold '" + InputMapper.GetBindingName(InputAction.Use) + "'", 
                        ePUIMessageStyle.Default);
                    GuiManager.InteractionLayer.InteractPromptVisible = true;
                    _Timer = Clock.Time + 0.05f;
                    break;

                case GlitchMode.Style2:
                    string format = GetFormat2(Text.Get(START_SECURITY_SCAN_SEQUENCE_TEXT_ID));
                    string format2 = GetFormat2(Text.Get(SCAN_UNKNOWN_TEXT_DB));
                    GuiManager.InteractionLayer.SetInteractPrompt(
                        $"{format}<color=red>{format2}</color>", 
                        CanInteract ? Text.Format(HOLD_TEXT_ID, InputMapper.GetBindingName(InputAction.Use)) : string.Empty, 
                        ePUIMessageStyle.Default);
                    //GuiManager.InteractionLayer.SetInteractPrompt(format + "<color=red>" + format2 + "</color>", "Hold '" + InputMapper.GetBindingName(InputAction.Use) + "'", ePUIMessageStyle.Default);
                    GuiManager.InteractionLayer.InteractPromptVisible = true;
                    _Timer = Clock.Time + 0.075f;
                    break;
            }
        }

        private string GetFormat1()
        {
            return string.Concat(new string[]
            {
                "<color=red>://Decryption E_RR at: [",
                GetRandomHex(),
                GetRandomHex(),
                "-",
                GetRandomHex(),
                GetRandomHex(),
                "-",
                GetRandomHex(),
                GetRandomHex(),
                "-",
                GetRandomHex(),
                GetRandomHex(),
                "]</color>"
            });
        }

        private string GetFormat2(string baseMessage)
        {
            _StrBuilder.Clear();
            foreach (char c in baseMessage)
            {
                if (Random.NextDouble() > 0.009999999776482582 || c == ':')
                {
                    _StrBuilder.Append(c);
                }
                else
                {
                    _StrBuilder.Append(ERR_CHARPOOL[Random.Next(0, ERR_CHARPOOL.Length)]);
                }
            }
            return _StrBuilder.ToString();
        }

        private string GetRandomHex()
        {
            return string.Format("{0}{1}", HEX_CHARPOOL[Random.Next(0, HEX_CHARPOOL.Length)], HEX_CHARPOOL[Random.Next(0, HEX_CHARPOOL.Length)]);
        }

        internal void RegisterDoorLocks(LG_SecurityDoor_Locks locks)
        {
            DoorLocks[locks.m_intCustomMessage.Pointer] = locks.m_door;
            DoorLocks[locks.m_intOpenDoor.Pointer] = locks.m_door;
        }

        public GlitchMode GetGlitchMode(Interact_Base interact)
        {
            if (!DoorLocks.TryGetValue(interact.Pointer, out var door)) return GlitchMode.None;
            var dim = door.Gate.DimensionIndex;
            var layer = door.LinksToLayerType;
            var localIndex = door.LinkedToZoneData.LocalIndex;

            var def = SecDoorIntTextOverrideManager.Current.GetDefinition(dim, layer, localIndex);
            return def?.GlitchMode ?? GlitchMode.None;
        }

        private void Clear()
        {
            DoorLocks.Clear();
        }

        private InteractGlitchManager()
        {
            START_SECURITY_SCAN_SEQUENCE_TEXT_ID = GameDataBlockBase<TextDataBlock>.GetBlock("InGame.InteractionPrompt.SecurityDoor.StartSecurityScanSequence")?.persistentID ?? 0;
            HOLD_TEXT_ID = GameDataBlockBase<TextDataBlock>.GetBlock("InGame.InteractionPrompt.Hold_X")?.persistentID ?? 0;
            SCAN_UNKNOWN_TEXT_DB = GameDataBlockBase<TextDataBlock>.GetBlock("InGame.InteractionPrompt.SecurityDoor.StartSecurityScanSequence_ScanUnknown")?.persistentID ?? 0;
            
            LevelAPI.OnBuildStart += Clear;
            LevelAPI.OnLevelCleanup += Clear;
        }

        static InteractGlitchManager()
        {
            ClassInjector.RegisterTypeInIl2Cpp<InteractGlitchManager>();

            GameObject gameObject = new GameObject("CONST_InteractGlitchManager");
            DontDestroyOnLoad(gameObject);
            Current = gameObject.AddComponent<InteractGlitchManager>();
        }

        private uint START_SECURITY_SCAN_SEQUENCE_TEXT_ID;

        private uint HOLD_TEXT_ID;

        private uint SCAN_UNKNOWN_TEXT_DB;
    }
}
