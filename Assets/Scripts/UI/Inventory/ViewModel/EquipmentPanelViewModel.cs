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
        // 物资箱载体增删变化, 视图据此重建动态载体
        public ISignal LootContainerChanged => m_lootContainerChanged;

        public InventoryViewModel DragSource => m_dragSource;
        private readonly InventoryViewModel[] m_containers;
        private readonly IInventoryModel m_model;
        private readonly Signal m_lootContainerChanged;
        private InventoryViewModel m_lootContainer;
        private InventoryViewModel m_dragSource; 

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
            if (m_lootContainer != null && m_lootContainer.ContainerId == containerId) return;

            if (!m_model.IsContainerRegistered(containerId))
            {
                QLog.Error($"打开物资箱载体失败, 容器未注册：{containerId}");
                return;
            }

            DisposeLootContainer();

            m_lootContainer = new InventoryViewModel(m_model, containerId, this);
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

            // 从开箱载体上搬走最后一件就该收箱, 否则右侧会一直挂着一张空壳网格
            CloseLootContainerIfExhausted(source.ContainerId);
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
        }

        /// <summary>
        /// 开箱载体跟着容器走: 只有当前开着的那只容器被搬空或已被收回, 才关掉右侧那一列
        /// </summary>
        /// <param name="containerId">刚被取走物品的容器号</param>
        public void CloseLootContainerIfExhausted(int containerId)
        {
            if (m_lootContainer == null || m_lootContainer.ContainerId != containerId) return;

            if (m_model.IsContainerRegistered(containerId) &&
                m_model.GetContainerVO(containerId).Items.Count > 0) return;

            CloseLootContainer();
        }
        #endregion
    }
}
