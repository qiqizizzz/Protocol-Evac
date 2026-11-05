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
            if (Containers.Length > viewModel.ContainerCount)
            {
                QLog.Error($"载体数量与视图模型不符: 预制体 {Containers.Length} 个, 面板 {viewModel.ContainerCount} 个");
                return;
            }

            List<UIInventoryCarrier> runtimeContainers = new List<UIInventoryCarrier>(Containers);
            for (int i = runtimeContainers.Count; i < viewModel.ContainerCount; i++)
            {
                UIInventoryCarrier source = Containers[i % Containers.Length];
                Transform carrier = ResolveExtraSlotCarrier(viewModel.GetContainer(i).ContainerId);
                if (carrier == null) return;

                UIInventoryCarrier clone = Instantiate(source, transform);
                clone.name = $"Slot_{viewModel.GetContainer(i).ContainerId}";
                RectTransform rect = clone.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(1f, 1f);
                rect.sizeDelta = new Vector2(104f, 104f);
                Canvas.ForceUpdateCanvases();
                rect.position = carrier.position + new Vector3(-8f, -40f, 0f);
                runtimeContainers.Add(clone);
            }

            for (int i = 0; i < runtimeContainers.Count; i++)
            {
                Scope.CreateChild(runtimeContainers[i].gameObject);
                runtimeContainers[i].Show(viewModel.GetContainer(i));
            }
        }

        //按容量载体查找独立装备槽的挂载位置
        private Transform ResolveExtraSlotCarrier(int containerId)
        {
            string carrierName = containerId switch
            {
                InventoryContainerIds.CHESTRIG_SLOT => "Carrier_Rig",
                InventoryContainerIds.BACKPACK_SLOT => "Carrier_Backpack",
                InventoryContainerIds.SAFEBOX_SLOT => "Carrier_SafeBox",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(carrierName))
                return null;

            Transform carrier = transform.Find($"ScrollView_Capacity/Viewport/Content/{carrierName}");
            if (carrier != null) return carrier;

            if (containerId == InventoryContainerIds.SAFEBOX_SLOT)
                return transform.Find("Carrier_SafeBox");

            QLog.Error($"找不到独立装备槽挂载位置：{carrierName}");
            return null;
        }
        #endregion
    }
}
