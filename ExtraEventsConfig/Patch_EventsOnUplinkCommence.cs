using HarmonyLib;
using LevelGeneration;
using GameData;
using LEGACY.Utils;


namespace LEGACY.ExtraEventsConfig
{
    [HarmonyPatch]
    internal class Patch_EventsOnUplinkCommence
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TerminalUplinkPuzzle), nameof(TerminalUplinkPuzzle.OnStartSequence))]
        private static void Post_OnStartSequence(TerminalUplinkPuzzle __instance)
        {
            var terminal = __instance.m_terminal;
            LG_LayerType layer = terminal.SpawnNode.LayerType;

            WardenObjectiveDataBlock db = null;
            WardenObjectiveManager.Current.TryGetActiveWardenObjectiveData(layer, out db);

            if (db == null || db.Type != eWardenObjectiveType.TerminalUplink && db.Type != eWardenObjectiveType.CorruptedTerminalUplink)
                return;

            // 当前已经完成了多少个uplink，就从第几个event break开始执行事件
            int current_step = WardenObjectiveManager.Current.ObjectiveItemsSolvedCount(layer);

            var eventsOnActivate = db.EventsOnActivate;

            int start_index = 0;
            while (start_index < eventsOnActivate.Count && current_step > 0)
            {
                if (eventsOnActivate[start_index] == null)
                {
                    LegacyLogger.Error("There's a null eventsOnActivate");
                    return;
                }

                if (eventsOnActivate[start_index].Type == eWardenObjectiveEventType.EventBreak)
                    current_step--;

                start_index++;
            }

            WardenObjectiveManager.CheckAndExecuteEventsWithBreaks(layer, eventsOnActivate, eWardenObjectiveEventTrigger.None, start_index, true);
        }
    }
}
