/*
 * ┌──────────────────────────────────┐
 * │  描    述: 物品详情占格预览, 携带每格实心与四边连接状态
 * │  类    名: ItemFootprintPreviewVO.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;

namespace UI.Inventory
{
    public sealed class ItemFootprintPreviewVO
    {
        public static readonly ItemFootprintPreviewVO Empty = new ItemFootprintPreviewVO(
            false,
            0,
            0,
            Array.Empty<bool>(),
            Array.Empty<InventorySlotEdgeKind>(),
            Array.Empty<InventorySlotEdgeKind>(),
            Array.Empty<InventorySlotEdgeKind>(),
            Array.Empty<InventorySlotEdgeKind>());

        public bool IsContainerLayout { get; }
        public int ColumnCount { get; }
        public int RowCount { get; }
        public IReadOnlyList<bool> SolidCells { get; }
        public IReadOnlyList<InventorySlotEdgeKind> TopEdges { get; }
        public IReadOnlyList<InventorySlotEdgeKind> RightEdges { get; }
        public IReadOnlyList<InventorySlotEdgeKind> BottomEdges { get; }
        public IReadOnlyList<InventorySlotEdgeKind> LeftEdges { get; }

        /// <summary>
        /// 创建固定预览区域的物品占格与区域连接快照
        /// </summary>
        /// <param name="isContainerLayout">是否为容器自身布局预览</param>
        /// <param name="columnCount">预览有效列数</param>
        /// <param name="rowCount">预览有效行数</param>
        /// <param name="solidCells">每格是否实心</param>
        /// <param name="topEdges">每格上边连接状态</param>
        /// <param name="rightEdges">每格右边连接状态</param>
        /// <param name="bottomEdges">每格下边连接状态</param>
        /// <param name="leftEdges">每格左边连接状态</param>
        public ItemFootprintPreviewVO(bool isContainerLayout, int columnCount, int rowCount,
            IReadOnlyList<bool> solidCells,
            IReadOnlyList<InventorySlotEdgeKind> topEdges, IReadOnlyList<InventorySlotEdgeKind> rightEdges,
            IReadOnlyList<InventorySlotEdgeKind> bottomEdges, IReadOnlyList<InventorySlotEdgeKind> leftEdges)
        {
            IsContainerLayout = isContainerLayout;
            ColumnCount = columnCount;
            RowCount = rowCount;
            SolidCells = solidCells;
            TopEdges = topEdges;
            RightEdges = rightEdges;
            BottomEdges = bottomEdges;
            LeftEdges = leftEdges;
        }
    }
}
