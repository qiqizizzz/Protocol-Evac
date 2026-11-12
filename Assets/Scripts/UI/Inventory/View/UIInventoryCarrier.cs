/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包载体视图, 表头加网格层加物品层的单容器面板
 * │  类    名: UIInventoryCarrier.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.MVVM.View;
using Module.Inventory.Core.Defines;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Find;

namespace UI.Inventory.View
{
    public sealed class UIInventoryCarrier : ViewBase<InventoryViewModel>
    {
        private const string CONTAINER_PATH = "Slot_Container";
        private const string CONTAINER_GRID_PATH = "Slot_Container/Layout_Grid";
        private const string CONTAINER_ITEM_PATH = "Slot_Container/Layout_Item";
        private const string GRID_PATH = "Layout_Grid";
        private const string ITEM_PATH = "Layout_Item";
        private const string HEADER_PATH = "Layout_Header";
        private const string EQUIP_SLOT_PATH = "Slot_Equipment";
        private const string VECTOR_ICON_PATH = "Img_Icon";
        private const string LOCK_ICON_PATH = "Img_locked";
        private const string CAPACITY_PATH = "Layout_Header/Txt_Capacity";
        private const string NAME_PATH = "Layout_Header/Txt_Name";
        private const string INFO_BUTTON_PATH = "Layout_Header/Btn_Info";

        private const int MIN_COLUMN_COUNT = 1;
        private const float HEADER_GAP = 8f; //表头底边到网格顶边的间距
        private const float BOTTOM_GAP = 8f; //网格底边到载体底边的间距

        [Header("模板设置")]
        [Tooltip("格子模板")]
        [SerializeField] private GameObject SlotPrefab;

        [Tooltip("物品模板")]
        [SerializeField] private GameObject ItemPrefab;

        [Header("表头设置")]
        [Tooltip("容器表头固定显示的名称, 留空则沿用预制体 Txt_Name 的原文案")]
        [SerializeField] private string DefaultName;

        [Header("尺寸设置")]
        [Tooltip("载体宽度是否跟随网格收合, 右侧开箱载体靠它把表头压到和网格一样宽")]
        [SerializeField] private bool AutoSizeWidth;

        private Transform m_gridRoot;
        private Transform m_itemRoot;
        private GameObject m_visibleRoot;
        private GameObject m_vectorIcon; //装备槽根节点下的矢量底图, 容量载体没有该节点
        private GameObject m_lockIcon; //安全箱装备槽的锁定标记, 普通装备槽没有该节点
        private GridLayoutGroup m_gridLayout;
        private TMP_Text m_capacityText;
        private TMP_Text m_nameText;
        private GameObject m_infoButton; //表头右侧的说明按钮, 独立装备槽没有该节点

        private string m_prefabName; //表头在预制体里本来写着的文案

        private RectTransform m_rect;
        private RectTransform m_gridRect;
        private RectTransform m_itemRect;
        private RectTransform m_containerRect; //容量载体的网格外框, 独立装备槽没有该节点
        private RectTransform m_headerRect; //表头, 独立装备槽没有该节点
        private RectTransform m_equipSlotRect; //载体内嵌的装备槽, 有无由穿戴配置决定

        private readonly List<UIInventorySlot> m_slotViews = new List<UIInventorySlot>();
        private readonly Dictionary<int, UIInventoryItem> m_itemViews = new Dictionary<int, UIInventoryItem>();
        private readonly List<int> m_staleItemIds = new List<int>();

        private int m_gridWidth = -1;

        /// <summary>
        /// 当前载体的网格单元格尺寸
        /// </summary>
        public Vector2 CellSize => m_gridLayout.cellSize;

        /// <summary>
        /// 当前载体的网格单元格间距
        /// </summary>
        public Vector2 CellSpacing => m_gridLayout.spacing;

        /// <summary>
        /// 物品所在的物品层, 拖影跟随与锚点格折算以此为基准
        /// </summary>
        public RectTransform ItemLayer => m_itemRect;

        /// <summary>
        /// 读取指定物品完整占格在屏幕空间中的矩形
        /// </summary>
        /// <param name="instanceId">物品实例号</param>
        /// <param name="screenRect">物品完整占格的屏幕矩形</param>
        public bool TryGetItemScreenRect(int instanceId, out Rect screenRect)
        {
            screenRect = default;
            if (!m_itemViews.TryGetValue(instanceId, out UIInventoryItem itemView)) return false;

            return itemView.TryGetScreenRect(out screenRect);
        }

