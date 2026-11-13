/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品视图, 渲染单个物品块并承载它的拖拽表现
 * │  类    名: UIInventoryItem.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using System.Collections;
using Framework.QTower.MVVM.Binding;
using TMPro;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Defines;
using UI.Inventory.View.Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.Find;

namespace UI.Inventory.View
{
    public sealed class UIInventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private const int DRAG_GHOST_SORTING_ORDER = 100; //拖影排序层级, 高于面板默认层级
        private const float DRAG_GHOST_ALPHA = 0.75f; //拖影期间的不透明度
        private const float DRAG_LIFT_THRESHOLD_SQR = 36f; //抬起拖影所需的指针位移平方, 约 6 像素

        private const int NO_SELECTION = -1; //无选中

        // 详情延迟窗口与双击配对窗口同源, 到点没等到第二击才把详情弹出来
        private static readonly WaitForSecondsRealtime m_detailRevealDelay =
            new WaitForSecondsRealtime(DoubleClickDetector.DOUBLE_CLICK_INTERVAL);

        private const string ICON_PATH = "Img_Icon";
        private const string SILHOUETTE_PATH = "Img_Silhouette";
        private const string COUNT_PATH = "Txt_Count";
        private const string SELECTED_PATH = "Img_Selected";

        private static readonly Vector2 m_centerAnchor = new Vector2(0.5f, 0.5f);

        private RectTransform m_rect;
        private Image m_background;
        private Image m_icon;
        private Image m_silhouette;
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
        private Vector2Int m_cellFootprint; // 自身占格
        private Vector2Int m_previewCell; // 最近一次上报的预览锚点格
        private Vector2 m_homeAnchoredPosition;
        private RectTransform m_dragPlaneRect; // 拖影投影所用的物品层矩形
        private Camera m_dragCamera; // 拖影投影相机, Overlay 画布为 null
        private Vector2 m_dragPointerPosition; // 指针屏幕坐标
        private RectTransform m_hoverLayer; // 悬停容器的物品层, 为空表示指针不在任何网格上
        private UIInventoryCarrier m_hoverCarrier; // 悬停容器载体, 为空表示指针不在任何网格上
        private readonly DoubleClickDetector m_doubleClick = new DoubleClickDetector(); // 左键双击丢弃判定
        private Coroutine m_pendingDetail; // 未到点的详情延迟协程, 双击或拖拽时掐掉
        private Vector2 m_hoverCell; // 悬停容器的单元格边长
        private Vector2 m_hoverSpacing; // 悬停容器的单元格间距
        private bool m_hoverIsEquipSlot; // 悬停容器是否单格装备槽
        private Vector2 m_hoverStep; // 悬停容器的格步长
        private Vector2Int m_hoverGridCount; // 悬停容器的格数
        private Vector2Int m_ghostFootprint; // 拖影在悬停容器中的占格
        private InventoryRotation m_modelRotation; // 快照里的物品朝向
        private Vector2Int m_anchorCell; // 拖影当前吸附的锚点格
        private Vector2Int m_hoverTargetCell; // 最近一次悬停目标自身的格号
        private bool m_isDragging;
        private bool m_isSearching;
        private Color m_baseColor;
        private bool m_isPendingDrag; // 已按下但位移未达阈值
        private bool m_dragLiftedThisGesture; // 本次按下是否真抬成过拖拽, 抬起后的点击不再算单击
        private Vector2 m_pendingDragStart; // 按下时的指针屏幕坐标
        private PointerEventData.InputButton m_dragButton;
        private Vector2 m_dragOriginSize;

        public int InstanceId { get; private set; }

        // 本视图所属容器是否为按单格收纳的装备槽
        private bool IsOwnContainerEquipSlot => m_viewModel != null &&
            InventoryContainerIds.TryGetEquipSlotRole(m_viewModel.ContainerId, out _);

        // 拖影当前朝向: 未绑定视图模型时沿用快照里的朝向
        private InventoryRotation DraggedRotation => m_viewModel != null ? m_viewModel.DragRotation : m_modelRotation;

