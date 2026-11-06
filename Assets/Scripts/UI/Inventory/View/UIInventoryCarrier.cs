/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包载体视图, 表头加网格层加物品层的单容器面板
 * │  类    名: UIInventoryCarrier.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.MVVM.View;
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
        private const string CAPACITY_PATH = "Layout_Header/Txt_Capacity";
        private const string NAME_PATH = "Layout_Header/Txt_Name";

        private const int MIN_COLUMN_COUNT = 1;

        [Header("模板设置")]
        [Tooltip("格子模板")]
        [SerializeField] private GameObject SlotPrefab;

        [Tooltip("物品模板")]
        [SerializeField] private GameObject ItemPrefab;

        private Transform m_gridRoot;
        private Transform m_itemRoot;
        private GameObject m_visibleRoot;
        private GridLayoutGroup m_gridLayout;
        private TMP_Text m_capacityText;
        private TMP_Text m_nameText;

        private readonly List<UIInventorySlot> m_slotViews = new List<UIInventorySlot>();
        private readonly Dictionary<int, UIInventoryItem> m_itemViews = new Dictionary<int, UIInventoryItem>();
        private readonly List<int> m_staleItemIds = new List<int>();

        private int m_gridWidth = -1;

        #region 生命周期
        protected override InventoryViewModel CreateViewModel() => new InventoryViewModel();

        protected override void OnAwake()
        {
            Transform containerRoot = transform.Find(CONTAINER_PATH);
            if (containerRoot != null)
            {
                m_gridRoot = this.FindChild(CONTAINER_GRID_PATH);
                m_itemRoot = this.FindChild(CONTAINER_ITEM_PATH);
                m_visibleRoot = containerRoot.gameObject;
            }
            else
            {
                m_gridRoot = this.FindChild(GRID_PATH);
                m_itemRoot = this.FindChild(ITEM_PATH);
                m_visibleRoot = gameObject;
            }

            m_gridLayout = m_gridRoot.GetComponent<GridLayoutGroup>();
            m_gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            m_nameText = transform.Find(NAME_PATH)?.GetComponent<TMP_Text>();
            m_capacityText = transform.Find(CAPACITY_PATH)?.GetComponent<TMP_Text>();
        }

        //登记绑定, 视图只做表现不做业务判定
        protected override void BindViewModel(InventoryViewModel viewModel)
        {
            Scope.Bind(viewModel.Snapshot, RenderSnapshot);
            Scope.Bind(viewModel.Visible, SetVisible);
        }
        #endregion

        #region Private Function
        //快照驱动网格层与物品层
        private void RenderSnapshot(InventoryContainerVO vo)
        {
            SyncSlotViews(vo);
            SyncItemViews(vo);

            if (m_nameText != null)
                m_nameText.text = vo.DisplayName;

            if (m_capacityText != null)
                m_capacityText.text = $"{vo.UsedCellCount}/{vo.CellCount}";
        }

        // 容量视图只显隐容器区域，避免连带隐藏独立装备槽
        private void SetVisible(bool visible)
        {
            m_visibleRoot.SetActive(visible);
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
                    itemView.Setup(item);
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

            itemView.Setup(item);
            m_itemViews.Add(item.InstanceId, itemView);
            itemView.Bind(Scope.CreateChild(itemObject), ViewModel);
        }

        //提示语义转文案
        private static string HintOf(InventoryHintKind kind)
        {
            switch (kind)
            {
                case InventoryHintKind.Selected: return "已选中, 点击格子放置";
                case InventoryHintKind.Placed: return "已放置";
                case InventoryHintKind.Failed: return "放不下";
                case InventoryHintKind.Desync: return "数据异常";
                default: return string.Empty;
            }
        }
        #endregion
    }
}
