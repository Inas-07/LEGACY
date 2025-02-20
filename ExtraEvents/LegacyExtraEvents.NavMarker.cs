﻿using GameData;
using UnityEngine;
using LEGACY.Utils;
using LEGACY.LegacyOverride;
using LevelGeneration;
using ScanPosOverride.Managers;
using Player;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static void SetNavMarker(WardenObjectiveEventData e)
        {
            string markerName = e.WorldEventObjectFilter;
            bool enabled = e.Enabled;
            float scale = e.FogTransitionDuration >= 0f ? e.FogTransitionDuration : 3.2f;
            int objectType = e.SustainedEventSlotIndex;

            if (enabled)
            {
                switch (objectType)
                {
                    case 0: // terminal
                        var terminal = Helper.FindTerminal(e.DimensionIndex, e.Layer, e.LocalIndex, e.Count);
                        if (terminal == null)
                        {
                            LegacyLogger.Error($"SetNavMarker: trying to set nav marker on terminal with {(e.DimensionIndex, e.Layer, e.LocalIndex, e.Count)} but the terminal is not found");
                            return;
                        }

                        NavMarkerManager.Current.EnableMarkerAt(markerName, terminal.gameObject, scale);
                        break;

                    case 1: // big pickup
                        var big_pickup = PuzzleReqItemManager.Current.GetBigPickupItem(e.Count);
                        if (big_pickup == null)
                        {
                            LegacyLogger.Error($"SetNavMarker: trying to set nav marker on bigpick with instance index {e.Count} but the item is not found");
                            return;
                        }

                        var prev_marker = NavMarkerManager.Current.GetMarkerVisuals(markerName);
                        NavMarkerManager.Current.EnableMarkerAt(markerName, big_pickup.gameObject, scale);

                        var cur_marker = NavMarkerManager.Current.GetMarkerVisuals(markerName);
                        if (prev_marker.markerVisual == null && cur_marker.markerVisual != null) // first call on this marker name, do some setup
                        {
                            var sync = big_pickup.m_sync.Cast<LG_PickupItem_Sync>();
                            sync.OnSyncStateChange += new System.Action<ePickupItemStatus, pPickupPlacement, PlayerAgent, bool>((_, _, _, isRecall) =>
                            {
                                if (isRecall || big_pickup.CanWarp)
                                {
                                    NavMarkerManager.Current.DisableMakrer(markerName);
                                    return;
                                }
                            });
                        }

                        break;

                    case 2: // sec door 
                        if (!Builder.CurrentFloor.TryGetZoneByLocalIndex(e.DimensionIndex, e.Layer, e.LocalIndex, out var zone) || zone == null)
                        {
                            LegacyLogger.Error($"SetNavMarker: cannot find zone {(e.DimensionIndex, e.Layer, e.LocalIndex)}");
                            return;
                        }

                        var door = zone.m_sourceGate.SpawnedDoor.Cast<LG_SecurityDoor>();
                        NavMarkerManager.Current.EnableMarkerAt(markerName, door.gameObject, scale);

                        break;

                    case -1: // arbitrary position
                        NavMarkerManager.Current.EnableArbitraryMarkerAt(markerName, e.Position);
                        break;

                    default:
                        LegacyLogger.Error($"Find Object of Type '{objectType}' is not implemented");
                        break;
                }

            }
            else
            {
                NavMarkerManager.Current.DisableMakrer(markerName);
            }
        }
    }
}