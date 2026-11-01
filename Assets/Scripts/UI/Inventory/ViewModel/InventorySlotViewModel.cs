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
        public IReadOnlyReactiveProperty<bool>  IsSelected { get; }
        
        private readonly Action<Vector2Int> m_clickHandler;
        
        

        public InventorySlotViewModel(Vector2Int cell, IReadOnlyReactiveProperty<bool> isSelected,
            Action<Vector2Int> clickHandler)
        {
            Cell = cell;
            IsSelected = isSelected;
            m_clickHandler = clickHandler;
        }

        public void NotifyClicked() => m_clickHandler(Cell);
    }
}