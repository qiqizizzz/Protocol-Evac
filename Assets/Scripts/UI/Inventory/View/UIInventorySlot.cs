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
        [SerializeField] private Color NormalColor = new Color(0.24f, 0.25f, 0.27f, 0.82f);

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

        [Header("区域分界")]
        [Tooltip("区域边界单边内缩距离, 相邻两格共同形成间距")]
        [SerializeField, Min(0f)] private float BoundaryInset = 2f;

        [Tooltip("区域分界线厚度")]
        [SerializeField, Min(0.5f)] private float BorderThickness = 1f;

        [Tooltip("同一区域内部的分隔线颜色")]
        [SerializeField] private Color InnerLineColor = new Color(0.36f, 0.38f, 0.41f, 1f);

        [Tooltip("区域外沿与跨区交界线颜色")]
        [SerializeField] private Color BoundaryLineColor = new Color(0.72f, 0.74f, 0.76f, 1f);

        private Image m_clickArea;
        private Image m_background;
        private Image m_borderTop;
        private Image m_borderRight;
        private Image m_borderBottom;
        private Image m_borderLeft;
        private RectTransform m_backgroundRect;
        private RectTransform m_rect;
        private bool m_isSolid = true;
        private InventoryCellMark m_mark;
        private InventorySlotEdgeKind m_topEdge = InventorySlotEdgeKind.None;
        private InventorySlotEdgeKind m_rightEdge = InventorySlotEdgeKind.None;
        private InventorySlotEdgeKind m_bottomEdge = InventorySlotEdgeKind.None;
        private InventorySlotEdgeKind m_leftEdge = InventorySlotEdgeKind.None;

        protected override InventorySlotViewModel CreateViewModel() => null;

        protected override void OnAwake()
        {
            m_clickArea = GetComponent<Image>();
            m_rect = GetComponent<RectTransform>();
            m_background = transform.Find("Img_Background")?.GetComponent<Image>();
            m_borderTop = transform.Find("Img_BorderTop")?.GetComponent<Image>();
            m_borderRight = transform.Find("Img_BorderRight")?.GetComponent<Image>();
            m_borderBottom = transform.Find("Img_BorderBottom")?.GetComponent<Image>();
            m_borderLeft = transform.Find("Img_BorderLeft")?.GetComponent<Image>();
            m_backgroundRect = m_background != null ? m_background.rectTransform : null;

            if (m_clickArea == null)
                QLog.Error($"格子预制体根节点缺少点击 Image 组件：{gameObject.name}");
            if (m_background == null)
                QLog.Error($"缺少底色节点：{gameObject.name}/Img_Background");
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
            Scope.Bind(viewModel.TopEdgeKind, RenderTopEdge);
            Scope.Bind(viewModel.RightEdgeKind, RenderRightEdge);
            Scope.Bind(viewModel.BottomEdgeKind, RenderBottomEdge);
            Scope.Bind(viewModel.LeftEdgeKind, RenderLeftEdge);
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
            if (m_clickArea != null)
                m_clickArea.raycastTarget = isSolid;

            ApplyVisual();
        }

        private void RenderMark(InventoryCellMark mark)
        {
            m_mark = mark;
            ApplyVisual();
        }

        //记录上边状态并刷新整格视觉
        private void RenderTopEdge(InventorySlotEdgeKind kind)
        {
            m_topEdge = kind;
            ApplyVisual();
        }

        //记录右边状态并刷新整格视觉
        private void RenderRightEdge(InventorySlotEdgeKind kind)
        {
            m_rightEdge = kind;
            ApplyVisual();
        }

        //记录下边状态并刷新整格视觉
        private void RenderBottomEdge(InventorySlotEdgeKind kind)
        {
            m_bottomEdge = kind;
            ApplyVisual();
        }

        //记录左边状态并刷新整格视觉
        private void RenderLeftEdge(InventorySlotEdgeKind kind)
        {
            m_leftEdge = kind;
            ApplyVisual();
        }

        //统一计算点击、底色、内缩与四边线条
        private void ApplyVisual()
        {
            if (m_clickArea != null)
            {
                m_clickArea.color = Color.clear;
                m_clickArea.raycastTarget = m_isSolid;
            }

            if (m_background != null)
            {
                m_background.enabled = m_isSolid;
                m_background.color = ColorOf(m_mark);
            }

            if (m_isSolid)
                ApplyBackgroundInset();

            ApplyBorders();
        }

        //只在区域边界内缩, 同区域相邻格保持无缝底色
        private void ApplyBackgroundInset()
        {
            if (m_backgroundRect == null) return;

            float left = EdgeInset(m_leftEdge);
            float right = EdgeInset(m_rightEdge);
            float top = EdgeInset(m_topEdge);
            float bottom = EdgeInset(m_bottomEdge);

            m_backgroundRect.anchorMin = Vector2.zero;
            m_backgroundRect.anchorMax = Vector2.one;
            m_backgroundRect.offsetMin = new Vector2(left, bottom);
            m_backgroundRect.offsetMax = new Vector2(-right, -top);
        }

        //边界线由当前格绘制, 内部灰线只由右/下侧绘制避免双线
        private void ApplyBorders()
        {
            ApplyTopBorder();
            ApplyRightBorder();
            ApplyBottomBorder();
            ApplyLeftBorder();
        }

        //上边只绘制跨区或外沿边界
        private void ApplyTopBorder()
        {
            bool show = m_isSolid && m_topEdge == InventorySlotEdgeKind.Boundary;
            ApplyBorder(m_borderTop, show, m_topEdge, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f));
        }

        //右边绘制区域边界或同区域内部分隔线
        private void ApplyRightBorder()
        {
            bool show = m_isSolid && (m_rightEdge == InventorySlotEdgeKind.Connected ||
                m_rightEdge == InventorySlotEdgeKind.Boundary);
            ApplyBorder(m_borderRight, show, m_rightEdge, new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(1f, 0.5f));
        }

        //下边绘制区域边界或同区域内部分隔线
        private void ApplyBottomBorder()
        {
            bool show = m_isSolid && (m_bottomEdge == InventorySlotEdgeKind.Connected ||
                m_bottomEdge == InventorySlotEdgeKind.Boundary);
            ApplyBorder(m_borderBottom, show, m_bottomEdge, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f));
        }

        //左边只绘制跨区或外沿边界
        private void ApplyLeftBorder()
        {
            bool show = m_isSolid && m_leftEdge == InventorySlotEdgeKind.Boundary;
            ApplyBorder(m_borderLeft, show, m_leftEdge, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f));
        }

        //按边方向设置线条矩形和颜色
        private void ApplyBorder(Image border, bool show, InventorySlotEdgeKind kind, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 pivot)
        {
            if (border == null) return;

            border.enabled = show;
            border.raycastTarget = false;
            if (!show) return;

            float left = EdgeInset(m_leftEdge);
            float right = EdgeInset(m_rightEdge);
            float top = EdgeInset(m_topEdge);
            float bottom = EdgeInset(m_bottomEdge);
            RectTransform rect = border.rectTransform;

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;

            if (anchorMax.x > anchorMin.x && Mathf.Approximately(anchorMin.y, 1f))
            {
                rect.offsetMin = new Vector2(left, -top - BorderThickness);
                rect.offsetMax = new Vector2(-right, -top);
            }
            else if (anchorMax.x > anchorMin.x)
            {
                rect.offsetMin = new Vector2(left, bottom);
                rect.offsetMax = new Vector2(-right, bottom + BorderThickness);
            }
            else if (Mathf.Approximately(anchorMin.x, 1f))
            {
                rect.offsetMin = new Vector2(-right - BorderThickness, bottom);
                rect.offsetMax = new Vector2(-right, -top);
            }
            else
            {
                rect.offsetMin = new Vector2(left, bottom);
                rect.offsetMax = new Vector2(left + BorderThickness, -top);
            }

            border.color = kind == InventorySlotEdgeKind.Boundary ? BoundaryLineColor : InnerLineColor;
        }

        //区域边界为视觉间距让出空间
        private float EdgeInset(InventorySlotEdgeKind kind) =>
            kind == InventorySlotEdgeKind.Boundary ? BoundaryInset : 0f;

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
