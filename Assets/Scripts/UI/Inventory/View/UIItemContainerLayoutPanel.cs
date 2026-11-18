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
        private const string SIZE_PATH = CARD_PATH + "/Layout_Header/Txt_Size";
        private const string CLOSE_PATH = CARD_PATH + "/Layout_Header/Btn_Close";
        private const string VIEWPORT_PATH = CARD_PATH + "/Layout_Viewport";
        private const string GRID_PATH = VIEWPORT_PATH + "/Layout_Grid";

        private const float BOUNDARY_LINE_WIDTH = 2f;
        private const float CONNECTED_LINE_WIDTH = 1f;
        private static readonly Color m_cellColor = new Color(0.16f, 0.18f, 0.22f, 1f);
        private static readonly Color m_boundaryColor = new Color(0.94f, 0.96f, 1f, 0.95f);
        private static readonly Color m_connectedColor = new Color(0.5f, 0.56f, 0.64f, 0.65f);

        private RectTransform m_cardRect;
        private RectTransform m_gridRect;
        private TMP_Text m_titleText;
        private TMP_Text m_sizeText;
        private Button m_dismissButton;
        private Button m_closeButton;
        private GridLayoutGroup m_gridLayout;

        #region 生命周期
        protected override ItemContainerLayoutViewModel CreateViewModel() => new ItemContainerLayoutViewModel();

        protected override void OnAwake()
        {
            m_cardRect = FindComponent<RectTransform>(CARD_PATH);
            m_gridRect = FindComponent<RectTransform>(GRID_PATH);
            m_titleText = FindComponent<TMP_Text>(TITLE_PATH);
            m_sizeText = FindComponent<TMP_Text>(SIZE_PATH);
            m_dismissButton = FindComponent<Button>(DISMISS_PATH);
            m_closeButton = FindComponent<Button>(CLOSE_PATH);
            m_gridLayout = FindComponent<GridLayoutGroup>(GRID_PATH);
        }

        protected override void BindViewModel(ItemContainerLayoutViewModel viewModel)
        {
            m_dismissButton.onClick.AddListener(viewModel.Dismiss);
            m_closeButton.onClick.AddListener(viewModel.Dismiss);

            Scope.Bind(viewModel.Title, title => m_titleText.text = title);
            Scope.Bind(viewModel.SizeLabel, size => m_sizeText.text = size);
            Scope.Bind(viewModel.Preview, RefreshGrid);
            Scope.Bind(viewModel.Dismissed, Hide);
        }

        protected override void OnShow(params object[] args)
        {
            if (!TryReadTarget(args, out int containerId, out int instanceId))
            {
                QLog.Error("容器布局窗口参数无效");
                return;
            }

            ItemPopupPlacement.RaiseAbovePanels(gameObject);
            transform.SetAsLastSibling();
            ViewModel.Open(containerId, instanceId);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_cardRect);
            RefreshGrid(ViewModel.Preview.Value);
        }
        #endregion

        #region Private Function
        // 刷新完整容器网格, 所有格节点保持激活以维持行列位置
        private void RefreshGrid(ItemFootprintPreviewVO preview)
        {
            ClearGrid();
            if (preview == null || preview.ColumnCount <= 0 || preview.RowCount <= 0) return;

            m_gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            m_gridLayout.constraintCount = preview.ColumnCount;
            m_gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            m_gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            m_gridLayout.childAlignment = TextAnchor.MiddleCenter;
            m_gridLayout.spacing = Vector2.zero;

            float cellSize = Mathf.Floor(Mathf.Min(
                m_gridRect.rect.width / preview.ColumnCount,
                m_gridRect.rect.height / preview.RowCount));
            cellSize = Mathf.Max(1f, cellSize);
            m_gridLayout.cellSize = new Vector2(cellSize, cellSize);

            int cellCount = preview.ColumnCount * preview.RowCount;
            for (int index = 0; index < cellCount; index++)
            {
                bool solid = index < preview.SolidCells.Count && preview.SolidCells[index];
                GameObject cell = CreateCell(index, solid);
                if (!solid) continue;

                ApplyEdge(cell.transform.Find("Img_BorderTop").GetComponent<Image>(),
                    ReadEdge(preview.TopEdges, index), false);
                ApplyEdge(cell.transform.Find("Img_BorderRight").GetComponent<Image>(),
                    ReadEdge(preview.RightEdges, index), true);
                ApplyEdge(cell.transform.Find("Img_BorderBottom").GetComponent<Image>(),
                    ReadEdge(preview.BottomEdges, index), true);
                ApplyEdge(cell.transform.Find("Img_BorderLeft").GetComponent<Image>(),
                    ReadEdge(preview.LeftEdges, index), false);
            }
        }

        // 创建单格根图与四条边线, 空洞格仍保留节点占位
        private GameObject CreateCell(int index, bool solid)
        {
            GameObject cell = NewNode($"Cell_{index:00}", m_gridRect);
            Image background = cell.AddComponent<Image>();
            background.color = m_cellColor;
            background.raycastTarget = false;
            background.enabled = solid;

            CreateBorder(cell.transform, "Img_BorderTop", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, BOUNDARY_LINE_WIDTH), Vector2.zero);
            CreateBorder(cell.transform, "Img_BorderRight", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(1f, 0.5f), new Vector2(BOUNDARY_LINE_WIDTH, 0f), Vector2.zero);
            CreateBorder(cell.transform, "Img_BorderBottom", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, BOUNDARY_LINE_WIDTH), Vector2.zero);
            CreateBorder(cell.transform, "Img_BorderLeft", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(BOUNDARY_LINE_WIDTH, 0f), Vector2.zero);
            return cell;
        }

        // 边线铺在格子内沿, 外沿与跨区域线使用白色, 同区域接缝使用细灰线
        private static void ApplyEdge(Image line, InventorySlotEdgeKind edgeKind, bool drawConnected)
        {
            if (line == null) return;

            bool visible = edgeKind == InventorySlotEdgeKind.Boundary ||
                (edgeKind == InventorySlotEdgeKind.Connected && drawConnected);
            line.enabled = visible;
            if (!visible) return;

            bool boundary = edgeKind == InventorySlotEdgeKind.Boundary;
            float width = boundary ? BOUNDARY_LINE_WIDTH : CONNECTED_LINE_WIDTH;
            line.color = boundary ? m_boundaryColor : m_connectedColor;
            RectTransform rect = line.rectTransform;
            if (rect.anchorMin.x == rect.anchorMax.x)
            {
                Vector2 size = rect.sizeDelta;
                size.x = width;
                rect.sizeDelta = size;
            }
            else
            {
                Vector2 size = rect.sizeDelta;
                size.y = width;
                rect.sizeDelta = size;
            }
        }

        // 创建一条沿父格内沿铺开的边线
        private static Image CreateBorder(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            GameObject border = NewNode(name, parent);
            RectTransform rect = border.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;

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

        // 读取窗口打开参数
        private static bool TryReadTarget(object[] args, out int containerId, out int instanceId)
        {
            containerId = 0;
            instanceId = 0;
            if (args == null || args.Length < 2 || !(args[0] is int) || !(args[1] is int)) return false;

            containerId = (int)args[0];
            instanceId = (int)args[1];
            return true;
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
