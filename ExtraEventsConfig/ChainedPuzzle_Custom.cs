using GameData;
using System.Collections;
using ChainedPuzzles;
using Player;
using ScanPosOverride.Managers;
using LEGACY.Utils;

namespace LEGACY.ExtraEventsConfig
{
    internal static class ChainedPuzzle_Custom
    {
        internal static IEnumerator ActivateChainedPuzzle(WardenObjectiveEventData e, float currentDuration)
        {
            uint puzzleOverrideIndex = e.ChainPuzzle;

            CP_Bioscan_Core bioscanCore = PuzzleOverrideManager.Current.GetBioscanCore(puzzleOverrideIndex);
            CP_Cluster_Core clusterCore = PuzzleOverrideManager.Current.GetClusterCore(puzzleOverrideIndex);

            iChainedPuzzleOwner owner;

            if (bioscanCore != null)
                owner = PuzzleOverrideManager.Current.ChainedPuzzleInstanceOwner(bioscanCore);
            else if(clusterCore != null) 
                owner = clusterCore.m_owner;
            else
            {
                Logger.Error($"ActivateChainedPuzzle: Cannot find puzzle with puzzle override index {puzzleOverrideIndex}!");
                yield break;
            }
            
            ChainedPuzzleInstance CPInstance = owner.TryCast<ChainedPuzzleInstance>();
            if (CPInstance == null)
            {
                Logger.Error("ActivateChainedPuzzle: Cannot find ChainedPuzzleInstance!");
                yield break;
            }

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

            CPInstance.AttemptInteract(eChainedPuzzleInteraction.Activate);

            Logger.Debug($"ActivateChainedPuzzle: puzzle override index: {puzzleOverrideIndex}");
            Logger.Debug($"ChainedPuzzleZone: Dim {CPInstance.m_sourceArea.m_zone.DimensionIndex}, {CPInstance.m_sourceArea.m_zone.m_layer.m_type}, Zone {CPInstance.m_sourceArea.m_zone.Alias}");
            Logger.Debug($"ChainedPuzzle Alarm name: {CPInstance.Data.PublicAlarmName}");
        }
    
        internal static IEnumerator AddReqItem(WardenObjectiveEventData e, float currentDuration)
        {
            uint puzzleOverrideIndex = e.ChainPuzzle;
            int reqItemIndex = e.Count;

            CarryItemPickup_Core itemToAdd = PuzzleReqItemManager.Current.GetBigPickupItem(reqItemIndex);
            if (itemToAdd == null)
            {
                Logger.Error($"AddReqItem: Cannot find BigPickup Item with index {reqItemIndex}");
                yield break;
            }

            CP_Bioscan_Core bioscanCore = PuzzleOverrideManager.Current.GetBioscanCore(puzzleOverrideIndex);
            CP_Cluster_Core clusterCore = PuzzleOverrideManager.Current.GetClusterCore(puzzleOverrideIndex);

            if(bioscanCore != null)
            {
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

                if(e.Duration > 0.0 && e.Duration < 1.0)
                {
                    float addThreshold = e.Duration;
                    CP_PlayerScanner scanner = bioscanCore.m_playerScanner.TryCast<CP_PlayerScanner>();
                    if(scanner != null)
                    {
                        float CheckInterval = e.FogTransitionDuration > 0.0 ? e.FogTransitionDuration : 0.5f;
                        Logger.Debug($"AddReqItem: item would be added on scan progression: {addThreshold}, progression check interval: {CheckInterval} (seconds)");
                        while (scanner.m_scanProgression < e.Duration)
                        {
                            yield return new UnityEngine.WaitForSeconds(CheckInterval);
                        }
                    }
                    else
                    {
                        Logger.Error("AddReqItem: Failed to get scanner for the CP_Bioscan_Core");
                    }
                }
                bioscanCore.AddRequiredItems(new iWardenObjectiveItem[1] { new iWardenObjectiveItem(itemToAdd.Pointer) });
                Logger.Debug($"AddReqItem: puzzle override index: {puzzleOverrideIndex}");
                Logger.Debug($"Item name: {itemToAdd.ItemDataBlock.publicName}");
            }
            else if(clusterCore != null)
            {
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

                var item = new iWardenObjectiveItem[1] { new iWardenObjectiveItem(itemToAdd.Pointer) };
                foreach(var childCore in clusterCore.m_childCores)
                {
                    childCore.AddRequiredItems(item);
                }

                Logger.Debug($"AddReqItem: puzzle override index: {puzzleOverrideIndex}");
                Logger.Debug($"Item name: {itemToAdd.ItemDataBlock.publicName}");
            }
            else
            {
                Logger.Error($"AddReqItem: cannot find puzzle core with index {puzzleOverrideIndex}");
                yield break;
            }
        }
    
        internal static IEnumerator RemoveReqItem(WardenObjectiveEventData e, float currentDuration)
        {
            uint puzzleOverrideIndex = e.ChainPuzzle;
            int reqItemIndex = e.Count;

            CarryItemPickup_Core itemToRemove = PuzzleReqItemManager.Current.GetBigPickupItem(reqItemIndex);
            if (itemToRemove == null)
            {
                Logger.Error($"RemoveReqItem: Cannot find BigPickup Item with index {reqItemIndex}");
                yield break;
            }

            CP_Bioscan_Core bioscanCore = PuzzleOverrideManager.Current.GetBioscanCore(puzzleOverrideIndex);
            CP_Cluster_Core clusterCore = PuzzleOverrideManager.Current.GetClusterCore(puzzleOverrideIndex);

            if (bioscanCore != null)
            {
                // execute event
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

                if (e.Duration > 0.0 && e.Duration < 1.0)
                {
                    float removeThreshold = e.Duration;
                    CP_PlayerScanner scanner = bioscanCore.m_playerScanner.TryCast<CP_PlayerScanner>();
                    if (scanner != null)
                    {
                        float CheckInterval = e.FogTransitionDuration > 0.0 ? e.FogTransitionDuration : 0.5f;
                        Logger.Debug($"RemoveReqItem: item would be added on scan progression: {removeThreshold}, progression check interval: {CheckInterval} (seconds)");
                        while (scanner.m_scanProgression < e.Duration)
                        {
                            yield return new UnityEngine.WaitForSeconds(CheckInterval);
                        }
                    }
                    else
                    {
                        Logger.Error("RemoveReqItem: Failed to get scanner for the CP_Bioscan_Core");
                    }
                }

                bioscanCore.RemoveRequiredItems(new iWardenObjectiveItem[1] { new iWardenObjectiveItem(itemToRemove.Pointer) });
                Logger.Debug($"RemoveReqItem: puzzle override index: {puzzleOverrideIndex}");
                Logger.Debug($"Removed Item name: {itemToRemove.ItemDataBlock.publicName}");
            }
            else if (clusterCore != null)
            {
                // execute event
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

                var item = new iWardenObjectiveItem[1] { new iWardenObjectiveItem(itemToRemove.Pointer) };
                foreach (var childCore in clusterCore.m_childCores)
                {
                    childCore.RemoveRequiredItems(item);
                }

                Logger.Debug($"RemoveReqItem: puzzle override index: {puzzleOverrideIndex}");
                Logger.Debug($"Removed Item name: {itemToRemove.ItemDataBlock.publicName}");
            }
            else
            {
                Logger.Error($"RemoveReqItem: cannot find puzzle core with index {puzzleOverrideIndex}");
                yield break;
            }
        }
    }
}
