/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包格子视图                      
 * │  类    名: UIInventorySlot.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Inventory.View
{
    public sealed class UIInventorySlot : ViewBase<InventorySlotViewModel>, IPointerClickHandler
    {
        private static readonly Color m_normalColor = new Color(1f, 1f, 1f, 0.16f);
        private static readonly Color m_selectedColor = new Color(0.31f, 0.76f, 1f, 0.55f);
        
        private Image m_background;
        
        protected override InventorySlotViewModel CreateViewModel() => null;

        protected override void OnAwake()
        {
            m_background = GetComponent<Image>();
        }

        protected override void BindViewModel(InventorySlotViewModel viewModel)
        {
            Scope.Bind(viewModel.IsSelected, RenderSelected);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ViewModel.NotifyClicked();
        }

        private void RenderSelected(bool isSelected)
        {
            m_background.color = isSelected ? m_selectedColor : m_normalColor;
        }
    }
}