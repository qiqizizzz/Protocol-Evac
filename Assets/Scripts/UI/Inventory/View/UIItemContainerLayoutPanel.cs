/*
 * ┌──────────────────────────────────┐
 * │  描    述: 容器内部布局窗口, 独立放大绘制容器掩码、区域连接线与外沿
 * │  类    名: UIItemContainerLayoutPanel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.View;
using TMPro;
using UI.Inventory.View.Tools;
using UnityEngine;
using UnityEngine.UI;
using Utils.log;

namespace UI.Inventory.View
{
    public sealed class UIItemContainerLayoutPanel : ViewBase<ItemContainerLayoutViewModel>
    {
        private const string DISMISS_PATH = "Btn_Dismiss";
        private const string CARD_PATH = "Panel_ContainerLayout";
        private const string TITLE_PATH = CARD_PATH + "/Layout_Header/Txt_Title";
        private const string CLOSE_PATH = CARD_PATH + "/Layout_Header/Btn_Close";
        private const string VIEWPORT_PATH = CARD_PATH + "/Layout_Viewport";
        private const string GRID_PATH = VIEWPORT_PATH + "/Layout_Grid";

        private const float BOUNDARY_INSET = 2f;
        private const float BORDER_THICKNESS = 1f;
        private const float DETAIL_RIGHT_GAP = 40f;
        private const float DETAIL_TOP_INSET = 8f;
        private const float MAX_GRID_WIDTH = 316f;
        private const float GRID_PADDING = 10f;
        private const float BASE_CELL_SIZE = 54f;
        private static readonly Color m_cellColor = new Color(0.24f, 0.25f, 0.27f, 0.82f);
        private static readonly Color m_boundaryColor = new Color(0.72f, 0.74f, 0.76f, 1f);
        private static readonly Color m_connectedColor = new Color(0.36f, 0.38f, 0.41f, 1f);

        private RectTransform m_rootRect;
        private RectTransform m_cardRect;
        private RectTransform m_gridRect;
        private LayoutElement m_viewportLayoutElement;
        private TMP_Text m_titleText;
        private Button m_dismissButton;
        private Button m_closeButton;
        private GridLayoutGroup m_gridLayout;

        #region 生命周期
        protected override ItemContainerLayoutViewModel CreateViewModel() => new ItemContainerLayoutViewModel();

        protected override void OnAwake()
        {
            m_rootRect = (RectTransform)transform;
            m_cardRect = FindComponent<RectTransform>(CARD_PATH);
            m_gridRect = FindComponent<RectTransform>(GRID_PATH);
            m_viewportLayoutElement = FindComponent<LayoutElement>(VIEWPORT_PATH);
            m_titleText = FindComponent<TMP_Text>(TITLE_PATH);
            m_dismissButton = FindComponent<Button>(DISMISS_PATH);
            m_closeButton = FindComponent<Button>(CLOSE_PATH);
            m_gridLayout = FindComponent<GridLayoutGroup>(GRID_PATH);
        }

        protected override void BindViewModel(ItemContainerLayoutViewModel viewModel)
        {
            m_dismissButton.onClick.AddListener(viewModel.Dismiss);
            m_closeButton.onClick.AddListener(viewModel.Dismiss);

            Scope.Bind(viewModel.Title, title => m_titleText.text = title);
            Scope.Bind(viewModel.Preview, RefreshGrid);
            Scope.Bind(viewModel.Dismissed, Hide);
        }

        protected override void OnShow(params object[] args)
        {
            if (!ItemPopupPlacement.TryReadContainerLayoutArgs(args, out int containerId, out int instanceId,
                    out Rect detailScreenRect))
            {
                QLog.Error("容器布局窗口参数无效");
                return;
            }

            ItemPopupPlacement.RaiseAbovePanels(gameObject);
            transform.SetAsLastSibling();
            ViewModel.Open(containerId, instanceId);
            RefreshGrid(ViewModel.Preview.Value);
            ItemPopupPlacement.AnchorToRightOfScreenRect(
                m_rootRect, m_cardRect, detailScreenRect, DETAIL_RIGHT_GAP, DETAIL_TOP_INSET);
        }
        #endregion

        #region Private Function
        // 刷新完整容器网格, 所有格节点保持激活以维持行列位置
        private void RefreshGrid(ItemFootprintPreviewVO preview)
        {
            ClearGrid();
            if (preview == null || preview.ColumnCount <= 0 || preview.RowCount <= 0) return;

            float cellSize = UpdateViewportSize(preview);
            m_gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            m_gridLayout.constraintCount = preview.ColumnCount;
            m_gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            m_gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            // 与胸挂槽位一致, 首格固定在左上角才能保持行列步长
            m_gridLayout.childAlignment = TextAnchor.UpperLeft;
            m_gridLayout.spacing = Vector2.zero;
            m_gridLayout.cellSize = new Vector2(cellSize, cellSize);

            int cellCount = preview.ColumnCount * preview.RowCount;
            for (int index = 0; index < cellCount; index++)
            {
                bool solid = index < preview.SolidCells.Count && preview.SolidCells[index];
                InventorySlotEdgeKind top = ReadEdge(preview.TopEdges, index);
                InventorySlotEdgeKind right = ReadEdge(preview.RightEdges, index);
                InventorySlotEdgeKind bottom = ReadEdge(preview.BottomEdges, index);
                InventorySlotEdgeKind left = ReadEdge(preview.LeftEdges, index);
                GameObject cell = CreateCell(index, solid, top, right, bottom, left);
                if (!solid) continue;

                ApplyEdge(cell.transform.Find("Img_BorderTop").GetComponent<Image>(),
                    top, right, bottom, left, 0);
                ApplyEdge(cell.transform.Find("Img_BorderRight").GetComponent<Image>(),
                    top, right, bottom, left, 1);
                ApplyEdge(cell.transform.Find("Img_BorderBottom").GetComponent<Image>(),
                    top, right, bottom, left, 2);
                ApplyEdge(cell.transform.Find("Img_BorderLeft").GetComponent<Image>(),
                    top, right, bottom, left, 3);
            }

            RebuildCardLayout();
        }

        // 按容器行列数更新视口首选尺寸, 卡片由 ContentSizeFitter 自动计算最终宽高
        private float UpdateViewportSize(ItemFootprintPreviewVO preview)
        {
            float cellSize = Mathf.Floor(Mathf.Min(BASE_CELL_SIZE, MAX_GRID_WIDTH / preview.ColumnCount));
            cellSize = Mathf.Max(1f, cellSize);

            float viewportWidth = preview.ColumnCount * cellSize + GRID_PADDING * 2f;
            float viewportHeight = preview.RowCount * cellSize + GRID_PADDING * 2f;

            m_viewportLayoutElement.preferredWidth = viewportWidth;
            m_viewportLayoutElement.preferredHeight = viewportHeight;
            return cellSize;
        }

        // 标记并立即重建卡片布局, 让定位逻辑读取自动计算后的实际尺寸
        private void RebuildCardLayout()
        {
            LayoutRebuilder.MarkLayoutForRebuild(m_cardRect);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_cardRect);
        }

        // 按胸挂格子的规则创建点击区、边界内缩底板与四条边线
        private GameObject CreateCell(int index, bool solid, InventorySlotEdgeKind top,
            InventorySlotEdgeKind right, InventorySlotEdgeKind bottom, InventorySlotEdgeKind left)
        {
            GameObject cell = NewNode($"Cell_{index:00}", m_gridRect);
            Image clickArea = cell.AddComponent<Image>();
            clickArea.color = Color.clear;
            clickArea.raycastTarget = false;
            clickArea.enabled = solid;

            GameObject backgroundNode = NewNode("Img_Background", cell.transform);
            RectTransform backgroundRect = backgroundNode.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            Image background = backgroundNode.AddComponent<Image>();
            background.color = m_cellColor;
            background.raycastTarget = false;
            background.enabled = solid;
            if (solid)
                ApplyBackgroundInset(backgroundRect, top, right, bottom, left);

            CreateBorder(cell.transform, "Img_BorderTop", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f));
            CreateBorder(cell.transform, "Img_BorderRight", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(1f, 0.5f));
            CreateBorder(cell.transform, "Img_BorderBottom", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f));
            CreateBorder(cell.transform, "Img_BorderLeft", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f));
            return cell;
        }

        // 只在区域边界内缩底板, 同区域相邻格保持连续
        private static void ApplyBackgroundInset(RectTransform background, InventorySlotEdgeKind top,
            InventorySlotEdgeKind right, InventorySlotEdgeKind bottom, InventorySlotEdgeKind left)
        {
            background.offsetMin = new Vector2(EdgeInset(left), EdgeInset(bottom));
            background.offsetMax = new Vector2(-EdgeInset(right), -EdgeInset(top));
        }

        // 按方向绘制外沿或同区域内部分隔线, 线条范围与底板内缩保持一致
        private static void ApplyEdge(Image line, InventorySlotEdgeKind top, InventorySlotEdgeKind right,
            InventorySlotEdgeKind bottom, InventorySlotEdgeKind left, int side)
        {
            if (line == null) return;

            InventorySlotEdgeKind edgeKind = side switch
            {
                0 => top,
                1 => right,
                2 => bottom,
                _ => left
            };
            bool visible = edgeKind == InventorySlotEdgeKind.Boundary ||
                ((side == 1 || side == 2) && edgeKind == InventorySlotEdgeKind.Connected);
            line.enabled = visible;
            if (!visible) return;

            bool boundary = edgeKind == InventorySlotEdgeKind.Boundary;
            line.color = boundary ? m_boundaryColor : m_connectedColor;

            float leftInset = EdgeInset(left);
            float rightInset = EdgeInset(right);
            float topInset = EdgeInset(top);
            float bottomInset = EdgeInset(bottom);
            RectTransform rect = line.rectTransform;
            switch (side)
            {
                case 0:
                    rect.offsetMin = new Vector2(
                        leftInset + (left == InventorySlotEdgeKind.Boundary ? BORDER_THICKNESS : 0f),
                        -topInset - BORDER_THICKNESS);
                    rect.offsetMax = new Vector2(
                        -rightInset - (right == InventorySlotEdgeKind.Boundary ? BORDER_THICKNESS : 0f),
                        -topInset);
                    break;
                case 1:
                    rect.offsetMin = new Vector2(
                        -rightInset - BORDER_THICKNESS,
                        bottomInset + (bottom == InventorySlotEdgeKind.Boundary ? BORDER_THICKNESS : 0f));
                    rect.offsetMax = new Vector2(
                        -rightInset,
                        -topInset - (top == InventorySlotEdgeKind.Boundary ? BORDER_THICKNESS : 0f));
                    break;
                case 2:
                    rect.offsetMin = new Vector2(
                        leftInset + (left == InventorySlotEdgeKind.Boundary ? BORDER_THICKNESS : 0f),
                        bottomInset);
                    rect.offsetMax = new Vector2(
                        -rightInset - (right == InventorySlotEdgeKind.Boundary ? BORDER_THICKNESS : 0f),
                        bottomInset + BORDER_THICKNESS);
                    break;
                default:
                    rect.offsetMin = new Vector2(
                        leftInset,
                        bottomInset + (bottom == InventorySlotEdgeKind.Boundary ? BORDER_THICKNESS : 0f));
                    rect.offsetMax = new Vector2(
                        leftInset + BORDER_THICKNESS,
                        -topInset - (top == InventorySlotEdgeKind.Boundary ? BORDER_THICKNESS : 0f));
                    break;
            }
        }

        // 区域边界为视觉间距让出空间
        private static float EdgeInset(InventorySlotEdgeKind kind) =>
            kind == InventorySlotEdgeKind.Boundary ? BOUNDARY_INSET : 0f;

        // 创建一条沿父格内沿铺开的边线
        private static Image CreateBorder(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot)
        {
            GameObject border = NewNode(name, parent);
            RectTransform rect = border.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;

            Image image = border.AddComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        // 读取边线数组, 缺项按不连接处理
        private static InventorySlotEdgeKind ReadEdge(
            System.Collections.Generic.IReadOnlyList<InventorySlotEdgeKind> edges, int index)
            => edges != null && index < edges.Count ? edges[index] : InventorySlotEdgeKind.None;

        // 清空旧尺寸留下的格子, 编辑模式下立即销毁以便测试与重排
        private void ClearGrid()
        {
            for (int i = m_gridRect.childCount - 1; i >= 0; i--)
            {
                GameObject child = m_gridRect.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        // 创建运行时 UI 节点
        private static GameObject NewNode(string name, Transform parent)
        {
            GameObject node = new GameObject(name, typeof(RectTransform));
            node.transform.SetParent(parent, false);
            return node;
        }
        #endregion
    }
}
