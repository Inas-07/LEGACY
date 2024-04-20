using GameData;
using System.Collections.Generic;
using LEGACY.LegacyOverride.SecDoorIntText;
using LEGACY.Utils;
using LevelGeneration;
using Player;
using SNetwork;
using UnityEngine;
using System;

namespace LEGACY.ExtraEvents
{
    internal static partial class LegacyExtraEvents
    {
        private static List<Vector3> lookDirs = new() { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

        private static void WarpItem(ItemInLevel item, eDimensionIndex warpToDim, Vector3 warpToPosition, Predicate<ItemInLevel> predicate = null)
        {
            if (!SNet.IsMaster || GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

            if (item != null && item.internalSync.GetCurrentState().placement.droppedOnFloor && item.CanWarp)
            {
                if (predicate == null || predicate.Invoke(item))
                {
                    var targetNodeCluster = Helper.GetNodeFromDimensionPosition(warpToDim, warpToPosition);
                    if (targetNodeCluster != null)
                    {
                        item.GetSyncComponent().AttemptPickupInteraction(
                            ePickupItemInteractionType.Place,
                            null,
                            item.pItemData.custom,
                            warpToPosition,
                            Quaternion.identity,
                            targetNodeCluster.CourseNode,
                            true,
                            true);
                    }
                    else
                    {
                        LegacyLogger.Error($"WarpTeam: cannot find course node for item to warp");
                    }
                }
                else
                {
                    LegacyLogger.Debug($"WarpItem: {item.PublicName} failed to warp, because it doesn't meet the given predicate");
                }
            }
        }

        private static void WarpItemsInZone(WardenObjectiveEventData e)
        {
            if (!SNet.IsMaster || GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

            PlayerAgent localPlayer = PlayerManager.GetLocalPlayerAgent();
            if (localPlayer == null)
            {
                LegacyLogger.Error($"WarpItemsInZone: master - LocalPlayerAgent is null???");
                return;
            }

            var dim = e.DimensionIndex;
            var layer = e.Layer;
            var localIndex = e.LocalIndex;
            string worldEventObjectFilter = e.WorldEventObjectFilter;

            //if (e.DimensionIndex != localPlayer.DimensionIndex)
            //{
            //    LegacyLogger.Error($"WarpItemsInZone: this event is only for warping in the same dimension");
            //    return;
            //}

            var warpLocations = DimensionWarpPositionManager.Current.GetWarpPositions(e.DimensionIndex, worldEventObjectFilter);

            if (warpLocations.Count == 0)
            {
                LegacyLogger.Error($"WarpItemsInZone: no warp position found");
                return;
            }

            int itemPositionIdx = 0;
            foreach (var warpable in Dimension.WarpableObjects)
            {
                var itemInLevel = warpable.TryCast<ItemInLevel>();
                if (itemInLevel != null)
                {
                    var itemPosition = warpLocations[itemPositionIdx].Position.ToVector3();
                    WarpItem(itemInLevel, e.DimensionIndex, itemPosition, item => {
                        var prevNode = item.CourseNode;
                        return prevNode.m_dimension.DimensionIndex == dim && prevNode.LayerType == layer && prevNode.m_zone.LocalIndex == localIndex;
                    } );
                    itemPositionIdx = (itemPositionIdx + 1) % warpLocations.Count;
                }
            }
        }

        private static void WarpAlivePlayersAndItemsInRange(WardenObjectiveEventData e)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

            PlayerAgent localPlayer = PlayerManager.GetLocalPlayerAgent();
            if (localPlayer == null)
            {
                LegacyLogger.Error($"WarpAlivePlayersAndItemsInRange: LocalPlayerAgent is null");
                return;
            }

            //if (e.DimensionIndex != localPlayer.DimensionIndex)
            //{
            //    LegacyLogger.Error($"WarpAlivePlayersAndItemsInRange: this event is only for warping in the same dimension");
            //    return;
            //}

            var rangeOrigin = e.Position;
            float range = e.FogTransitionDuration;

            string worldEventObjectFilter = e.WorldEventObjectFilter;
            var warpLocations = DimensionWarpPositionManager.Current.GetWarpPositions(e.DimensionIndex, worldEventObjectFilter);
            if (warpLocations.Count == 0)
            {
                LegacyLogger.Error($"WarpAlivePlayersInRange: no warp locations found");
                return;
            }

            int positionIndex = localPlayer.PlayerSlotIndex % warpLocations.Count;
            var warpPosition = warpLocations[positionIndex].Position.ToVector3();
            int lookDirIndex = warpLocations[positionIndex].LookDir % lookDirs.Count;
            var lookDir = lookDirs[lookDirIndex];

            // warp warpable items within range
            int itemPositionIdx = 0;
            List<SentryGunInstance> sentryGunToWarp = new();
            foreach (var warpable in Dimension.WarpableObjects)
            {
                var sentryGun = warpable.TryCast<SentryGunInstance>();
                if (sentryGun != null)
                {
                    if (sentryGun.LocallyPlaced
                        && sentryGun.Owner.Alive && (rangeOrigin - sentryGun.Owner.Position).magnitude < range // owner is gonna warp
                        && (rangeOrigin - sentryGun.transform.position).magnitude < range) // sentry is in the warp range
                    {
                        sentryGunToWarp.Add(sentryGun);
                    }
                    continue;
                }

                if (SNet.IsMaster)
                {
                    var itemInLevel = warpable.TryCast<ItemInLevel>();
                    if (itemInLevel != null)
                    {
                        var itemPosition = warpLocations[itemPositionIdx].Position.ToVector3();
                        WarpItem(itemInLevel, e.DimensionIndex, itemPosition, item => (item.transform.position - rangeOrigin).magnitude < range);
                        itemPositionIdx = (itemPositionIdx + 1) % warpLocations.Count;

                        continue;
                    }
                }
            }
            sentryGunToWarp.ForEach(sentryGun => sentryGun.m_sync.WantItemAction(sentryGun.Owner, SyncedItemAction_New.PickUp));

            if (localPlayer.Alive && (rangeOrigin - localPlayer.Position).magnitude < range)
            {
                // warp player
                if (!localPlayer.TryWarpTo(e.DimensionIndex, warpPosition, lookDir, true))
                {
                    LegacyLogger.Error($"WarpAlivePlayersInRange: TryWarpTo failed, Position: {warpPosition}");
                }
            }
        }

        private static void WarpTeam(WardenObjectiveEventData e)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

            PlayerAgent localPlayer = PlayerManager.GetLocalPlayerAgent();
            if (localPlayer == null)
            {
                LegacyLogger.Error($"WarpTeam: LocalPlayerAgent is null");
                return;
            }

            //if (e.DimensionIndex != localPlayer.DimensionIndex)
            //{
            //    LegacyLogger.Error($"WarpTeam: this event is only for warping in the same dimension");
            //    return;
            //}

            string worldEventObjectFilter = e.WorldEventObjectFilter;
            var warpLocations = DimensionWarpPositionManager.Current.GetWarpPositions(e.DimensionIndex, worldEventObjectFilter);

            if (warpLocations.Count == 0)
            {
                LegacyLogger.Error($"WarpTeam: no warp position found");
                return; 
            }

            int positionIndex = localPlayer.PlayerSlotIndex % warpLocations.Count;
            var warpPosition = warpLocations[positionIndex].Position.ToVector3();
            int lookDirIndex = warpLocations[positionIndex].LookDir % lookDirs.Count;
            var lookDir = lookDirs[lookDirIndex];
            bool dontWarpWarpable = e.ClearDimension;

            if (!dontWarpWarpable)
            {
                int itemPositionIdx = 0;
                List<SentryGunInstance> sentryGunToWarp = new();

                foreach (var warpable in Dimension.WarpableObjects)
                {
                    var sentryGun = warpable.TryCast<SentryGunInstance>();
                    if (sentryGun != null)
                    {
                        if (sentryGun.LocallyPlaced)
                        {
                            sentryGunToWarp.Add(sentryGun);
                        }
                        continue;
                    }

                    if (SNet.IsMaster)
                    {
                        var itemInLevel = warpable.TryCast<ItemInLevel>();
                        if (itemInLevel != null)
                        {
                            var itemPosition = warpLocations[itemPositionIdx].Position.ToVector3();
                            WarpItem(itemInLevel, e.DimensionIndex, itemPosition);
                            itemPositionIdx = (itemPositionIdx + 1) % warpLocations.Count;
                            continue;
                        }
                    }
                }
                sentryGunToWarp.ForEach(sentryGun => sentryGun.m_sync.WantItemAction(sentryGun.Owner, SyncedItemAction_New.PickUp));
            }

            if (!localPlayer.TryWarpTo(e.DimensionIndex, warpPosition, lookDir, true))
            {
                LegacyLogger.Error($"WarpTeam: TryWarpTo failed. Position: {warpPosition}, playerSlotIndex: {localPlayer.PlayerSlotIndex}, warpLocationIndex: {positionIndex}");
            }
        }
    }
}