        /// <summary>
        /// 取消当前载体里正在拖拽的物品视图
        /// </summary>
        /// <returns>载体里是否有正在拖拽的物品</returns>
        public bool TryCancelDraggedItem()
        {
            if (ViewModel == null || !ViewModel.IsDragging) return false;

            int instanceId = ViewModel.DraggingInstanceId;
            if (m_itemViews.TryGetValue(instanceId, out UIInventoryItem itemView))
            {
                itemView.CancelDrag();
                return true;
            }

            ViewModel.CancelDrag();
            return true;
        }

        /// <summary>
        /// 当前载体的网格格数, 视图模型未就绪时返回零
        /// </summary>
        public Vector2Int GridCount
        {
            get
            {
                if (ViewModel == null) return Vector2Int.zero;

                InventoryContainerVO vo = ViewModel.Snapshot.Value;
                return new Vector2Int(vo.Width, vo.Height);
            }
        }

        #region 生命周期
        protected override InventoryViewModel CreateViewModel() => new InventoryViewModel();

        protected override void OnAwake()
        {
            m_rect = (RectTransform)transform;
            Transform containerRoot = transform.Find(CONTAINER_PATH);
            if (containerRoot != null)
            {
                m_gridRoot = this.FindChild(CONTAINER_GRID_PATH);
                m_itemRoot = this.FindChild(CONTAINER_ITEM_PATH);
                m_containerRect = (RectTransform)containerRoot;
                m_visibleRoot = containerRoot.gameObject;
            }
            else
            {
                m_gridRoot = this.FindChild(GRID_PATH);
                m_itemRoot = this.FindChild(ITEM_PATH);
                m_visibleRoot = gameObject;
            }

            m_gridRect = (RectTransform)m_gridRoot;
            m_itemRect = (RectTransform)m_itemRoot;
            m_gridLayout = m_gridRoot.GetComponent<GridLayoutGroup>();
            m_gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            //物品层按首格左上角原点摆放, 网格层必须左上对齐才能与物品层重合
            m_gridLayout.childAlignment = TextAnchor.UpperLeft;

            m_headerRect = transform.Find(HEADER_PATH) as RectTransform;
            m_equipSlotRect = transform.Find(EQUIP_SLOT_PATH) as RectTransform;
            m_vectorIcon = transform.Find(VECTOR_ICON_PATH)?.gameObject;
            m_lockIcon = transform.Find(LOCK_ICON_PATH)?.gameObject;
            m_nameText = transform.Find(NAME_PATH)?.GetComponent<TMP_Text>();
            m_capacityText = transform.Find(CAPACITY_PATH)?.GetComponent<TMP_Text>();
            m_infoButton = transform.Find(INFO_BUTTON_PATH)?.gameObject;
            ResolvePrefabName();

            SyncItemLayerToGrid();
        }

        //登记绑定, 视图只做表现不做业务判定
        protected override void BindViewModel(InventoryViewModel viewModel)
        {
            Scope.Bind(viewModel.Snapshot, RenderSnapshot);
            Scope.Bind(viewModel.Visible, SetVisible);
        }
        #endregion

        #region Private Function
        //快照驱动网格尺寸与两层视图
        private void RenderSnapshot(InventoryContainerVO vo)
        {
            SyncGridSize(vo);
            SyncSlotViews(vo);
            SyncItemViews(vo);
            RenderVectorIcon(vo);
            RenderLockIcon(vo);
            RenderName(vo);
            RenderHeaderTools(vo);
        }

        // 表头的容量与说明按钮只服务玩家自己的容器, 开箱载体只是借这块网格摆物品, 一律收起
        private void RenderHeaderTools(InventoryContainerVO vo)
        {
            bool isLootContainer = ViewModel.IsLootContainer;

            if (m_capacityText != null)
            {
                m_capacityText.gameObject.SetActive(!isLootContainer);
                if (!isLootContainer)
                    m_capacityText.text = $"{vo.UsedCellCount}/{vo.CellCount}";
            }

            if (m_infoButton != null)
                m_infoButton.SetActive(!isLootContainer);
        }

