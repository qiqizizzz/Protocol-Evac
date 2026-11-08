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
    public sealed class UIInventorySlot : ViewBase<InventorySlotViewModel>, IPointerClickHandler, IDropHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        [Header("格子着色")]
        [Tooltip("无特殊状态时的普通底色")]
        [SerializeField] private Color NormalColor = new Color(1.00f, 1.00f, 1.00f, 0.16f);

        [Tooltip("当前选中物品的占格色")]
        [SerializeField] private Color SelectedColor = new Color(0.31f, 0.76f, 1.00f, 0.55f);

        [Tooltip("可直接放置的落点色")]
        [SerializeField] private Color DropValidColor = new Color(0.25f, 0.85f, 0.42f, 0.55f);

        [Tooltip("会与占格物品交换的落点色")]
        [SerializeField] private Color DropSwapColor = new Color(1.00f, 0.66f, 0.20f, 0.55f);

        [Tooltip("会与同类物品堆叠合并的落点色")]
        [SerializeField] private Color DropMergeColor = new Color(0.30f, 0.85f, 0.75f, 0.55f);

        [Tooltip("被规则拒绝的落点色")]
        [SerializeField] private Color DropInvalidColor = new Color(0.92f, 0.27f, 0.26f, 0.55f);

        private Image m_background;
        private RectTransform m_rect;
        private bool m_isSolid = true;
        private InventoryCellMark m_mark;

        protected override InventorySlotViewModel CreateViewModel() => null;

        protected override void OnAwake()
        {
            m_background = GetComponent<Image>();
            m_rect = GetComponent<RectTransform>();
#if UNITY_EDITOR
            if (m_background == null)
                Utils.log.QLog.Error($"[{nameof(UIInventorySlot)}] 格子预制体缺少 Image 组件：{gameObject.name}");
#endif
        }

        protected override void BindViewModel(InventorySlotViewModel viewModel)
        {
            Scope.Bind(viewModel.Mark, RenderMark);
            Scope.Bind(viewModel.IsSolid, RenderSolid);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 右键用于物品菜单, 空格子仅响应左键
            if (eventData.button != PointerEventData.InputButton.Left) return;

            ViewModel.NotifyClicked();
        }

        public void OnDrop(PointerEventData eventData)
        {
            ViewModel.NotifyDropped(UIInventoryItem.ResolveDropAnchor(eventData, m_rect, ViewModel.Cell));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ViewModel.NotifyHovered(true, UIInventoryItem.ResolveDropAnchor(eventData, m_rect, ViewModel.Cell));
        }

        // 空格子占一格, 离开时按自身格号上报
        public void OnPointerExit(PointerEventData eventData)
        {
            ViewModel.NotifyHovered(false, Vector2Int.zero);
        }

        #region Private Function
        //空洞格只留位置不画也不接点击, 布局仍按外接矩形排
        private void RenderSolid(bool isSolid)
        {
            m_isSolid = isSolid;
            if (m_background == null) return;
            m_background.enabled = isSolid;
            m_background.raycastTarget = isSolid;
            ApplyMark();
        }

        private void RenderMark(InventoryCellMark mark)
        {
            m_mark = mark;
            ApplyMark();
        }

        private void ApplyMark()
        {
            if (!m_isSolid || m_background == null) return;

            m_background.color = ColorOf(m_mark);
        }

        private Color ColorOf(InventoryCellMark mark)
        {
            switch (mark)
            {
                case InventoryCellMark.Selected: return SelectedColor;
                case InventoryCellMark.DropValid: return DropValidColor;
                case InventoryCellMark.DropSwap: return DropSwapColor;
                case InventoryCellMark.DropMerge: return DropMergeColor;
                case InventoryCellMark.DropInvalid: return DropInvalidColor;
                default: return NormalColor;
            }
        }
        #endregion
    }
}
