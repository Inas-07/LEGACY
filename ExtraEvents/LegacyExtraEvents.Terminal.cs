using GameData;
using LEGACY.Utils;
using Player;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static void ToggleEnableDisableTerminal(WardenObjectiveEventData e)
        {
            bool active = e.Enabled;

            var terminal = Helper.FindTerminal(e.DimensionIndex, e.Layer, e.LocalIndex, e.Count);
            if (terminal == null) return;

            terminal.OnProximityExit();
            Interact_ComputerTerminal componentInChildren = terminal.GetComponentInChildren<Interact_ComputerTerminal>(true);
            if (componentInChildren != null)
            {
                componentInChildren.enabled = active;
                componentInChildren.SetActive(active);
            }
            GUIX_VirtualSceneLink component = terminal.GetComponent<GUIX_VirtualSceneLink>();
            if (component != null && component.m_virtualScene != null)
            {
                GUIX_VirtualCamera virtualCamera = component.m_virtualScene.virtualCamera;
                virtualCamera.SetFovAndClip(virtualCamera.paramCamera.fieldOfView, active ? 0.3f : 0.0f, active ? 1000f : 0.0f);
            }

            if(terminal.m_text != null) terminal.m_text.enabled = active;
            
            if (!active)
            {
                PlayerAgent interactionSource = terminal.m_localInteractionSource;
                if (interactionSource != null && interactionSource.FPItemHolder.InTerminalTrigger)
                    terminal.ExitFPSView();
            }
        }
    }
}