        // 槽位有物品时隐藏矢量底图, 空槽显示占位图标
        private void RenderVectorIcon(InventoryContainerVO vo)
        {
            if (m_vectorIcon == null) return;

            m_vectorIcon.SetActive(vo.Items.Count == 0);
        }

        //安全箱装备槽在对局内保持锁定表现
        private void RenderLockIcon(InventoryContainerVO vo)
        {
            if (m_lockIcon == null) return;

            m_lockIcon.SetActive(vo.ContainerId == InventoryContainerIds.SAFEBOX_SLOT);
        }

        //表头名优先用载体上写死的覆盖名, 其次跟随模型给的容器显示名, 动态物资箱就是靠这一路拿到箱名
        private void RenderName(InventoryContainerVO vo)
        {
            if (m_nameText == null) return;

            if (!string.IsNullOrEmpty(DefaultName))
            {
                m_nameText.text = DefaultName;
                return;
            }

            m_nameText.text = string.IsNullOrEmpty(vo.DisplayName) ? m_prefabName : vo.DisplayName;
        }

        //记下预制体原始文案, 供没有名字的快照回落
        private void ResolvePrefabName()
        {
            m_prefabName = m_nameText != null ? m_nameText.text : string.Empty;
        }

        // 容量视图只显隐容器区域，避免连带隐藏独立装备槽
        private void SetVisible(bool visible)
        {
            m_visibleRoot.SetActive(visible);
        }

        //按容器拓扑换算网格矩形并撑合载体, 让尺寸始终来自配置而不是手调预制体
        private void SyncGridSize(InventoryContainerVO vo)
        {
            bool hasGrid = vo.Width >= MIN_COLUMN_COUNT && vo.Height > 0;
            if (!hasGrid)
            {
                //空容量载体仍要由表头和装备槽撑开, 否则后续载体表头会压到装备槽上
                if (m_containerRect != null)
                {
                    m_rect.sizeDelta = new Vector2(m_rect.sizeDelta.x,
                        GridTopOffset() + EquipSlotHeight() + BOTTOM_GAP);
                    RebuildLayout();
                }

                return;
            }

            //格子边长与间距的唯一入口是 Layout_Grid 上的 GridLayoutGroup, 想改尺寸改这两处而不是根节点 RectTransform
            Vector2 cell = m_gridLayout.cellSize;
            Vector2 spacing = m_gridLayout.spacing;
            RectOffset padding = m_gridLayout.padding;
            float gridWidth = padding.left + padding.right + cell.x * vo.Width + spacing.x * (vo.Width - 1);
            float gridHeight = padding.top + padding.bottom + cell.y * vo.Height + spacing.y * (vo.Height - 1);

            if (m_containerRect == null)
            {
                //独立装备槽: 外框等于网格本身, 不再保留第二套尺寸
                m_rect.sizeDelta = new Vector2(gridWidth, gridHeight);
            }
            else
            {
                m_containerRect.anchoredPosition = new Vector2(m_containerRect.anchoredPosition.x, -GridTopOffset());
                m_containerRect.sizeDelta = new Vector2(gridWidth, gridHeight);
                // 收合宽度只在开关打开时生效, 左侧背包列继续用预制体里写死的宽度
                float carrierWidth = AutoSizeWidth ? gridWidth : m_rect.sizeDelta.x;
                m_rect.sizeDelta = new Vector2(carrierWidth, GridTopOffset() + Mathf.Max(gridHeight, EquipSlotHeight()) + BOTTOM_GAP);
            }

            RebuildLayout();
        }

        //容量载体的尺寸变化需要同步刷新自身与父布局, 保证后续载体立即重新排布
        private void RebuildLayout()
        {
            LayoutRebuilder.MarkLayoutForRebuild(m_gridRect);
            if (transform.parent is RectTransform parentRect)
                LayoutRebuilder.MarkLayoutForRebuild(parentRect);
        }