        private void Awake()
        {
            m_rect = this.GetOwnerComponent<RectTransform>();
            m_background = this.GetOwnerComponent<Image>();
            m_icon = this.FindChildComponent<Image>(ICON_PATH);
            m_silhouette = this.FindChildComponent<Image>(SILHOUETTE_PATH);
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
        private void OnDisable()
        {
            StopDetailReveal();
            FinishDrag();
        }

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

            // 视图复用给别的物品时旧的点击配对作废, 否则跨物品可能被误判成双击
            if (InstanceId != item.InstanceId)
            {
                m_doubleClick.Reset();
                StopDetailReveal();
            }
            InstanceId = item.InstanceId;

            m_origin = item.Origin;
            m_cellFootprint = item.Size;
            m_modelRotation = item.Rotation;
            m_homeAnchoredPosition = CellOrigin(item.Origin);

            // 拖拽期间不重设几何, 否则快照刷新会把浮动的拖影拽回格子
            if (!m_isDragging)
            {
                m_rect.sizeDelta = FootprintSize(item.Size);
                m_rect.anchoredPosition = m_homeAnchoredPosition;
                ApplyIconGeometry(item.Size, IsOwnContainerEquipSlot ? InventoryRotation.None : item.Rotation,
                    m_cellSize, m_cellSpacing);
            }

            m_baseColor = item.BaseColor;
            m_background.color = m_baseColor;
            m_icon.sprite = item.Icon;
            m_countText.text = item.StackCount > 1 ? item.StackCount.ToString() : string.Empty;
            ApplyPresentation();
        }

        /// <summary>
        /// 设置物品是否处于未搜索状态, 搜索态只显示连续占格阴影
        /// </summary>
        /// <param name="searching">是否隐藏图标与数量并显示阴影</param>
        public void SetSearching(bool searching)
        {
            m_isSearching = searching;
            ApplyPresentation();
        }

        public void Bind(BindingScope scope, InventoryViewModel viewModel)
        {
            m_viewModel = viewModel;
            scope.Bind(viewModel.SelectedInstanceId, RenderSelected);

            // 浮层被任何入口收起时, 未到点的详情延迟一起作废: 点空白、反选、拖拽都不该再补一次弹窗
            scope.Bind(viewModel.PopupClosed, StopDetailReveal);
        }

        /// <summary>
        /// 读取物品完整占格在屏幕空间中的矩形
        /// </summary>
        /// <param name="screenRect">物品完整占格的屏幕矩形</param>
        public bool TryGetScreenRect(out Rect screenRect)
        {
            screenRect = default;
            if (m_rect == null) return false;

            Canvas canvas = m_rect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector3[] corners = new Vector3[4];
            m_rect.GetWorldCorners(corners);

            Vector2 first = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 second = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            screenRect = Rect.MinMaxRect(
                Mathf.Min(first.x, second.x),
                Mathf.Min(first.y, second.y),
                Mathf.Max(first.x, second.x),
                Mathf.Max(first.y, second.y));
            return true;
        }

        /// <summary>
        /// 取消物品拖拽并复位拖影与本体位置
        /// </summary>
        public void CancelDrag()
        {
            FinishDrag();
        }

        /// <summary>
        /// 旋转当前拖拽中的物品拖影
        /// </summary>
        /// <returns>是否处于可旋转的拖拽状态</returns>
        public bool TryRotateDuringDrag()
        {
            if (!m_isDragging) return false;

            RotateGhostDuringDrag();
            return true;
        }

        /// <summary>
        /// 右键取消由其他按键发起的拖拽, 右键自身发起的拖拽保持原行为
        /// </summary>
        /// <returns>是否取消了本次拖拽</returns>
        public bool TryCancelDragBySecondaryButton()
        {
            if (!m_isDragging || m_dragButton == PointerEventData.InputButton.Right) return false;

            FinishDrag();
            return true;
        }

