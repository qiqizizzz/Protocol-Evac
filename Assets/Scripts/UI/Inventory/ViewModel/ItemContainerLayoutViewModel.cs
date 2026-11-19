/*
 * ┌──────────────────────────────────┐
 * │  描    述: 容器内部布局视图模型, 按容器形状与区域生成完整占格快照
 * │  类    名: ItemContainerLayoutViewModel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Framework.QTower.MVVM.View;
using Framework.QTower.MVVM.ViewModel;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using Module.Inventory.Core.Config;
using Utils.log;

namespace UI.Inventory
{
    public sealed class ItemContainerLayoutViewModel : ViewModelBase
    {
        private const int NO_ITEM = -1;

        public IReadOnlyReactiveProperty<string> Title => m_title;
        public IReadOnlyReactiveProperty<ItemFootprintPreviewVO> Preview => m_preview;
        public ISignal Dismissed => m_dismissed;

        private readonly IInventoryModel m_model;
        private readonly ReactiveProperty<string> m_title;
        private readonly ReactiveProperty<ItemFootprintPreviewVO> m_preview;
        private readonly Signal m_dismissed;

        private int m_containerId;
        private int m_instanceId = NO_ITEM;

        public ItemContainerLayoutViewModel() : this(MvvmRoot.Instance.Models.Get<IInventoryModel>())
        {
        }

        public ItemContainerLayoutViewModel(IInventoryModel model)
        {
            m_model = model;
            m_title = CreateProperty(string.Empty);
            m_preview = CreateProperty(ItemFootprintPreviewVO.Empty);
            m_dismissed = CreateSignal();
        }

        #region 生命周期
        protected override void OnBindCore()
        {
            m_model.OnChanged += HandleModelChanged;
        }

        protected override void OnUnbindCore()
        {
            m_model.OnChanged -= HandleModelChanged;
        }
        #endregion

        #region Public Function
        /// <summary>
        /// 打开指定物品自身容器的完整内部布局
        /// </summary>
        /// <param name="containerId">物品所在容器号</param>
        /// <param name="instanceId">物品实例号</param>
        public void Open(int containerId, int instanceId)
        {
            m_containerId = containerId;
            m_instanceId = instanceId;
            Refresh();
        }

        // 点击遮罩或关闭按钮时收起独立布局窗口
        public void Dismiss() => m_dismissed.Fire();
        #endregion

        #region Private Function
        private void HandleModelChanged(int containerId)
        {
            if (containerId != m_containerId || m_instanceId == NO_ITEM) return;

            Refresh();
        }

        // 物品消失或配置不再是容器时自动收起窗口
        private void Refresh()
        {
            if (!m_model.IsContainerRegistered(m_containerId))
            {
                m_dismissed.Fire();
                return;
            }

            InventoryContainerVO source = m_model.GetContainerVO(m_containerId);
            if (!source.TryGetItem(m_instanceId, out InventoryItemVO item))
            {
                m_dismissed.Fire();
                return;
            }

            if (!ItemRegistry.TryGet(item.ItemId, out InventoryItemConfigSO config) ||
                !config.IsContainer || config.Container == null)
            {
                QLog.Error($"物品没有可展示的容器定义：{item.ItemId}");
                m_dismissed.Fire();
                return;
            }

            InventoryContainerConfigSO container = config.Container;
            int width = Math.Max(1, container.Width);
            int height = Math.Max(1, container.Height);
            IReadOnlyList<bool> mask = container.Mask;
            IReadOnlyList<int> regionIds = container.RegionIds;

            m_title.Set(string.IsNullOrWhiteSpace(item.DisplayName) ? container.DisplayName : item.DisplayName);
            m_preview.Set(BuildPreview(width, height, mask, regionIds));
        }

        // 把容器掩码与区域表转换成窗口绘制所需的逐格四边快照
        private static ItemFootprintPreviewVO BuildPreview(int width, int height, IReadOnlyList<bool> mask,
            IReadOnlyList<int> regionIds)
        {
            int cellCount = width * height;
            bool[] solidCells = new bool[cellCount];
            InventorySlotEdgeKind[] topEdges = new InventorySlotEdgeKind[cellCount];
            InventorySlotEdgeKind[] rightEdges = new InventorySlotEdgeKind[cellCount];
            InventorySlotEdgeKind[] bottomEdges = new InventorySlotEdgeKind[cellCount];
            InventorySlotEdgeKind[] leftEdges = new InventorySlotEdgeKind[cellCount];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    int regionId = index < regionIds.Count ? regionIds[index] : -1;
                    bool solid = index < mask.Count && mask[index] && regionId >= 0;
                    solidCells[index] = solid;
                    if (!solid) continue;

                    topEdges[index] = InventoryContainerVO.ResolveEdgeKind(
                        width, height, mask, regionIds, x, y, 0, -1, regionId);
                    rightEdges[index] = InventoryContainerVO.ResolveEdgeKind(
                        width, height, mask, regionIds, x, y, 1, 0, regionId);
                    bottomEdges[index] = InventoryContainerVO.ResolveEdgeKind(
                        width, height, mask, regionIds, x, y, 0, 1, regionId);
                    leftEdges[index] = InventoryContainerVO.ResolveEdgeKind(
                        width, height, mask, regionIds, x, y, -1, 0, regionId);
                }
            }

            return new ItemFootprintPreviewVO(true, width, height, solidCells, topEdges, rightEdges,
                bottomEdges, leftEdges);
        }
        #endregion
    }
}
