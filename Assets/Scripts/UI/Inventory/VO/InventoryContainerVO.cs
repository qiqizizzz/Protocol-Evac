/*
 * ┌──────────────────────────────────┐
 * │  描    述: 容器快照渲染载体                      
 * │  类    名: InventoryContainerVO.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;

namespace UI.Inventory
{
    public sealed class InventoryContainerVO
    {
        public static readonly InventoryContainerVO Empty =
            new InventoryContainerVO(0, 0, 0, 0, 0f, 0f, Array.Empty<InventoryItemVO>());
        
        public int ContainerId { get; }
        public int Width { get; }
        public int Height { get; }
        public int Version { get; }
        public float TotalWeight { get; }
        public float WeightLimit { get; }
        
        public IReadOnlyList<InventoryItemVO> Items { get; }
        
        public int CellCount => Width * Height;

        private readonly Dictionary<int, InventoryItemVO> m_itemIndex; //按实例号查物品载体

        public InventoryContainerVO(int containerId, int width, int height, int version, float totalWeight,
            float weightLimit, InventoryItemVO[] items)
        {
            ContainerId = containerId;
            Width = width;
            Height = height;
            Version = version;
            TotalWeight = totalWeight;
            WeightLimit = weightLimit;
            Items = items;
            m_itemIndex = new Dictionary<int, InventoryItemVO>(items.Length);
            
            for (int i = 0; i < items.Length; i++)
                m_itemIndex[items[i].InstanceId] = items[i];
        }
        
        public bool TryGetItem(int instanceId, out InventoryItemVO item) => m_itemIndex.TryGetValue(instanceId, out item);
        public bool Contains(int instanceId) => m_itemIndex.ContainsKey(instanceId);
    }
}