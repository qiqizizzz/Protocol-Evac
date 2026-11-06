/*
 * ┌──────────────────────────────────┐
 * │  描    述: 战备面板视图, 同屏装配容量载体与独立装备槽并注入对应子视图模型
 * │  类    名: UIEquipmentPanel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.View;
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
            if (Containers.Length != viewModel.ContainerCount)
            {
                QLog.Error($"载体数量与视图模型不符: 预制体 {Containers.Length} 个, 面板 {viewModel.ContainerCount} 个");
                return;
            }

            for (int i = 0; i < Containers.Length; i++)
            {
                Scope.CreateChild(Containers[i].gameObject);
                Containers[i].Show(viewModel.GetContainer(i));
            }
        }
        #endregion
    }
}
