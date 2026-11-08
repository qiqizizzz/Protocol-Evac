/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品视图, 渲染单个物品块并承载它的拖拽表现
 * │  类    名: UIInventoryItem.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.MVVM.Binding;
using TMPro;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Defines;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Find;

namespace UI.Inventory.View
{
    public sealed class UIInventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const int DRAG_GHOST_SORTING_ORDER = 100; //拖影排序层级, 高于面板默认层级
        private const float DRAG_GHOST_ALPHA = 0.75f; //拖影期间的不透明度

        private const int NO_SELECTION = -1; //无选中

        private const string ICON_PATH = "Img_Icon";
        private const string COUNT_PATH = "Txt_Count";
        private const string SELECTED_PATH = "Img_Selected";

        private RectTransform m_rect;
        private Image m_background;
        private Image m_icon;
        private TMP_Text m_countText;
        private GameObject m_selectedFrame;
        private CanvasGroup m_canvasGroup;
        private Canvas m_dragCanvas;

        //单元格边长与间距, 由所在载体的 GridLayoutGroup 注入, 物品尺寸始终跟随格子配置
        private Vector2 m_cellSize;
        private Vector2 m_cellSpacing;

        //恢复裁切时复用的图形缓存, 避免每次拖拽结束产生分配
        private readonly List<MaskableGraphic> m_clippableGraphics = new List<MaskableGraphic>();

        private InventoryViewModel m_viewModel;
        private Vector2Int m_origin;
        private Vector2 m_homeAnchoredPosition;
        private RectTransform m_snapTarget;
        private bool m_isDragging;
        private PointerEventData.InputButton m_dragButton;
        private Vector2 m_dragOriginSize;

        public int InstanceId { get; private set; }

        private void Awake()
        {
            m_rect = this.GetOwnerComponent<RectTransform>();
            m_background = this.GetOwnerComponent<Image>();
            m_icon = this.FindChildComponent<Image>(ICON_PATH);
            m_countText = this.FindChildComponent<TMP_Text>(COUNT_PATH);
            m_selectedFrame = this.FindChildGo(SELECTED_PATH);
            m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 兜底自愈: 残留的拖影排序会让物品永久脱离 RectMask2D 视口裁切, 每次激活都复位一次
        private void OnEnable()
        {
            if (!TryGetComponent(out m_dragCanvas)) return;
            if (!m_dragCanvas.overrideSorting) return;

            ReleaseDragGhost();
        }

        // 面板关闭或载体隐藏时收不到 OnEndDrag, 这里兜底收尾, 避免拖影状态残留
        private void OnDisable() => FinishDrag();

        /// <summary>
        /// 按快照刷新物品的占格尺寸、位置与外观
        /// </summary>
        /// <param name="item">物品数据载体</param>
        /// <param name="cellSize">所在容器 GridLayoutGroup 的单元格边长</param>
        /// <param name="cellSpacing">所在容器 GridLayoutGroup 的单元格间距</param>
        public void Setup(InventoryItemVO item, Vector2 cellSize, Vector2 cellSpacing)
        {
            m_cellSize = cellSize;
            m_cellSpacing = cellSpacing;
            InstanceId = item.InstanceId;

            m_origin = item.Origin;
            m_rect.sizeDelta = FootprintSize(item.Size);
            m_homeAnchoredPosition = CellOrigin(item.Origin);
            m_rect.anchoredPosition = m_homeAnchoredPosition;

            m_background.enabled = true;
            m_countText.enabled = true;

            m_background.color = item.BaseColor;
            m_icon.sprite = item.Icon;
            m_icon.enabled = item.Icon != null;

            m_countText.text = item.StackCount > 1 ? item.StackCount.ToString() : string.Empty;
        }

        public void Bind(BindingScope scope, InventoryViewModel viewModel)
        {
            m_viewModel = viewModel;
            scope.Bind(viewModel.SelectedInstanceId, RenderSelected);
        }

        /// <summary>
        /// 拖拽中将物品视图对齐到目标格子的左上角原点
        /// </summary>
        /// <param name="eventData">当前指针事件</param>
        /// <param name="target">被悬停格子或物品的 RectTransform</param>
        public static void SnapGhostTo(PointerEventData eventData, RectTransform target)
        {
            if (eventData == null || eventData.pointerDrag == null) return;
            if (!eventData.pointerDrag.TryGetComponent(out UIInventoryItem ghost)) return;
            if (!ghost.m_isDragging) return;

            ghost.m_snapTarget = target;
            ghost.SnapToCell(target);
        }

        #region UI事件
        //左键开详情, 右键开按钮集合, 中键不响应
        public void OnPointerClick(PointerEventData eventData)
        {
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    m_viewModel.InteractAt(m_origin);
                    break;
                case PointerEventData.InputButton.Right:
                    m_viewModel.OpenMenuAt(m_origin);
                    break;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SnapGhostTo(eventData, m_rect);
            m_viewModel.PreviewDropAt(m_origin);
        }

        public void OnPointerExit(PointerEventData eventData) => m_viewModel.ClearDropPreview(m_origin);

        public void OnBeginDrag(PointerEventData eventData)
        {
            // 同一次拖拽的重复回调只认第一次, 否则会叠加出第二个嵌套 Canvas 把裁切彻底弄丢
            if (m_isDragging) return;

            m_viewModel.BeginDragAt(m_origin);

            m_isDragging = true;
            m_dragButton = eventData.button;
            BeginDragGhost();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (m_snapTarget != null)
                SnapToCell(m_snapTarget);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == gameObject) return;

            m_viewModel.PlaceDraggedItemAt(m_origin);
        }

