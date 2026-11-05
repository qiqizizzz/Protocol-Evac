/*
 * ┌──────────────────────────────────┐
 * │  描    述: 战备面板视图模型, 统管六个装备槽与四个容量载体的子视图模型
 * │  类    名: EquipmentPanelViewModel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.MVVM.View;
using Framework.QTower.MVVM.ViewModel;
using Module.Inventory.Core.Defines;
using Module.Inventory.Core.Rule.Type;
using UnityEngine;

namespace UI.Inventory
{
    public class EquipmentPanelViewModel : ViewModelBase
    {
        public IReadOnlyList<InventoryViewModel> Containers => m_containers;
        public int ContainerCount => m_containers.Length;

        public InventoryViewModel DragSource => m_dragSource;
        private readonly InventoryViewModel[] m_containers;
        private readonly IInventoryModel m_model;
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
        }

        public InventoryViewModel GetContainer(int index) => m_containers[index];

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
                source.DraggingInstanceId, cell);
            source.ApplyTransferResult(result);
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
        }
        #endregion
    }
}