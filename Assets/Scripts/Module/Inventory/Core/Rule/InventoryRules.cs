/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包规则类                      
 * │  类    名: InventoryRules.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Grid;
using Module.Inventory.Core.Rule.Type;
using UnityEngine;

namespace Module.Inventory.Core.Rule
{
    public static class InventoryRules
    {
        public static InventoryMoveResult TryAdd(InventoryItemInstance instance,
            InventoryContainer target, Vector2Int position,
            InventoryRotation rotation = InventoryRotation.None)
        {
            var placement = new InventoryItemPlacement(instance, position);
            placement.SetRotation(rotation);

            if (IsOverWeight(target, instance)) return InventoryMoveResult.WeightExceeded;

            InventoryMoveResult fit = CheckFit(target.Grid, placement, position, rotation, null);
            if (fit != InventoryMoveResult.Success) return fit;

            target.Grid.Add(placement);
            target.NotifyChanged();
            return InventoryMoveResult.Success;
        }

        public static InventoryMoveResult TryMove(InventoryItemPlacement placement, InventoryContainer source,
            InventoryContainer target, Vector2Int position, InventoryRotation rotation)
        {
            bool sameContainer = ReferenceEquals(source, target);

            if (sameContainer && placement.Position == position && placement.Rotation == rotation)
                return InventoryMoveResult.SameSpot;

            if (rotation != placement.Rotation && !placement.Instance.Config.CanRotate)
                return InventoryMoveResult.NotRotatable;

            if (!sameContainer && IsOverWeight(target, placement.Instance))
                return InventoryMoveResult.WeightExceeded;

            InventoryMoveResult fit = CheckFit(target.Grid, placement, position, rotation,
                sameContainer ? placement : null);
            if (fit != InventoryMoveResult.Success) return fit;

            source.Grid.Remove(placement);
            placement.SetPosition(position);
            placement.SetRotation(rotation);
            target.Grid.Add(placement);

            source.NotifyChanged();
            if (!sameContainer) target.NotifyChanged();

            return InventoryMoveResult.Success;
        }

        public static InventoryMoveResult TryRotate(InventoryItemPlacement placement, InventoryContainer container)
        {
            if (!placement.Instance.Config.CanRotate) return InventoryMoveResult.NotRotatable;

            InventoryRotation next = (InventoryRotation)(((int)placement.Rotation + 1) % 4);
            InventoryMoveResult fit = CheckFit(container.Grid, placement, placement.Position, next, placement);
            if (fit != InventoryMoveResult.Success) return fit;

            placement.SetRotation(next);
            container.Grid.Remove(placement);
            container.Grid.Add(placement);
            container.NotifyChanged();

            return InventoryMoveResult.Success;
        }

        public static bool TryRemove(InventoryItemPlacement placement, InventoryContainer container)
        {
            if (!container.Remove(placement)) return false;

            container.NotifyChanged();
            return true;
        }

        private static InventoryMoveResult CheckFit(InventoryGrid grid, InventoryItemPlacement placement,
            Vector2Int position, InventoryRotation rotation, InventoryItemPlacement ignore)
        {
            Vector2Int size = placement.SizeAt(rotation);
            if (!grid.IsAreaInside(position, size)) return InventoryMoveResult.OutOfBounds;
            if (!grid.IsAreaFree(position, size, ignore)) return InventoryMoveResult.Occupied;

            return InventoryMoveResult.Success;
        }

        private static bool IsOverWeight(InventoryContainer target, InventoryItemInstance instance)
        {
            return target.TotalWeight + instance.Weight > target.WeightLimit;
        }
    }
}