        /// <summary>
        /// 拖拽悬停时按指针重排拖影, 并算出落点锚格相对悬停目标的偏移
        /// </summary>
        /// <param name="eventData">当前指针事件</param>
        /// <param name="target">被悬停格子或物品的 RectTransform</param>
        /// <param name="targetCell">被悬停目标自身的格号</param>
        /// <returns>落点锚格相对目标格的偏移, 非拖拽状态返回零</returns>
        public static Vector2Int ResolveDropAnchor(PointerEventData eventData, RectTransform target,
            Vector2Int targetCell)
        {
            if (eventData == null || eventData.pointerDrag == null) return Vector2Int.zero;
            if (!eventData.pointerDrag.TryGetComponent(out UIInventoryItem ghost)) return Vector2Int.zero;
            if (!ghost.m_isDragging) return Vector2Int.zero;

            ghost.m_hoverTargetCell = targetCell;
            ghost.TrackHoveredContainer(target);
            ghost.SyncGhost();

            return ghost.m_anchorCell - targetCell;
        }

        #region UI事件
        // 按下只作废上一手势的拖拽标记, 详情与菜单一律等抬起再判定, 拖拽起点不会误触
        public void OnPointerDown(PointerEventData eventData) => m_dragLiftedThisGesture = false;

        // 首次抬起只起延迟计时, 双击窗口内没有第二击才弹详情; 窗口内来了第二击就按双击直接走业务
        public void OnPointerClick(PointerEventData eventData)
        {
            // 未搜索物品的首击只用于揭晓当前格, 不进入详情、菜单或快速收纳
            if (m_isSearching)
            {
                if (eventData.button == PointerEventData.InputButton.Left)
                    SetSearching(false);

                return;
            }

            // 拖拽落点不算点击, 否则每拖完一件物品都会顺手弹一次详情
            if (m_dragLiftedThisGesture) return;

            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    // 配对窗口和详情延迟协程走同一块真实时钟, 缩放时间被改动时两头不会错开
                    if (m_doubleClick.TryClick(InstanceId, Time.unscaledTime, out bool isDoubleClick) && isDoubleClick)
                    {
                        // 双击打断: 掐掉第一击还没到点的延迟, 详情全程没机会画出来
                        StopDetailReveal();

                        // 浮层先收起再动手: 收走最后一件会把整张开箱载体连同它的视图模型一起销毁,
                        // 事后再收就打在已释放的信号上
                        m_viewModel.ClosePopups();

                        // 开箱载体里的物品是"收进背包", 玩家自己身上的才是"丢到地面"
                        if (m_viewModel.IsLootContainer)
                            m_viewModel.TryQuickMoveToOwnContainer(InstanceId);
                        else
                            UIInventoryHotkey.TryDropItemAtPlayer(m_viewModel.ContainerId, InstanceId);

                        break;
                    }

                    ScheduleDetailReveal();
                    break;
                case PointerEventData.InputButton.Right:
                    // 菜单已经按用户意图弹出来, 之前那次左键的详情延迟就作废, 否则到点会把菜单顶掉
                    StopDetailReveal();
                    m_viewModel.OpenMenuAt(m_origin);
                    break;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (m_isSearching) return;

            m_previewCell = m_origin + ResolveDropAnchor(eventData, m_rect, m_origin);
            m_viewModel.PreviewDropAt(m_previewCell);
        }

        // 按上次上报的锚点格撤销预览, 居中后锚点可能不等于自身格号
        public void OnPointerExit(PointerEventData eventData)
        {
            if (m_isSearching) return;

            m_viewModel.ClearDropPreview(m_previewCell);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (m_isSearching) return;

            // 同一次拖拽的重复回调只认第一次, 否则会叠加出第二个嵌套 Canvas 把裁切彻底弄丢
            if (m_isDragging || m_isPendingDrag) return;

            m_isPendingDrag = true;
            m_dragButton = eventData.button;
            m_pendingDragStart = eventData.position;
            m_dragPointerPosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (m_isSearching) return;

            m_dragPointerPosition = eventData.position;

            if (m_isDragging || !m_isPendingDrag) return;
            if ((eventData.position - m_pendingDragStart).sqrMagnitude < DRAG_LIFT_THRESHOLD_SQR) return;

            LiftGhost(eventData);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (m_isSearching) return;
            if (eventData.pointerDrag == gameObject) return;

            // 落点与拖影当前位置同源, 避免预览格子与实际放置格子不一致
            m_viewModel.PlaceDraggedItemAt(m_origin + ResolveDropAnchor(eventData, m_rect, m_origin));
        }

