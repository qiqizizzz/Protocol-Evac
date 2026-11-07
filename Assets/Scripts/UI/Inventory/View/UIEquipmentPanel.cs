/*
 * ┌──────────────────────────────────┐
 * │  描    述: 战备面板视图, 同屏装配容量载体与独立装备槽并注入对应子视图模型
 * │  类    名: UIEquipmentPanel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.View;
using UnityEngine;
using UnityEngine.UI;
using Utils.log;

namespace UI.Inventory.View
{
    public sealed class UIEquipmentPanel : ViewBase<EquipmentPanelViewModel>
    {
        //容量载体滚动视图节点路径
        private const string SCROLL_VIEW_PATH = "Layout_Storage/ScrollView_Capacity";

        [Header("载体设置")]
        [SerializeField] private UIInventoryCarrier[] Containers;

        private ScrollRect m_capacityScroll;

        #region 生命周期
        protected override EquipmentPanelViewModel CreateViewModel() => new EquipmentPanelViewModel();

        protected override void OnAwake()
        {
            m_capacityScroll = FindComponent<ScrollRect>(SCROLL_VIEW_PATH);
        }

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

        //打开面板时容量区会被布局重新撑高, 滚动位置必须归位到顶部, 否则第一个载体整块落在视口外
        protected override void OnShow(params object[] args)
        {
            Canvas.ForceUpdateCanvases();
            m_capacityScroll.verticalNormalizedPosition = 1f;
        }
        #endregion
    }
}
