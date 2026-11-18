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
            new InventoryContainerVO(0, 0, 0, Array.Empty<bool>(), Array.Empty<int>(), 0, 0f, 0f,
                Array.Empty<InventoryItemVO>(), string.Empty);

        public int ContainerId { get; }

        public string DisplayName { get; }
        public int Width { get; }
        public int Height { get; }

        public IReadOnlyList<bool> Mask { get; }

        //逻辑区域表, 与掩码同下标, 空洞为 -1
        public IReadOnlyList<int> RegionIds { get; }

        public int Version { get; }
        public float TotalWeight { get; }
        public float WeightLimit { get; }

        public IReadOnlyList<InventoryItemVO> Items { get; }

        public int SlotCount => Width * Height;

        public int CellCount { get; }

        public int UsedCellCount { get; }

        private readonly Dictionary<int, InventoryItemVO> m_itemIndex; //按实例号查物品载体

        public InventoryContainerVO(int containerId, int width, int height, IReadOnlyList<bool> mask,
            IReadOnlyList<int> regionIds, int version, float totalWeight, float weightLimit,
            InventoryItemVO[] items, string displayName)
        {
            ContainerId = containerId;
            DisplayName = displayName;
            Width = width;
            Height = height;
            Mask = mask;
            RegionIds = regionIds;
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

        /// <summary>
        /// 解析指定格子某一侧与相邻格子的区域连接状态
        /// </summary>
        /// <param name="x">当前格列号</param>
        /// <param name="y">当前格行号</param>
        /// <param name="offsetX">相邻格列偏移</param>
        /// <param name="offsetY">相邻格行偏移</param>
        /// <param name="regionId">当前格所属区域号</param>
        public InventorySlotEdgeKind ResolveEdgeKind(int x, int y, int offsetX, int offsetY, int regionId)
            => ResolveEdgeKind(Width, Height, Mask, RegionIds, x, y, offsetX, offsetY, regionId);

        /// <summary>
        /// 按网格定义解析指定格某侧与相邻格子的区域连接状态
        /// </summary>
        /// <param name="width">网格列数</param>
        /// <param name="height">网格行数</param>
        /// <param name="mask">行主序实心掩码</param>
        /// <param name="regionIds">行主序区域表</param>
        /// <param name="x">当前格列号</param>
        /// <param name="y">当前格行号</param>
        /// <param name="offsetX">相邻格列偏移</param>
        /// <param name="offsetY">相邻格行偏移</param>
        /// <param name="regionId">当前格所属区域号</param>
        public static InventorySlotEdgeKind ResolveEdgeKind(int width, int height, IReadOnlyList<bool> mask,
            IReadOnlyList<int> regionIds, int x, int y, int offsetX, int offsetY, int regionId)
        {
            if (mask == null || regionIds == null) return InventorySlotEdgeKind.None;
            if (x < 0 || x >= width || y < 0 || y >= height) return InventorySlotEdgeKind.None;

            int index = y * width + x;
            if (index >= mask.Count || index >= regionIds.Count) return InventorySlotEdgeKind.None;
            if (!mask[index] || regionId < 0) return InventorySlotEdgeKind.None;

            int neighborX = x + offsetX;
            int neighborY = y + offsetY;
            if (neighborX < 0 || neighborX >= width || neighborY < 0 || neighborY >= height)
                return InventorySlotEdgeKind.Boundary;

            int neighborIndex = neighborY * width + neighborX;
            if (neighborIndex >= mask.Count || neighborIndex >= regionIds.Count)
                return InventorySlotEdgeKind.None;
            if (!mask[neighborIndex]) return InventorySlotEdgeKind.Boundary;
            if (regionIds[neighborIndex] != regionId) return InventorySlotEdgeKind.Boundary;

            return InventorySlotEdgeKind.Connected;
        }

        public bool TryGetItem(int instanceId, out InventoryItemVO item) => m_itemIndex.TryGetValue(instanceId, out item);
        public bool Contains(int instanceId) => m_itemIndex.ContainsKey(instanceId);
    }
}
