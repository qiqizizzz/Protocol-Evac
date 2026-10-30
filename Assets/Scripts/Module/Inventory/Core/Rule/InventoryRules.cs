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
            InventoryItemPlacement ignorePlacement)
        {
            //检查是否在网格范围内
            if (!grid.IsAreaInside(position, size)) return InventoryMoveResult.OutOfBounds;
            //检查是否重叠
            if(!grid.IsAreaFree(position, size, ignorePlacement)) return InventoryMoveResult.Occupied;

            return InventoryMoveResult.Success;
        }
        
        //超重检测
        private bool IsOverWeight(InventoryContainer target, float addWeight)
        {
            return target.TotalWeight + addWeight > target.WeightLimit;
        }
    }
}