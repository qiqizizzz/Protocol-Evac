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
using Module.Inventory.Core.Rule.Extensions;
using Module.Inventory.Core.Rule.Type;
using UnityEngine;

namespace Module.Inventory.Core.Rule
{
    public class InventoryRules : IInventoryRules
    {
        // 四次旋转构成一周, 与 CanRotate 的朝向推进保持一致
        private const int ROTATION_COUNT = 4;

        //默认无状态单例
        public static readonly InventoryRules Instance = new InventoryRules();
        
        #region 主要方法
        public InventoryMoveResult CanAdd(InventoryContainer target, InventoryItemInstance instance, Vector2Int position,
            InventoryRotation rotation)
        {
            //装备槽只收对应部位的装备
            if (target.IsEquipSlot && instance.Config.EquipSlot != target.EquipSlot)
                return InventoryMoveResult.SlotMismatch;

            if (IsOverWeight(target, instance.Weight)) return InventoryMoveResult.WeightExceeded;

            Vector2Int size = target.ResolveFootprint(instance, rotation);
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

            //跨容器搬进装备槽同样要部位对得上
            if (!sameContainer && target.IsEquipSlot && !CanEquipInSlot(placement.Instance.Config.EquipSlot, target.EquipSlot))
                return InventoryMoveResult.SlotMismatch;

            //物品移动后目标容器重量超标
            if(!sameContainer && IsOverWeight(target, placement.Instance.Weight)) 
                return InventoryMoveResult.WeightExceeded;

            Vector2Int size = target.ResolveFootprint(placement.Instance, rotation);
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

            if (target.IsEquipSlot && !CanEquipInSlot(moving.Instance.Config.EquipSlot, target.EquipSlot))
                return InventoryMoveResult.SlotMismatch;
            if (source.IsEquipSlot && !CanEquipInSlot(occupant.Instance.Config.EquipSlot, source.EquipSlot))
                return InventoryMoveResult.SlotMismatch;

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
            if (sameContainer && IsAreaOverlap(position, target.ResolveFootprint(moving.Instance, rotation), moving.Position,
                    source.ResolveFootprint(occupant.Instance, occupant.Rotation)))
                return InventoryMoveResult.Occupied;

            //moving 落进 target 的新位置, 同容器要连自己一起忽略, 否则新旧占位会互相卡住
            InventoryMoveResult fitMoving = CheckFit(target.Grid, position, target.ResolveFootprint(moving.Instance, rotation),
                occupant, sameContainer ? moving : null);
            if (fitMoving != InventoryMoveResult.Success) return fitMoving;

            //occupant 保持自身朝向被顶回 moving 原位
            return CheckFit(source.Grid, moving.Position, source.ResolveFootprint(occupant.Instance, occupant.Rotation), moving,
                sameContainer ? occupant : null);
        }
        public InventoryMoveResult CanRotate(InventoryContainer container, InventoryItemPlacement placement,
            out InventoryRotation nextRotation)
        {
            nextRotation = placement.Rotation;
            
            if(!container.Contains(placement)) return InventoryMoveResult.SourceNotFound;

            //装备槽按单格收纳, 朝向没有意义
            if (container.IsEquipSlot) return InventoryMoveResult.NotRotatable;
            
            if(!placement.Instance.Config.CanRotate) return InventoryMoveResult.NotRotatable;

            nextRotation = (InventoryRotation)(((int)placement.Rotation + 1) % 4);
            
            return CanMove(container, container, placement, placement.Position, nextRotation);
        }

