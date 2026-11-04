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
            new InventoryContainerVO(0, 0, 0, Array.Empty<bool>(), 0, 0f, 0f, Array.Empty<InventoryItemVO>(),
                string.Empty);

        public int ContainerId { get; }

        public string DisplayName { get; }
        public int Width { get; }
        public int Height { get; }

        public IReadOnlyList<bool> Mask { get; }

        public int Version { get; }
        public float TotalWeight { get; }
        public float WeightLimit { get; }

        public IReadOnlyList<InventoryItemVO> Items { get; }

        public int SlotCount => Width * Height;

        public int CellCount { get; }

        public int UsedCellCount { get; }

        private readonly Dictionary<int, InventoryItemVO> m_itemIndex; //按实例号查物品载体

        public InventoryContainerVO(int containerId, int width, int height, IReadOnlyList<bool> mask, int version,
            float totalWeight, float weightLimit, InventoryItemVO[] items, string displayName)
        {
            ContainerId = containerId;
            DisplayName = displayName;
            Width = width;
            Height = height;
            Mask = mask;
            Version = version;
            TotalWeight = totalWeight;
            WeightLimit = weightLimit;
            Items = items;
            m_itemIndex = new Dictionary<int, InventoryItemVO>(items.Length);

            for (int i = 0; i < mask.Count; i++)
                if (mask[i]) CellCount++;

            for (int i = 0; i < items.Length; i++)
            {
                m_itemIndex[items[i].InstanceId] = items[i];
                UsedCellCount += items[i].CellCount;
            }
        }
        
        public bool TryGetItem(int instanceId, out InventoryItemVO item) => m_itemIndex.TryGetValue(instanceId, out item);
        public bool Contains(int instanceId) => m_itemIndex.ContainsKey(instanceId);
    }
}