        public void OnEndDrag(PointerEventData eventData) => FinishDrag();
        #endregion

        #region Private Function
        // 位移达到阈值才真正抬起: 登记拖拽状态、关掉射线并把拖影中心对到指针
        private void LiftGhost(PointerEventData eventData)
        {
            m_isPendingDrag = false;
            m_isDragging = true;
            m_dragLiftedThisGesture = true;
            StopDetailReveal();

            m_viewModel.BeginDragAt(m_origin);
            CaptureDragPlane(eventData);
            BeginDragGhost();
            SeedHoverFromOrigin();

            SyncGhost();
        }

        // 记录投影平面与相机: 拖影在物品层所在平面上跟随指针, Overlay 画布下相机为空
        private void CaptureDragPlane(PointerEventData eventData)
        {
            m_dragPlaneRect = m_rect.parent as RectTransform;
            m_dragCamera = eventData.pressEventCamera;
        }

        // 抬起瞬间用源容器预置跟随参数, 保证首帧就按指针摆好拖影并算出锚点格
        private void SeedHoverFromOrigin()
        {
            m_anchorCell = m_origin;
            m_hoverTargetCell = m_origin;
            m_ghostFootprint = m_cellFootprint;
            m_hoverStep = m_cellSize + m_cellSpacing;
            m_hoverLayer = m_dragPlaneRect;
            m_hoverCarrier = null;
            m_hoverCell = m_cellSize;
            m_hoverSpacing = m_cellSpacing;
            m_hoverIsEquipSlot = IsOwnContainerEquipSlot;

            if (m_viewModel == null) return;

            InventoryContainerVO vo = m_viewModel.Snapshot.Value;
            m_hoverGridCount = new Vector2Int(vo.Width, vo.Height);
        }

