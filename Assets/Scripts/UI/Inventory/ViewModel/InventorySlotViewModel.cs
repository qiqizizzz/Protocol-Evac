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
        private readonly ReactiveProperty<bool> m_isSelected;
        private readonly ReactiveProperty<bool> m_isBlocked;

        private readonly Action<Vector2Int> m_requestHandler;
        
        public Vector2Int Cell {get; }
        public IReadOnlyReactiveProperty<bool>  IsSelected => m_isSelected;
        public IReadOnlyReactiveProperty<bool>  IsBlocked => m_isBlocked;

        public InventorySlotViewModel(Vector2Int cell, Action<Vector2Int> requestHandler)
        {
            Cell = cell;
            m_requestHandler = requestHandler;
            
            m_isSelected = CreateProperty(false);
            m_isBlocked = CreateProperty(false);
        }

        public void RequestSelect() => m_requestHandler?.Invoke(Cell);
        
        public void SetSelected(bool selected) => m_isSelected.TrySet(selected);
        public void SetBlocked(bool blocked) => m_isBlocked.TrySet(blocked);
    }
}