/*
 * ┌──────────────────────────────────┐
 * │  描    述: 战备面板视图, 同屏装配容量载体与独立装备槽并注入对应子视图模型
 * │  类    名: UIEquipmentPanel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.MVVM.View;
using Module.Inventory.Core.Defines;
using UnityEngine;
using Utils.log;

namespace UI.Inventory.View
{
    public sealed class UIEquipmentPanel : ViewBase<EquipmentPanelViewModel>
    {
        [Header("载体设置")]
        [SerializeField] private UIInventoryCarrier[] Containers;

        #region 生命周期
        protected override EquipmentPanelViewModel CreateViewModel() => new EquipmentPanelViewModel();

        protected override void BindViewModel(EquipmentPanelViewModel viewModel)
        {
            UIInventoryCarrier[] allContainers = GetAllContainers(viewModel);
            if (allContainers == null)
                return;

            if (allContainers.Length != viewModel.ContainerCount)
            {
                QLog.Error($"载体数量与视图模型不符: 预制体 {allContainers.Length} 个, 面板 {viewModel.ContainerCount} 个");
                return;
            }

            for (int i = 0; i < allContainers.Length; i++)
            {
                Scope.CreateChild(allContainers[i].gameObject);
                allContainers[i].Show(viewModel.GetContainer(i));
            }
        }

        //读取各容量载体预制体中固定配置的独立装备槽
        private UIInventoryCarrier[] GetAllContainers(EquipmentPanelViewModel viewModel)
        {
            List<UIInventoryCarrier> result = new List<UIInventoryCarrier>(Containers);
            for (int i = 0; i < Containers.Length; i++)
            {
                Transform slotTransform = Containers[i].transform.Find("Slot_Equipment");
                if (slotTransform != null)
                    slotTransform.gameObject.SetActive(false);
            }

            int[] capacityContainerIds = { InventoryContainerIds.RIG, InventoryContainerIds.BACKPACK, InventoryContainerIds.SAFEBOX };
            for (int i = 0; i < capacityContainerIds.Length; i++)
            {
                int containerIndex = FindContainerIndex(viewModel, capacityContainerIds[i]);
                if (containerIndex < 0)
                    return null;

                Transform slotTransform = Containers[containerIndex].transform.Find("Slot_Equipment");
                UIInventoryCarrier slot = slotTransform == null ? null : slotTransform.GetComponent<UIInventoryCarrier>();
#if UNITY_EDITOR
                if (slot == null)
                    QLog.Error($"容量载体缺少固定独立装备槽：容器 {capacityContainerIds[i]}");
#endif
                if (slot == null)
                    return null;

                slot.gameObject.SetActive(true);
                result.Add(slot);
            }

            return result.ToArray();
        }

        //按容器编号查找面板载体索引
        private static int FindContainerIndex(EquipmentPanelViewModel viewModel, int containerId)
        {
            for (int i = 0; i < viewModel.ContainerCount; i++)
            {
                if (viewModel.GetContainer(i).ContainerId == containerId)
                    return i;
            }

            QLog.Error($"找不到容量容器：{containerId}");
            return -1;
        }
        #endregion
    }
}