        public void OnEndDrag(PointerEventData eventData) => FinishDrag();
        #endregion

        #region Private Function
        private void SnapToCell(RectTransform target)
        {
            ApplyTargetCellMetrics(target);

            Rect targetRect = target.rect;
            m_rect.position = target.TransformPoint(new Vector3(targetRect.xMin, targetRect.yMax, 0f));
        }

        private void Update()
        {
            if (!m_isDragging) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                FinishDrag();
                return;
            }

            if (m_dragButton != PointerEventData.InputButton.Right && Input.GetMouseButtonDown(1)) FinishDrag();
        }

        private void BeginDragGhost()
        {
            m_dragOriginSize = m_rect.sizeDelta;

            // 全程复用同一个 Canvas, 只用 overrideSorting 开关拖影层级, 结束时就地交还裁切权
            if (m_dragCanvas == null && !TryGetComponent(out m_dragCanvas))
                m_dragCanvas = gameObject.AddComponent<Canvas>();

            m_dragCanvas.overrideSorting = true;
            m_dragCanvas.sortingOrder = DRAG_GHOST_SORTING_ORDER;

            m_background.enabled = false;
            m_countText.enabled = false;
            m_selectedFrame.SetActive(false);

            m_canvasGroup.alpha = DRAG_GHOST_ALPHA;
            m_canvasGroup.blocksRaycasts = false;
        }

        //拖影尺寸跟随悬停容器的格子配置, 避免从 160 的装备槽拖到 80 的背包时仍沿用源容器尺寸
        private void ApplyTargetCellMetrics(RectTransform target)
        {
            if (target == null) return;

            UIInventoryCarrier targetCarrier = target.GetComponentInParent<UIInventoryCarrier>();
            if (targetCarrier == null) return;

            Vector2Int footprint = ResolveTargetFootprint(targetCarrier);
            if (footprint.x <= 0 || footprint.y <= 0) return;

            Vector2 cell = targetCarrier.CellSize;
            Vector2 spacing = targetCarrier.CellSpacing;
            m_rect.sizeDelta = new Vector2(
                footprint.x * cell.x + (footprint.x - 1) * spacing.x,
                footprint.y * cell.y + (footprint.y - 1) * spacing.y);
        }

        //装备槽里的物品按 1x1 展示, 其余容器按物品配置的真实占格
        private Vector2Int ResolveTargetFootprint(UIInventoryCarrier carrier)
        {
            if (carrier.ViewModel != null &&
                InventoryContainerIds.TryGetEquipSlotRole(carrier.ViewModel.ContainerId, out _))
                return Vector2Int.one;

            if (m_viewModel != null && m_viewModel.Snapshot.Value.TryGetItem(InstanceId, out InventoryItemVO item) &&
                ItemRegistry.TryGet(item.ItemId, out InventoryItemConfigSO config))
                return config.GetSizeAt(item.Rotation);

            return Vector2Int.zero;
        }

        // 按配置的单元格尺寸算物品占格的外接矩形
        private Vector2 FootprintSize(Vector2Int size)
        {
            return new Vector2(
                size.x * m_cellSize.x + (size.x - 1) * m_cellSpacing.x,
                size.y * m_cellSize.y + (size.y - 1) * m_cellSpacing.y);
        }

        // 按配置的单元格尺寸算物品左上角锚点位置
        private Vector2 CellOrigin(Vector2Int origin)
        {
            return new Vector2(
                origin.x * (m_cellSize.x + m_cellSpacing.x),
                -origin.y * (m_cellSize.y + m_cellSpacing.y));
        }

        private void EndDragGhost()
        {
            m_rect.sizeDelta = m_dragOriginSize;
            m_background.enabled = true;
            m_countText.enabled = true;
            RenderSelected(m_viewModel != null ? m_viewModel.SelectedInstanceId.Value : NO_SELECTION);

            m_canvasGroup.alpha = 1f;
            m_canvasGroup.blocksRaycasts = true;

            ReleaseDragGhost();
        }

        // 关闭拖影排序并交还视口裁切: RectMask2D 在祖先链出现 overrideSorting 的 Canvas 时会整体失效
        private void ReleaseDragGhost()
        {
            if (m_dragCanvas == null) return;

            m_dragCanvas.overrideSorting = false;
            RestoreViewportClipping();

            Destroy(m_dragCanvas);
            m_dragCanvas = null;
        }

        // 切换 overrideSorting 不会通知 IClippable, 只能手动让物品内所有 Graphic 重新认领遮罩
        private void RestoreViewportClipping()
        {
            GetComponentsInChildren(true, m_clippableGraphics);

            for (int i = 0; i < m_clippableGraphics.Count; i++)
                m_clippableGraphics[i].RecalculateClipping();

            m_clippableGraphics.Clear();
        }

        private void FinishDrag()
        {
            if (!m_isDragging) return;

            m_isDragging = false;
            m_snapTarget = null;
            EndDragGhost();
            m_rect.anchoredPosition = m_homeAnchoredPosition;
            m_viewModel.CancelDrag();
        }

        private void RenderSelected(int selectedInstanceId)
        {
            m_selectedFrame.SetActive(selectedInstanceId == InstanceId);
        }
        #endregion
    }
}