        // 同类可堆物品的落点判定：数量能并入占格那一堆时视为可放, 不要求腾出整块空间
        public bool CanMerge(InventoryContainer source, InventoryContainer target,
            InventoryItemPlacement moving, InventoryItemPlacement occupant)
        {
            if (!source.Contains(moving) || !target.Contains(occupant)) return false;
            if (ReferenceEquals(moving, occupant)) return false;

            // 装备槽按单格收纳整件装备, 不做堆叠合并
            if (target.IsEquipSlot) return false;

            InventoryItemInstance dragged = moving.Instance;
            InventoryItemInstance destination = occupant.Instance;
            if (!dragged.IsStackable || !destination.IsStackable) return false;
            if (dragged.ItemId != destination.ItemId) return false;

            int absorbed = Mathf.Min(destination.MaxStackCount - destination.StackCount, dragged.StackCount);
            if (absorbed <= 0) return false;

            // 跨容器并堆时目标容器只承接真正过去的那部分净重
            if (!ReferenceEquals(source, target) &&
                IsOverWeight(target, destination.Config.UnitWeight * absorbed)) return false;

            return true;
        }

        // 转移落点判定：先按指定朝向判定, 空间不足时依次尝试其余朝向
        public InventoryMoveResult ResolveTransfer(InventoryContainer source, InventoryContainer target,
            InventoryItemPlacement moving, Vector2Int cell, InventoryRotation preferredRotation,
            out InventoryRotation rotation,
            out InventoryItemPlacement occupant)
        {
            rotation = preferredRotation;
            InventoryMoveResult verdict = EvaluateTransfer(source, target, moving, cell, preferredRotation, out occupant);

            // 超重与部位不符与朝向无关, 保留原始判定结果
            if (!verdict.IsSpatialFailure() || !CanAutoRotate(target, moving)) return verdict;

            // 指定朝向优先, 命中即返回, 可原样放置时不改变朝向
            for (int step = 1; step < ROTATION_COUNT; step++)
            {
                InventoryRotation candidate = (InventoryRotation)(((int)preferredRotation + step) % ROTATION_COUNT);
                InventoryMoveResult candidateVerdict = EvaluateTransfer(source, target, moving, cell, candidate,
                    out InventoryItemPlacement candidateOccupant);

                if (!candidateVerdict.IsSuccessOrSame()) continue;

                rotation = candidate;
                occupant = candidateOccupant;
                return candidateVerdict;
            }

            return verdict;
        }
        #endregion
        
        // 单朝向落点判定：目标被占用时依次改判为与占格物品合并或交换
        private InventoryMoveResult EvaluateTransfer(InventoryContainer source, InventoryContainer target,
            InventoryItemPlacement moving, Vector2Int cell, InventoryRotation rotation, out InventoryItemPlacement occupant)
        {
            occupant = null;

            InventoryMoveResult verdict = CanMove(source, target, moving, cell, rotation);
            if (verdict != InventoryMoveResult.Occupied) return verdict;

            if (!target.Grid.TryGetPlacementAt(cell, out occupant)) return InventoryMoveResult.Occupied;

            // 落点上是同类可堆物品时优先并堆, 并堆不受大件压不进小堆的空间限制
            if (CanMerge(source, target, moving, occupant)) return InventoryMoveResult.Success;

            return CanSwap(source, target, moving, occupant, cell, rotation);
        }

        // 自动旋转前置条件：物品允许旋转, 且目标不是单格装备槽
        private static bool CanAutoRotate(InventoryContainer target, InventoryItemPlacement moving)
            => moving.Instance.Config.CanRotate && !target.IsEquipSlot;

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

        //允许主武器、副武器与手枪装备槽之间互换
        private static bool CanEquipInSlot(InventoryEquipSlot itemSlot, InventoryEquipSlot targetSlot)
        {
            if (itemSlot == targetSlot) return true;

            return IsWeaponSlot(itemSlot) && IsWeaponSlot(targetSlot);
        }

        //判断是否为可互换的武器装备槽
        private static bool IsWeaponSlot(InventoryEquipSlot slot)
        {
            return slot == InventoryEquipSlot.PrimaryWeapon ||
                   slot == InventoryEquipSlot.SecondaryWeapon ||
                   slot == InventoryEquipSlot.Pistol;
        }
    }
}
