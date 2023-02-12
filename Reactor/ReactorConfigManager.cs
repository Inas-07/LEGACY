using GameData;
using LevelGeneration;
using Player;
using System;
using System.Collections;
using GTFO.API;
using SNetwork;
using LEGACY.Utilities;

namespace LEGACY.Reactor
{
    internal class ReactorConfigManager
    {
        public static readonly ReactorConfigManager Current;

        private LG_WardenObjective_Reactor[] reactors = new LG_WardenObjective_Reactor[3];

        public LG_WardenObjective_Reactor FindReactor(LG_LayerType layer)
        {
            if (reactors[(int)layer] != null) return reactors[(int)layer];

            LG_WardenObjective_Reactor reactor = null;
            foreach (var keyvalue in WardenObjectiveManager.Current.m_wardenObjectiveItem)
            {
                if (keyvalue.Key.Layer != layer)
                    continue;

                reactor = keyvalue.Value.TryCast<LG_WardenObjective_Reactor>();
                if (reactor == null)
                    continue;

                break;
            }

            reactors[(int)layer] = reactor;
            return reactor;
        }

        internal bool IsReactorStartup(LG_LayerType layer)
        {
            WardenObjectiveDataBlock data;
            if (!WardenObjectiveManager.Current.TryGetActiveWardenObjectiveData(layer, out data) || data == null) return false;

            return data.Type == eWardenObjectiveType.Reactor_Startup;
        }

        public LG_ComputerTerminal FindTerminalWithTerminalSerial(eDimensionIndex dim, LG_LayerType layer, eLocalZoneIndex localIndex, string itemKey)
        {
            LG_Zone zone = null;
            if (Builder.CurrentFloor.TryGetZoneByLocalIndex(dim, layer, localIndex, out zone) && zone != null)
            {
                foreach (var terminal in zone.TerminalsSpawnedInZone)
                {
                    if (terminal.ItemKey.Equals(itemKey, StringComparison.InvariantCultureIgnoreCase))
                        return terminal;
                }
            }

            return null;
        }

        internal void MoveVerifyLog(WardenObjectiveEventData e)
        {
            if (!IsReactorStartup(e.Layer))
            {
                Logger.Error($"ExtraEventsConfig: {e.Layer} is not ReactorStartup. MoveVerifyLog is invalid.");
                return;
            }

            LG_WardenObjective_Reactor reactor = FindReactor(e.Layer);

            if (reactor == null)
            {
                Logger.Error($"ExtraEventsConfig: Cannot find reactor in {e.Layer}.");
                return;
            }


            //reactor.m_currentWaveData.ver
        }


        private IEnumerator Handle(WardenObjectiveEventData eventToTrigger, float currentDuration)
        {
            WardenObjectiveEventData e = eventToTrigger;

            float delay = UnityEngine.Mathf.Max(e.Delay - currentDuration, 0f);
            if (delay > 0f)
            {
                yield return new UnityEngine.WaitForSeconds(delay);
            }

            WardenObjectiveManager.DisplayWardenIntel(e.Layer, e.WardenIntel);
            if (e.DialogueID > 0u)
            {
                PlayerDialogManager.WantToStartDialog(e.DialogueID, -1, false, false);
            }
            if (e.SoundID > 0u)
            {
                WardenObjectiveManager.Current.m_sound.Post(e.SoundID, true);
                var line = e.SoundSubtitle.ToString();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    GuiManager.PlayerLayer.ShowMultiLineSubtitle(line);
                }
            }
        }

        private void OnLevelCleanup()
        {
            for (int i = 0; i < reactors.Length; i++)
                reactors[i] = null;
        }

        static ReactorConfigManager()
        {
            Current = new();
            LevelAPI.OnLevelCleanup += Current.OnLevelCleanup;
        }
    }
}
