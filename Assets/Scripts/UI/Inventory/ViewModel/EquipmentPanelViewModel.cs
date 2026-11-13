/*
 * ┌──────────────────────────────────┐
 * │  描    述: 战备面板视图模型, 统管固定载体与右侧动态物资箱载体的子视图模型
 * │  类    名: EquipmentPanelViewModel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.MVVM.View;
using Framework.QTower.MVVM.ViewModel;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using Module.Inventory.Core.Defines;
using Module.Inventory.Core.Rule.Type;
using Module.Inventory.Core.Rule.Extensions;
using UnityEngine;
using Utils.log;

namespace UI.Inventory
{
    public class EquipmentPanelViewModel : ViewModelBase
    {
        public IReadOnlyList<InventoryViewModel> Containers => m_containers;
        public int ContainerCount => m_containers.Length;

        // 右侧动态物资箱载体, 为空表示当前没有打开任何箱子
        public InventoryViewModel LootContainer => m_lootContainer;
        // 当前物资箱是否等待玩家逐件搜索
        public bool LootContainerSearching => m_lootContainerSearching;
        // 物资箱载体增删变化, 视图据此重建动态载体
        public ISignal LootContainerChanged => m_lootContainerChanged;

        public InventoryViewModel DragSource => m_dragSource;
        private readonly InventoryViewModel[] m_containers;
        private readonly IInventoryModel m_model;
        private readonly Signal m_lootContainerChanged;
        private InventoryViewModel m_lootContainer;
        private InventoryViewModel m_dragSource;
        private bool m_lootContainerSearching;

        public EquipmentPanelViewModel()
            : this(MvvmRoot.Instance.Models.Get<IInventoryModel>(), InventoryContainerIds.EQUIPMENT_CONTAINER_IDS)
        {
        }

        /// <summary>
        /// 按槽位号逐个建出容器子视图模型, 子 VM 的生死由本面板统管
        /// </summary>
        /// <param name="model">背包模型</param>
        /// <param name="containerIds">角色容器槽位号, 顺序即载体顺序</param>
        public EquipmentPanelViewModel(IInventoryModel model, int[] containerIds)
        {
            m_model = model;
            m_containers = new InventoryViewModel[containerIds.Length];
            for (int i = 0; i < containerIds.Length; i++)
                m_containers[i] = new InventoryViewModel(model, containerIds[i], this);

            m_lootContainerChanged = CreateSignal();
        }

        public InventoryViewModel GetContainer(int index) => m_containers[index];

        /// <summary>
        /// 打开一只物资箱: 建出它的动态子视图模型并通知视图挂载体, 重复打开同一只箱子不重建
        /// </summary>
        /// <param name="containerId">物资箱容器号</param>
        public void OpenLootContainer(int containerId)
        {
            OpenLootContainer(containerId, false);
        }

        /// <summary>
        /// 打开一只物资箱并按入口决定是否先显示未搜索阴影
        /// </summary>
        /// <param name="containerId">物资箱容器号</param>
        /// <param name="requiresSearch">是否等待玩家搜索物品</param>
        public void OpenLootContainer(int containerId, bool requiresSearch)
        {
            if (m_lootContainer != null && m_lootContainer.ContainerId == containerId) return;

            if (!m_model.IsContainerRegistered(containerId))
            {
                QLog.Error($"打开物资箱载体失败, 容器未注册：{containerId}");
                return;
            }

            DisposeLootContainer();

            m_lootContainer = new InventoryViewModel(m_model, containerId, this);
            m_lootContainerSearching = requiresSearch;
            m_lootContainerChanged.Fire();
        }

        // 关闭物资箱: 只销毁子视图模型, 容器与内容留在模型里等下次开箱
        public void CloseLootContainer()
        {
            if (m_lootContainer == null) return;

            DisposeLootContainer();
            m_lootContainerChanged.Fire();
        }

        //子面板拖起物品
        public void NotifyDragBegan(InventoryViewModel source) => m_dragSource = source;

        //子面板结束拖拽
        public void NotifyDragEnded(InventoryViewModel source)
        {
            if (ReferenceEquals(m_dragSource, source)) m_dragSource = null;
        }

        //物品落到别的容器上
        public void HandleCrossContainerDrop(InventoryViewModel target, Vector2Int cell)
        {
            InventoryViewModel source = m_dragSource;
            if (source == null || ReferenceEquals(source, target)) return;

            InventoryMoveResult result = m_model.TryTransferItem(source.ContainerId, target.ContainerId,
                source.DraggingInstanceId, cell, source.DragRotation);
            source.ApplyTransferResult(result);

            // 地面堆被搬空会当场销毁, 右侧那列跟着收掉; 物资箱仍然在册, 载体保持打开
            CloseLootContainerIfRemoved(source.ContainerId);
        }

        /// <summary>
        /// 把开箱载体里的物品收进玩家自己的容量容器, 按候选顺序找第一块放得下的空格
        /// </summary>
        /// <param name="sourceContainerId">物品当前所在的开箱容器号</param>
        /// <param name="instanceId">要收纳的物品实例号</param>
        /// <returns>实际采用的转移结果, 候选全塞不下时返回最后一次的失败</returns>
        public InventoryMoveResult TryStowLootItem(int sourceContainerId, int instanceId)
        {
            int[] targetIds = InventoryContainerIds.QUICK_STOW_CONTAINER_IDS;
            InventoryMoveResult result = InventoryMoveResult.Occupied;

            for (int i = 0; i < targetIds.Length; i++)
            {
                if (!m_model.IsContainerRegistered(targetIds[i])) continue;

                result = m_model.TryTransferItemAuto(sourceContainerId, targetIds[i], instanceId);
                if (!result.IsSuccessOrSame()) continue;

                CloseLootContainerIfRemoved(sourceContainerId);
                return result;
            }

            return result;
        }

        /// <summary>
        /// 开箱载体跟着容器走: 只有当前开着的那只容器已被收回, 才关掉右侧那一列
        /// 物资箱被搬空仍然保留容器, 载体要留着继续塞东西; 地面堆掏空即销毁, 才跟着收场
        /// </summary>
        /// <param name="containerId">刚被取走物品的容器号</param>
        public void CloseLootContainerIfRemoved(int containerId)
        {
            if (m_lootContainer == null || m_lootContainer.ContainerId != containerId) return;
            if (m_model.IsContainerRegistered(containerId)) return;

            CloseLootContainer();
        }

        #region 生命周期
        protected override void OnDisposeCore()
        {
            for (int i = 0; i < m_containers.Length; i++)
            {
                m_containers[i].OnUnbind();
                m_containers[i].OnDispose();
                m_containers[i].Dispose();
            }

            DisposeLootContainer();
        }
        #endregion

        #region Private Function
        // 收掉动态物资箱的子视图模型, 顺带清理它可能持有的拖拽身份
        private void DisposeLootContainer()
        {
            if (m_lootContainer == null) return;

            if (ReferenceEquals(m_dragSource, m_lootContainer)) m_dragSource = null;

            m_lootContainer.OnUnbind();
            m_lootContainer.OnDispose();
            m_lootContainer.Dispose();
            m_lootContainer = null;
            m_lootContainerSearching = false;
        }
        #endregion
    }
}
