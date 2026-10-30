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
        public InventoryGrid Grid { get; }
        public float WeightLimit { get; }
        
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

        public InventoryContainer(int containerId, int width, int height, float weightLimit)
            : this(containerId, width, height, weightLimit, InventoryRules.Instance) { }
        
        public InventoryContainer(int containerId, int width, int height, float weightLimit, IInventoryRules rules)
        {
            ContainerId = containerId;
            Grid = new InventoryGrid(width, height);
            WeightLimit = weightLimit;
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

        //移除物品
        public bool Remove(InventoryItemPlacement placement) => Grid.Remove(placement);
        
        public bool Contains(InventoryItemPlacement placement)
        {
            return Grid.TryGetPlacementAt(placement.Position, out var found) && ReferenceEquals(found, placement);
        }
        
        
        
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
            target.Grid.Add(placement);
        }
    }
}