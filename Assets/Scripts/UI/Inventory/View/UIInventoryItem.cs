/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品视图, 渲染单个物品块并承载它的拖拽表现
 * │  类    名: UIInventoryItem.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

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
        public const int CELL_SIZE = 80;
        public const int CELL_SPACING = 4;

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

        public void Setup(InventoryItemVO item)
        {
            InstanceId = item.InstanceId;

            m_origin = item.Origin;
            m_rect.sizeDelta = new Vector2(
                item.Size.x * CELL_SIZE + (item.Size.x - 1) * CELL_SPACING,
                item.Size.y * CELL_SIZE + (item.Size.y - 1) * CELL_SPACING);
            m_homeAnchoredPosition = new Vector2(
                item.Origin.x * (CELL_SIZE + CELL_SPACING),
                -item.Origin.y * (CELL_SIZE + CELL_SPACING));
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
        public void OnPointerClick(PointerEventData eventData) => m_viewModel.InteractAt(m_origin);

        public void OnPointerEnter(PointerEventData eventData)
        {
            SnapGhostTo(eventData, m_rect);
            m_viewModel.PreviewDropAt(m_origin);
        }

        public void OnPointerExit(PointerEventData eventData) => m_viewModel.ClearDropPreview(m_origin);

        public void OnBeginDrag(PointerEventData eventData)
        {
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
            ResizeGhostToConfiguredFootprint();
            m_dragCanvas = gameObject.AddComponent<Canvas>();
            m_dragCanvas.overrideSorting = true;
            m_dragCanvas.sortingOrder = DRAG_GHOST_SORTING_ORDER;

            m_background.enabled = false;
            m_countText.enabled = false;
            m_selectedFrame.SetActive(false);

            m_canvasGroup.alpha = DRAG_GHOST_ALPHA;
            m_canvasGroup.blocksRaycasts = false;
        }

        //装备槽中的物品拖拽时立即恢复配置尺寸, 让头盔等大件显示真实拖影
        private void ResizeGhostToConfiguredFootprint()
        {
            if (m_viewModel == null || !InventoryContainerIds.TryGetEquipSlotRole(m_viewModel.ContainerId, out _))
                return;

            if (!m_viewModel.Snapshot.Value.TryGetItem(InstanceId, out InventoryItemVO item))
                return;

            if (!ItemRegistry.TryGet(item.ItemId, out InventoryItemConfigSO config))
                return;

            Vector2Int size = config.GetSizeAt(item.Rotation);
            m_rect.sizeDelta = new Vector2(
                size.x * CELL_SIZE + (size.x - 1) * CELL_SPACING,
                size.y * CELL_SIZE + (size.y - 1) * CELL_SPACING);
        }

        private void EndDragGhost()
        {
            m_rect.sizeDelta = m_dragOriginSize;
            m_background.enabled = true;
            m_countText.enabled = true;
            RenderSelected(m_viewModel != null ? m_viewModel.SelectedInstanceId.Value : NO_SELECTION);

            if (m_dragCanvas == null) return;

            Destroy(m_dragCanvas);
            m_dragCanvas = null;
            m_canvasGroup.alpha = 1f;
            m_canvasGroup.blocksRaycasts = true;
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
