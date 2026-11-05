/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包容器实体类, 管理游戏运行时逻辑                      
 * │  类    名: InventoryContainer.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Grid;
using Module.Inventory.Core.Rule;
using Module.Inventory.Core.Rule.Type;
using UnityEngine;

namespace Module.Inventory.Core.Data
{
    public class InventoryContainer
    {
        public int ContainerId { get; }

        //容器显示名, 装配处从容器定义带进来
        public string DisplayName { get; }
        public InventoryGrid Grid { get; }
        public float WeightLimit { get; }

        //穿戴部位, 普通容器为 None
        public InventoryEquipSlot EquipSlot { get; }

        //是不是装备槽, 装备槽只收对应部位的装备且按单格收纳
        public bool IsEquipSlot => EquipSlot != InventoryEquipSlot.None;
        
        //每次成功变更后自增, 用于作联机时的冲突检测
        public int Version { get; private set; }
        
        public event Action<InventoryContainer> OnChanged;
        public IReadOnlyCollection<InventoryItemPlacement> Placements => Grid.Placements; //背包物品集合
        public int Count => Grid.Count;
        public IInventoryRules Rules => m_rules;
        
        private readonly IInventoryRules m_rules;

        public float TotalWeight
        {
            get
            {
                float total = 0f;
                foreach (var placement in Grid.Placements)
                {
                    total += placement.Instance.Weight;
                }
                return total;
            }
        }

        public InventoryContainer(int containerId, InventoryContainerSpec spec)
            : this(containerId, spec, InventoryRules.Instance) { }
        
        public InventoryContainer(int containerId, InventoryContainerSpec spec, IInventoryRules rules)
        {
            ContainerId = containerId;
            Grid = new InventoryGrid(spec.Width, spec.Height, spec.Mask);
            WeightLimit = spec.WeightLimit;
            DisplayName = spec.DisplayName;
            EquipSlot = spec.EquipSlot;
            m_rules = rules;
        }

        #region 主要方法
        //放入物品
        public InventoryItemPlacement TryAdd(InventoryItemInstance instance, Vector2Int position,
            InventoryRotation rotation = InventoryRotation.None)
        {
            var placement = new InventoryItemPlacement(instance, position, rotation);

            if (m_rules.CanAdd(this, instance, position, rotation) != InventoryMoveResult.Success)
                return null;

            placement.Footprint = ResolveFootprint(instance, rotation);
            Grid.Add(placement);
            Commit();
            return placement;
        }

        //移动物品
        public InventoryMoveResult TryMoveTo(InventoryContainer target, InventoryItemPlacement placement,
            Vector2Int position, InventoryRotation rotation)
        {
            InventoryMoveResult result = m_rules.CanMove(this, target, placement, position, rotation);
            if (result != InventoryMoveResult.Success) return result;

            ApplyMove(this, target, placement, position, rotation);

            Commit();
            if (!ReferenceEquals(this, target)) target.Commit();

            return InventoryMoveResult.Success;
        }

        //与目标容器指定位置上的物品交换, occupant 被顶回 moving 原位且保持自身朝向
        public InventoryMoveResult TrySwapTo(InventoryContainer target, InventoryItemPlacement moving,
            InventoryItemPlacement occupant, Vector2Int position, InventoryRotation rotation)
        {
            InventoryMoveResult result = m_rules.CanSwap(this, target, moving, occupant, position, rotation);
            if (result != InventoryMoveResult.Success) return result;

            Vector2Int originPosition = moving.Position;
            InventoryRotation originRotation = moving.Rotation;

            //先把两件物品双双摘出网格再各就各位, 否则新旧占位会在网格里互相压住
            Grid.Remove(moving);
            target.Grid.Remove(occupant);

            moving.SetPosition(position);
            moving.SetRotation(rotation);
            occupant.SetPosition(originPosition);

            //两件物品各奔新家, 落位前按新东家的口径重算足迹
            moving.Footprint = target.ResolveFootprint(moving.Instance, rotation);
            occupant.Footprint = ResolveFootprint(occupant.Instance, occupant.Rotation);

            Grid.Add(occupant);
            target.Grid.Add(moving);

            Commit();
            if (!ReferenceEquals(this, target)) target.Commit();

            return InventoryMoveResult.Success;
        }
        //旋转物品
        public InventoryMoveResult TryRotate(InventoryItemPlacement placement)
        {
            InventoryMoveResult result = m_rules.CanRotate(this, placement, out var nextRotation);
            if (result != InventoryMoveResult.Success) return result;

            ApplyMove(this, this, placement, placement.Position, nextRotation);
            Commit();
            return InventoryMoveResult.Success;
        }

        //移除物品
        public bool TryRemove(InventoryItemPlacement placement)
        {
            if (!Grid.Remove(placement)) return false;

            Commit();
            return true;
        }
        #endregion
        
        //尝试获取指定物品的放置信息
        public bool TryGetPlacement(InventoryItemInstance instance, out InventoryItemPlacement itemPlacement)
        {
            foreach (var item in Grid.Placements)
            {
                if (ReferenceEquals(item.Instance, instance))
                {
                    itemPlacement = item;
                    return true;
                }
            }
            
            itemPlacement = null;
            return false;
        }
        
        public bool Contains(InventoryItemPlacement placement) => Grid.Contains(placement);

        //这件物品在该容器里实际占下的格子范围
        public Vector2Int ResolveFootprint(InventoryItemInstance instance, InventoryRotation rotation)
            => IsEquipSlot ? Vector2Int.one : instance.Config.GetSizeAt(rotation);
        
        public override string ToString()
        {
            return $"[Container {ContainerId} {Grid} Weight: {TotalWeight}/{WeightLimit} v{Version}]";
        }

        private void Commit()
        {
            Version++;
            OnChanged?.Invoke(this);
        }
        
        private void ApplyMove(InventoryContainer source, InventoryContainer target,
            InventoryItemPlacement placement, Vector2Int position, InventoryRotation rotation)
        {
            source.Grid.Remove(placement);
            placement.SetPosition(position);
            placement.SetRotation(rotation);
            placement.Footprint = target.ResolveFootprint(placement.Instance, rotation);
            target.Grid.Add(placement);
        }
    }
}