        //物品层收缩为网格的内缩区域, 原点钉在首格左上角, 保证物品永远压在格子上
        private void SyncItemLayerToGrid()
        {
            RectOffset padding = m_gridLayout.padding;
            m_itemRect.anchorMin = m_gridRect.anchorMin;
            m_itemRect.anchorMax = m_gridRect.anchorMax;
            m_itemRect.pivot = m_gridRect.pivot;
            m_itemRect.sizeDelta = m_gridRect.sizeDelta - new Vector2(padding.horizontal, padding.vertical);
            m_itemRect.anchoredPosition = m_gridRect.anchoredPosition + new Vector2(padding.left, -padding.top);
        }

        //网格顶边相对载体顶边的下沉量, 由表头高度推导
        private float GridTopOffset()
        {
            float headerHeight = m_headerRect != null ? m_headerRect.rect.height : 0f;
            return headerHeight + HEADER_GAP;
        }

        //载体内装备槽占的行高, 没有该节点或已停用时不占位
        private float EquipSlotHeight()
        {
            if (m_equipSlotRect == null || !m_equipSlotRect.gameObject.activeSelf) return 0f;
            return m_equipSlotRect.rect.height;
        }

        //按容器拓扑增减格子视图
        private void SyncSlotViews(InventoryContainerVO vo)
        {
            if (vo.Width != m_gridWidth)
            {
                ClearSlotViews();
                m_gridWidth = vo.Width;
                m_gridLayout.constraintCount = Mathf.Max(MIN_COLUMN_COUNT, vo.Width);
            }

            while (m_slotViews.Count < vo.SlotCount)
                AddSlotView(m_slotViews.Count);

            while (m_slotViews.Count > vo.SlotCount)
                RemoveLastSlotView();
        }

        //实例化格子视图并挂到父作用域下的子作用域
        private void AddSlotView(int index)
        {
            GameObject slotObject = Instantiate(SlotPrefab, m_gridRoot);
            UIInventorySlot slotView = slotObject.GetComponent<UIInventorySlot>();

            Scope.CreateChild(slotObject);
            m_slotViews.Add(slotView);
            slotView.Show(ViewModel.GetSlot(index));
        }

        //回收末尾格子视图
        private void RemoveLastSlotView()
        {
            int last = m_slotViews.Count - 1;
            UIInventorySlot slotView = m_slotViews[last];
            m_slotViews.RemoveAt(last);

            Destroy(slotView.gameObject);
        }

        //清空全部格子视图
        private void ClearSlotViews()
        {
            for (int i = m_slotViews.Count - 1; i >= 0; i--)
                Destroy(m_slotViews[i].gameObject);

            m_slotViews.Clear();
        }

        //按快照增删改物品视图
        private void SyncItemViews(InventoryContainerVO vo)
        {
            m_staleItemIds.Clear();
            m_staleItemIds.AddRange(m_itemViews.Keys);

            for (int i = 0; i < vo.Items.Count; i++)
            {
                InventoryItemVO item = vo.Items[i];

                if (m_itemViews.TryGetValue(item.InstanceId, out UIInventoryItem itemView))
                {
                    itemView.Setup(item, m_gridLayout.cellSize, m_gridLayout.spacing);
                    m_staleItemIds.Remove(item.InstanceId);
                    continue;
                }

                AddItemView(item);
            }

            for (int i = 0; i < m_staleItemIds.Count; i++)
            {
                UIInventoryItem itemView = m_itemViews[m_staleItemIds[i]];
                m_itemViews.Remove(m_staleItemIds[i]);

                Destroy(itemView.gameObject);
            }
        }

        //实例化物品视图并按子作用域绑定所在面板
        private void AddItemView(InventoryItemVO item)
        {
            GameObject itemObject = Instantiate(ItemPrefab, m_itemRoot);
            UIInventoryItem itemView = itemObject.GetComponent<UIInventoryItem>();

            itemView.Setup(item, m_gridLayout.cellSize, m_gridLayout.spacing);
            m_itemViews.Add(item.InstanceId, itemView);
            itemView.Bind(Scope.CreateChild(itemObject), ViewModel);
        }

        //提示语义转文案
        private static string HintOf(InventoryHintKind kind)
        {
            switch (kind)
            {
                case InventoryHintKind.Selected: return "已选中, 可拖拽移动";
                case InventoryHintKind.Placed: return "已放置";
                case InventoryHintKind.Failed: return "放不下";
                case InventoryHintKind.Desync: return "数据异常";
                default: return string.Empty;
            }
        }
        #endregion
    }
}
