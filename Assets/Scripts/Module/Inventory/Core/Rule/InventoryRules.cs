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
    public class InventoryRules : IInventoryRules
    {
        //默认无状态单例
        public static readonly InventoryRules Instance = new InventoryRules();
        
        #region 主要方法
        public InventoryMoveResult CanAdd(InventoryContainer target, InventoryItemInstance instance, Vector2Int position,
            InventoryRotation rotation)
        {
            if (IsOverWeight(target, instance.Weight)) return InventoryMoveResult.WeightExceeded;

            Vector2Int size = instance.Config.GetSizeAt(rotation);
            return CheckFit(target.Grid, position, size, null);
        }

        public InventoryMoveResult CanMove(InventoryContainer source, InventoryContainer target, InventoryItemPlacement placement,
            Vector2Int position, InventoryRotation rotation)
        {
            //判断物品是否在源容器中(不在则可能是网络相关错误)
            if (!source.Contains(placement)) return InventoryMoveResult.SourceNotFound;
            
            bool sameContainer = ReferenceEquals(source, target);
            
            //物体未移动
            if(sameContainer && placement.Position == position && placement.Rotation == rotation)
                return InventoryMoveResult.SameSpot;

            //物品禁止旋转
            if (rotation != placement.Rotation && !placement.Instance.Config.CanRotate)
                return InventoryMoveResult.NotRotatable;
            
            //物品移动后目标容器重量超标
            if(!sameContainer && IsOverWeight(target, placement.Instance.Weight)) 
                return InventoryMoveResult.WeightExceeded;

            Vector2Int size = placement.SizeAt(rotation);
            return CheckFit(target.Grid, position, size, sameContainer ? placement : null);
        }

        public InventoryMoveResult CanSwap(InventoryContainer source, InventoryContainer target,
            InventoryItemPlacement moving, InventoryItemPlacement occupant, Vector2Int position,
            InventoryRotation rotation)
        {
            //判断两件物品是否真在各自容器里(不在则可能是网络相关错误)
            if (!source.Contains(moving)) return InventoryMoveResult.SourceNotFound;
            if (!target.Contains(occupant)) return InventoryMoveResult.SourceNotFound;

            //自己和自己交换视为未移动
            if (ReferenceEquals(moving, occupant)) return InventoryMoveResult.SameSpot;

            bool sameContainer = ReferenceEquals(source, target);

            //物体未移动
            if (sameContainer && position == moving.Position && rotation == moving.Rotation)
                return InventoryMoveResult.SameSpot;

            //物品禁止旋转
            if (rotation != moving.Rotation && !moving.Instance.Config.CanRotate)
                return InventoryMoveResult.NotRotatable;

            //跨容器时双方净重互抵, 任一侧超标都算失败
            if (!sameContainer)
            {
                float delta = moving.Instance.Weight - occupant.Instance.Weight;
                if (IsOverWeight(target, delta)) return InventoryMoveResult.WeightExceeded;
                if (IsOverWeight(source, -delta)) return InventoryMoveResult.WeightExceeded;
            }

            //同容器交换, 两件物品的新足迹仍在同一张网格上, 一旦互相压住就换不动
            if (sameContainer && IsAreaOverlap(position, moving.SizeAt(rotation), moving.Position, occupant.Size))
                return InventoryMoveResult.Occupied;

            //moving 落进 target 的新位置, 同容器要连自己一起忽略, 否则新旧占位会互相卡住
            InventoryMoveResult fitMoving = CheckFit(target.Grid, position, moving.SizeAt(rotation),
                occupant, sameContainer ? moving : null);
            if (fitMoving != InventoryMoveResult.Success) return fitMoving;

            //occupant 保持自身朝向被顶回 moving 原位
            return CheckFit(source.Grid, moving.Position, occupant.Size, moving, sameContainer ? occupant : null);
        }
        public InventoryMoveResult CanRotate(InventoryContainer container, InventoryItemPlacement placement,
            out InventoryRotation nextRotation)
        {
            nextRotation = placement.Rotation;
            
            if(!container.Contains(placement)) return InventoryMoveResult.SourceNotFound;
            
            if(!placement.Instance.Config.CanRotate) return InventoryMoveResult.NotRotatable;

            nextRotation = (InventoryRotation)(((int)placement.Rotation + 1) % 4);
            
            return CanMove(container, container, placement, placement.Position, nextRotation);
        }
        #endregion
        
        //空间拟合检测
        private InventoryMoveResult CheckFit(InventoryGrid grid, Vector2Int position, Vector2Int size,
            InventoryItemPlacement ignorePlacement, InventoryItemPlacement ignorePlacement2 = null)
        {
            //检查是否在网格范围内
            if (!grid.IsAreaInside(position, size)) return InventoryMoveResult.OutOfBounds;
            //检查是否重叠
            if (!grid.IsAreaFree(position, size, ignorePlacement, ignorePlacement2)) return InventoryMoveResult.Occupied;

            return InventoryMoveResult.Success;
        }
        
        //两块区域是否互相压住, 按 RectInt 的排他边语义判定, 跨容器不共网格故无需比对
        private static bool IsAreaOverlap(Vector2Int positionA, Vector2Int sizeA, Vector2Int positionB, Vector2Int sizeB)
            => new RectInt(positionA.x, positionA.y, sizeA.x, sizeA.y).Overlaps(
                new RectInt(positionB.x, positionB.y, sizeB.x, sizeB.y));

        //超重检测
        private bool IsOverWeight(InventoryContainer target, float addWeight)
        {
            return target.TotalWeight + addWeight > target.WeightLimit;
        }
    }
}