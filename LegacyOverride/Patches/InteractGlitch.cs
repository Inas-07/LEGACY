using HarmonyLib;
using LEGACY.LegacyOverride.SecDoorIntText;
using LevelGeneration;

namespace LEGACY.LegacyOverride.Patches
{
    [HarmonyPatch]
    internal class InteractGlitch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Interact_Timed), nameof(Interact_Timed.OnSelectedChange))]
        private static void Post_Interact_Timed_OnSelectedChange(Interact_Timed __instance, bool selected)
        {
            Handle(__instance, selected, true);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Interact_MessageOnScreen), nameof(Interact_MessageOnScreen.OnSelectedChange))]
        private static void Post_Interact_MessageOnScreen_OnSelectedChange(Interact_MessageOnScreen __instance, bool selected)
        {
            Handle(__instance, selected, false);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LG_SecurityDoor_Locks), nameof(LG_SecurityDoor_Locks.Setup), new System.Type[] { typeof(LG_SecurityDoor) })]
        private static void Post_LG_SecurityDoor_Locks_Setup(LG_SecurityDoor_Locks __instance, LG_SecurityDoor door)
        {
            InteractGlitchManager.Current.RegisterDoorLocks(__instance);
        }

        private static void Handle(Interact_Base interact, bool selected, bool canInteract)
        {
            if (selected)
            {
                if (interact == null)
                {
                    return;
                }

                var mode = InteractGlitchManager.Current.GetGlitchMode(interact);

                switch (mode)
                {
                    case GlitchMode.Style1:
                        InteractGlitchManager.Current.Enabled = true;
                        InteractGlitchManager.Current.CanInteract = canInteract;
                        InteractGlitchManager.Current.Mode = GlitchMode.Style1;
                        GuiManager.InteractionLayer.InteractPromptVisible = false;
                        break; 
                    case GlitchMode.Style2:
                        InteractGlitchManager.Current.Enabled = true;
                        InteractGlitchManager.Current.CanInteract = canInteract;
                        InteractGlitchManager.Current.Mode = GlitchMode.Style2;
                        GuiManager.InteractionLayer.InteractPromptVisible = false;
                        break;

                    default: return;
                }
            }
            else
            {
                InteractGlitchManager.Current.Enabled = false;
            }
        }
    }
}
