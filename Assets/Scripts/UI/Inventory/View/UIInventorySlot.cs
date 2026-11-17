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
using Utils.log;

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
        private Image m_borderTop;
        private Image m_borderRight;
        private Image m_borderBottom;
        private Image m_borderLeft;
        private RectTransform m_rect;
        private bool m_isSolid = true;
        private bool m_showTopBorder;
        private bool m_showRightBorder;
        private bool m_showBottomBorder;
        private bool m_showLeftBorder;
        private InventoryCellMark m_mark;

        protected override InventorySlotViewModel CreateViewModel() => null;

        protected override void OnAwake()
        {
            m_background = GetComponent<Image>();
            m_rect = GetComponent<RectTransform>();
            m_borderTop = transform.Find("Img_BorderTop")?.GetComponent<Image>();
            m_borderRight = transform.Find("Img_BorderRight")?.GetComponent<Image>();
            m_borderBottom = transform.Find("Img_BorderBottom")?.GetComponent<Image>();
            m_borderLeft = transform.Find("Img_BorderLeft")?.GetComponent<Image>();

            if (m_background == null)
                QLog.Error($"格子预制体缺少 Image 组件：{gameObject.name}");
            if (m_borderTop == null)
                QLog.Error($"缺少上边框节点：{gameObject.name}/Img_BorderTop");
            if (m_borderRight == null)
                QLog.Error($"缺少右边框节点：{gameObject.name}/Img_BorderRight");
            if (m_borderBottom == null)
                QLog.Error($"缺少下边框节点：{gameObject.name}/Img_BorderBottom");
            if (m_borderLeft == null)
                QLog.Error($"缺少左边框节点：{gameObject.name}/Img_BorderLeft");
        }

        protected override void BindViewModel(InventorySlotViewModel viewModel)
        {
            Scope.Bind(viewModel.Mark, RenderMark);
            Scope.Bind(viewModel.IsSolid, RenderSolid);
            Scope.Bind(viewModel.ShowTopBorder, RenderTopBorder);
            Scope.Bind(viewModel.ShowRightBorder, RenderRightBorder);
            Scope.Bind(viewModel.ShowBottomBorder, RenderBottomBorder);
            Scope.Bind(viewModel.ShowLeftBorder, RenderLeftBorder);
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
            ApplyBorders();
        }

        private void RenderMark(InventoryCellMark mark)
        {
            m_mark = mark;
            ApplyMark();
        }

        private void RenderTopBorder(bool show)
        {
            m_showTopBorder = show;
            ApplyBorders();
        }

        private void RenderRightBorder(bool show)
        {
            m_showRightBorder = show;
            ApplyBorders();
        }

        private void RenderBottomBorder(bool show)
        {
            m_showBottomBorder = show;
            ApplyBorders();
        }

        private void RenderLeftBorder(bool show)
        {
            m_showLeftBorder = show;
            ApplyBorders();
        }

        private void ApplyMark()
        {
            if (!m_isSolid || m_background == null) return;

            m_background.color = ColorOf(m_mark);
        }

        //边框只用于勾出区域边界, 不参与射线检测
        private void ApplyBorders()
        {
            ApplyBorder(m_borderTop, m_showTopBorder);
            ApplyBorder(m_borderRight, m_showRightBorder);
            ApplyBorder(m_borderBottom, m_showBottomBorder);
            ApplyBorder(m_borderLeft, m_showLeftBorder);
        }

        private void ApplyBorder(Image border, bool show)
        {
            if (border == null) return;

            border.enabled = m_isSolid && show;
            border.raycastTarget = false;
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
