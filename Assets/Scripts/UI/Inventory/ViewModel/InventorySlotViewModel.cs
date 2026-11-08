/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包格子视图模型
 * │  类    名: InventorySlotViewModel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using Framework.QTower.MVVM.ViewModel;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using UnityEngine;

namespace UI.Inventory
{
    public sealed class InventorySlotViewModel : ViewModelBase
    {
        public Vector2Int Cell {get; }

        public IReadOnlyReactiveProperty<InventoryCellMark> Mark { get; }
        public IReadOnlyReactiveProperty<bool> IsSolid { get; }

        private readonly Action<Vector2Int> m_clickHandler;
        private readonly Action<Vector2Int> m_dropHandler;
        private readonly Action<Vector2Int, bool> m_hoverHandler;

        public InventorySlotViewModel(Vector2Int cell, IReadOnlyReactiveProperty<InventoryCellMark> mark,
            IReadOnlyReactiveProperty<bool> isSolid, Action<Vector2Int> clickHandler,
            Action<Vector2Int> dropHandler = null, Action<Vector2Int, bool> hoverHandler = null)
        {
            Cell = cell;
            Mark = mark;
            IsSolid = isSolid;
            m_clickHandler = clickHandler;
            m_dropHandler = dropHandler;
            m_hoverHandler = hoverHandler;
        }

        public void NotifyClicked() => m_clickHandler(Cell);

        /// <summary>
        /// 松手落点：本格与拖影居中偏移之和
        /// </summary>
        /// <param name="anchorOffset">锚点格相对本格的偏移</param>
        public void NotifyDropped(Vector2Int anchorOffset) => m_dropHandler?.Invoke(Cell + anchorOffset);

        /// <summary>
        /// 指针进出：本格与拖影居中偏移之和
        /// </summary>
        /// <param name="isEnter">True 为进入, False 为离开</param>
        /// <param name="anchorOffset">锚点格相对本格的偏移, 离开时传零</param>
        public void NotifyHovered(bool isEnter, Vector2Int anchorOffset) =>
            m_hoverHandler?.Invoke(Cell + anchorOffset, isEnter);
    }
}
