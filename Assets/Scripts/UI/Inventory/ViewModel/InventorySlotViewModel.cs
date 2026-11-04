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

        public void NotifyDropped() => m_dropHandler?.Invoke(Cell);

        public void NotifyHovered(bool isEnter) => m_hoverHandler?.Invoke(Cell, isEnter);
    }
}
