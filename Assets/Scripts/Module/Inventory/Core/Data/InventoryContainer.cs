/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包容器实体类, 管理游戏运行时逻辑                      
 * │  类    名: InventoryContainer.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Module.Inventory.Core.Grid;

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

        public InventoryContainer(int containerId, int Width, int Height, float weightLimit)
        {
            ContainerId = containerId;
            Grid = new InventoryGrid(Width, Height);
            WeightLimit = weightLimit;
        }

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
        
        #region 外部通知
        public void NotifyChanged()
        {
            Version++;
            OnChanged?.Invoke(this);
        }
        #endregion
    }
}