        // 指针投影到物品层, 折算成相对首格左上角的偏移, 向下为负
        private bool TryPointerOffset(RectTransform layer, out Vector2 pointerOffset)
        {
            pointerOffset = Vector2.zero;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, m_dragPointerPosition,
                m_dragCamera, out Vector2 local)) return false;

            pointerOffset = DragAnchorCentering.PointerOffset(layer.rect, local);
            return true;
        }

        // 每帧同步拖影: 本体让中心贴着指针连续移动, 锚点格再折算到格阵, 高亮与落点始终同源
        private void SyncGhost()
        {
            // 悬停容器解析失败时退回源物品层, 拖影继续跟手, 落点就是悬停格本身
            RectTransform layer = m_hoverLayer != null ? m_hoverLayer : m_dragPlaneRect;
            if (layer == null) return;
            if (!TryPointerOffset(layer, out Vector2 pointerOffset)) return;

            Vector2 ghostOffset = DragAnchorCentering.GhostOffset(pointerOffset, m_rect.rect.size);
            PlaceGhostAtOffset(layer, ghostOffset);

            if (m_hoverLayer == null)
            {
                m_anchorCell = m_hoverTargetCell;
                return;
            }

            m_anchorCell = DragAnchorCentering.ClampAnchor(
                DragAnchorCentering.LatticeAnchor(ghostOffset, m_hoverStep), m_ghostFootprint, m_hoverGridCount);
        }

        // 把拖影左上角落到物品层的连续偏移上, 只按矩形自身角点折算, 与枢轴和缩放无关
        private void PlaceGhostAtOffset(RectTransform layer, Vector2 ghostOffset)
        {
            Rect layerRect = layer.rect;
            Vector3 cornerWorld = layer.TransformPoint(
                new Vector3(layerRect.xMin + ghostOffset.x, layerRect.yMax + ghostOffset.y, 0f));
            Rect rect = m_rect.rect;
            Vector3 pivotToCorner = m_rect.TransformPoint(new Vector3(rect.xMin, rect.yMax, 0f)) - m_rect.position;

            m_rect.position = cornerWorld - pivotToCorner;
        }

        // 悬停换容器会改写拖影尺寸, 落点也要跟着重算, 因此每帧渲染前再同步一次
        private void LateUpdate()
        {
            if (!m_isDragging) return;

            SyncGhost();
        }

        private void BeginDragGhost()
        {
            m_dragOriginSize = m_rect.sizeDelta;

            // 全程复用同一个 Canvas, 只用 overrideSorting 开关拖影层级, 结束时就地交还裁切权
            if (m_dragCanvas == null && !TryGetComponent(out m_dragCanvas))
                m_dragCanvas = gameObject.AddComponent<Canvas>();

            m_dragCanvas.overrideSorting = true;
            m_dragCanvas.sortingOrder = DRAG_GHOST_SORTING_ORDER;

            ApplyPresentation();

            m_canvasGroup.alpha = DRAG_GHOST_ALPHA;
            m_canvasGroup.blocksRaycasts = false;
        }

        private void TrackHoveredContainer(RectTransform target)
            => ApplyHoveredContainer(target != null ? target.GetComponentInParent<UIInventoryCarrier>() : null);

        // 记录悬停容器的物品层、格阵与占格, 并把拖影尺寸与图标朝向换成该容器的配置
        private void ApplyHoveredContainer(UIInventoryCarrier targetCarrier)
        {
            Vector2Int resolved = targetCarrier != null ? ResolveTargetFootprint(targetCarrier) : Vector2Int.zero;

            // 容器或占格无法解析时交回自由跟随, 落点就是悬停格本身
            if (targetCarrier == null || targetCarrier.ItemLayer == null || resolved.x <= 0 || resolved.y <= 0)
            {
                m_hoverCarrier = null;
                m_hoverIsEquipSlot = false;
                m_hoverLayer = null;
                m_anchorCell = m_hoverTargetCell;
                ApplyGhostIconRotation();
                return;
            }

            Vector2 cell = targetCarrier.CellSize;
            Vector2 spacing = targetCarrier.CellSpacing;
            m_rect.sizeDelta = FootprintSize(resolved, cell, spacing);

            m_hoverCarrier = targetCarrier;
            m_hoverCell = cell;
            m_hoverSpacing = spacing;
            m_hoverIsEquipSlot = targetCarrier.ViewModel != null &&
                InventoryContainerIds.TryGetEquipSlotRole(targetCarrier.ViewModel.ContainerId, out _);
            m_hoverLayer = targetCarrier.ItemLayer;
            m_hoverStep = cell + spacing;
            m_hoverGridCount = targetCarrier.GridCount;
            m_ghostFootprint = resolved;

            ApplyGhostIconRotation();
        }

        //装备槽里的物品按 1x1 展示, 其余容器按物品配置的真实占格
        private Vector2Int ResolveTargetFootprint(UIInventoryCarrier carrier)
        {
            if (carrier.ViewModel != null &&
                InventoryContainerIds.TryGetEquipSlotRole(carrier.ViewModel.ContainerId, out _))
                return Vector2Int.one;

            if (m_viewModel != null && m_viewModel.Snapshot.Value.TryGetItem(InstanceId, out InventoryItemVO item) &&
                ItemRegistry.TryGet(item.ItemId, out InventoryItemConfigSO config))
                return config.GetSizeAt(DraggedRotation);

            return Vector2Int.zero;
        }

        // 按配置的单元格尺寸算物品占格的外接矩形
        private Vector2 FootprintSize(Vector2Int size) => FootprintSize(size, m_cellSize, m_cellSpacing);

        // 按指定格参数算占格外接矩形, 供跨容器拖影复用
        private static Vector2 FootprintSize(Vector2Int size, Vector2 cell, Vector2 spacing)
        {
            return new Vector2(
                size.x * cell.x + (size.x - 1) * spacing.x,
                size.y * cell.y + (size.y - 1) * spacing.y);
        }

        // 图标按未旋转时的外形铺满占格, 换向交给 Z 轴整体旋转, 避免非等比拉伸
        private void ApplyIconGeometry(Vector2Int footprint, InventoryRotation rotation, Vector2 cell, Vector2 spacing)
        {
            RectTransform rect = m_icon.rectTransform;

            rect.anchorMin = m_centerAnchor;
            rect.anchorMax = m_centerAnchor;
            rect.pivot = m_centerAnchor;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = FootprintSize(UnrotateFootprint(footprint, rotation), cell, spacing);
            rect.localEulerAngles = new Vector3(0f, 0f, -(int)rotation * 90f);
        }

        // 奇数朝向时占格是外形宽高互换, 还原成旋转前的外形
        private static Vector2Int UnrotateFootprint(Vector2Int footprint, InventoryRotation rotation)
            => (int)rotation % 2 == 1 ? new Vector2Int(footprint.y, footprint.x) : footprint;

        // 拖影图标朝向跟随拖拽中的朝向, 落进单格装备槽时摆正
        private void ApplyGhostIconRotation()
        {
            if (m_ghostFootprint.x <= 0 || m_ghostFootprint.y <= 0) return;

            InventoryRotation display = m_hoverIsEquipSlot ? InventoryRotation.None : DraggedRotation;
            ApplyIconGeometry(m_ghostFootprint, display, m_hoverCell, m_hoverSpacing);
        }

        // 拖拽中推进朝向后重算拖影外形与图标朝向
        private void RotateGhostDuringDrag()
        {
            if (m_viewModel == null || !m_viewModel.RotateDraggedItem()) return;

            ApplyHoveredContainer(m_hoverCarrier);
        }

        // 按配置的单元格尺寸算物品左上角锚点位置
        private Vector2 CellOrigin(Vector2Int origin)
        {
            return DragAnchorCentering.CellCornerOffset(origin, m_cellSize + m_cellSpacing);
        }

        private void EndDragGhost()
        {
            m_rect.sizeDelta = m_dragOriginSize;
            ApplyIconGeometry(m_cellFootprint, IsOwnContainerEquipSlot ? InventoryRotation.None : m_modelRotation,
                m_cellSize, m_cellSpacing);
            ApplyPresentation();

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
            // 未达阈值的按下不算拖拽, 抬起流程没走过就只需清待命状态
            if (m_isPendingDrag)
            {
                m_isPendingDrag = false;
                return;
            }

            if (!m_isDragging) return;

            m_isDragging = false;
            EndDragGhost();
            m_rect.anchoredPosition = m_homeAnchoredPosition;

            // 源容器可能在落点回调里被当场收掉, 视图模型已释放就不要再往属性里写
            if (!m_viewModel.IsDisposed) m_viewModel.CancelDrag();
        }

        private void RenderSelected(int selectedInstanceId)
        {
            m_selectedFrame.SetActive(!m_isSearching && selectedInstanceId == InstanceId);
        }

        // 物品底板、图标、数量、选中框与搜索阴影的唯一显隐入口
        private void ApplyPresentation()
        {
            bool showItem = !m_isSearching && !m_isDragging;

            m_background.enabled = !m_isDragging;
            m_background.color = m_isSearching ? Color.clear : m_baseColor;
            m_icon.enabled = showItem && m_icon.sprite != null;
            m_countText.enabled = showItem && !string.IsNullOrEmpty(m_countText.text);
            m_silhouette.enabled = m_isSearching && !m_isDragging;

            int selectedInstanceId = m_viewModel != null ? m_viewModel.SelectedInstanceId.Value : NO_SELECTION;
            RenderSelected(selectedInstanceId);
        }

        // 首次左键抬起只记下目标物品并起延迟, 到点仍没有第二击才真的请求详情
        private void ScheduleDetailReveal()
        {
            StopDetailReveal();
            m_pendingDetail = StartCoroutine(RevealDetailCoroutine(m_origin, InstanceId));
        }

        // 掐掉未到点的详情延迟: 双击、抬成拖拽、换物品、右键菜单、载体销毁都要收
        private void StopDetailReveal()
        {
            if (m_pendingDetail == null) return;

            StopCoroutine(m_pendingDetail);
            m_pendingDetail = null;
        }

        // 延迟到双击窗口结束再开详情, 期间物品可能已被别的入口收走, 迟到就作废
        private IEnumerator RevealDetailCoroutine(Vector2Int origin, int instanceId)
        {
            yield return m_detailRevealDelay;

            m_pendingDetail = null;

            // 载体销毁会让视图模型先走, 换绑又可能把这件视图派给别的物品
            if (m_viewModel.IsDisposed || InstanceId != instanceId) yield break;

            m_viewModel.InteractAt(origin);
        }
        #endregion
    }
}
