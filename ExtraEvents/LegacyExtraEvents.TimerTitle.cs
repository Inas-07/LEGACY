using GameData;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static IEnumerator CountDown(WardenObjectiveEventData e)
        {
            var duration = e.Duration;
            GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(true, true);
            GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerTitle(e.CustomSubObjectiveHeader.ToString());
            GuiManager.PlayerLayer.m_objectiveTimer.SetTimerTextEnabled(true);

            UnityEngine.Color color;
            if (UnityEngine.ColorUtility.TryParseHtmlString(e.CustomSubObjective.ToString(), out color) == false)
            {
                color.r = color.g = color.b = 255.0f;
            }

            var time = 0.0f;
            while (time <= duration)
            {
                if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
                {
                    break;
                }

                GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerText(duration - time, duration, color);
                time += UnityEngine.Time.deltaTime;
                yield return null;
            }

            GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(false, true);
        }

        private static void SetTimerTitle(WardenObjectiveEventData e)
        {
            float duration = e.Duration;

            // countdown
            if (duration > 0.0) // no idea why this fked up
            {
                var coroutine = CoroutineManager.StartCoroutine(CountDown(e).WrapToIl2Cpp());
                WorldEventManager.m_worldEventEventCoroutines.Add(coroutine);
                return;
            }

            // ==== set title ====
            // disable title
            if (e.CustomSubObjectiveHeader.ToString().Length == 0)
            {
                GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(false);
            }
            // enable title
            else
            {
                GuiManager.PlayerLayer.m_objectiveTimer.SetTimerActive(true);
                GuiManager.PlayerLayer.m_objectiveTimer.UpdateTimerTitle(e.CustomSubObjectiveHeader.ToString());
                GuiManager.PlayerLayer.m_objectiveTimer.SetTimerTextEnabled(false);
            }
        }

    }
}