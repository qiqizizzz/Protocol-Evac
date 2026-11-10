/*
 * ┌──────────────────────────────────┐
 * │  描    述: 战备面板视图, 同屏装配容量载体与独立装备槽并注入对应子视图模型
 * │  类    名: UIEquipmentPanel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Cysharp.Threading.Tasks;
using Framework.QTower.MVVM.View;
using Framework.QTower.MVVM.View.Interfaces;
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

        // 记录弹窗当前归属容器, 仅该容器的点击与拖拽可隐藏弹窗
        private InventoryViewModel m_popupOwner;
        private bool m_detailOpening;
        private bool m_menuOpening;

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
                InventoryViewModel container = viewModel.GetContainer(i);

                Scope.CreateChild(Containers[i].gameObject);
                Containers[i].Show(container);

                // 弹窗显隐由面板统一编排, 载体只发出详情与关闭请求
                Scope.Bind(container.DetailRequested, () => OpenDetailPopup(container));
                Scope.Bind(container.MenuRequested, () => OpenMenuPopup(container));
                Scope.Bind(container.PopupClosed, () => CloseOwnedPopups(container));
            }
        }

        //打开面板时容量区会被布局重新撑高, 滚动位置必须归位到顶部, 否则第一个载体整块落在视口外
        protected override void OnShow(params object[] args)
        {
            Canvas.ForceUpdateCanvases();
            m_capacityScroll.verticalNormalizedPosition = 1f;
        }

        // 战备面板隐藏时同步隐藏弹窗, 避免面板关闭后弹窗残留
        protected override void OnHide()
        {
            m_popupOwner = null;
            ClosePopups();
        }
        #endregion

        #region Private Function
        // 左键详情：详情与菜单同时只保留一个
        private void OpenDetailPopup(InventoryViewModel container)
        {
            m_popupOwner = container;

            // 预制体首次加载需等待一帧, 期间的重复请求忽略, 避免同一键重复注册
            if (m_detailOpening) return;
            m_detailOpening = true;

            HidePopup<UIItemMenuPanel>();
            ShowDetailAsync(container).Forget();
        }

        // 右键菜单：OpenMenuAt 已在视图模型中将该物品置为选中
        private void OpenMenuPopup(InventoryViewModel container)
        {
            m_popupOwner = container;
            if (m_menuOpening) return;
            m_menuOpening = true;

            HidePopup<UIItemDetailPanel>();
            HidePopup<UIItemSplitPanel>();
            ShowMenuAsync(container).Forget();
        }

        private async UniTaskVoid ShowDetailAsync(InventoryViewModel container)
        {
            if (!TryGetItemScreenRect(container, out Rect itemScreenRect))
            {
                m_detailOpening = false;
                return;
            }

            await MvvmRoot.Instance.Windows.ShowAsync<UIItemDetailPanel>(container.ContainerId,
                container.DetailInstanceId.Value, itemScreenRect);

            m_detailOpening = false;
        }

        private async UniTaskVoid ShowMenuAsync(InventoryViewModel container)
        {
            await MvvmRoot.Instance.Windows.ShowAsync<UIItemMenuPanel>(container.ContainerId,
                container.SelectedInstanceId.Value, Input.mousePosition);

            m_menuOpening = false;
        }

        // 详情定位以物品完整占格为准, 与鼠标落在物品内的具体位置无关
        private bool TryGetItemScreenRect(InventoryViewModel container, out Rect itemScreenRect)
        {
            itemScreenRect = default;

            for (int i = 0; i < Containers.Length; i++)
            {
                if (!ReferenceEquals(Containers[i].ViewModel, container)) continue;

                if (Containers[i].TryGetItemScreenRect(container.DetailInstanceId.Value, out itemScreenRect))
                    return true;

                QLog.Error($"详情物品视图不存在, 容器 {container.ContainerId}, 实例 {container.DetailInstanceId.Value}");
                return false;
            }

            QLog.Error($"详情物品所属载体不存在, 容器 {container.ContainerId}");
            return false;
        }

        // 反选、点击空白、开始拖拽时仅隐藏本容器的弹窗
        private void CloseOwnedPopups(InventoryViewModel container)
        {
            if (!ReferenceEquals(m_popupOwner, container)) return;

            m_popupOwner = null;
            ClosePopups();
        }

        private void ClosePopups()
        {
            HidePopup<UIItemDetailPanel>();
            HidePopup<UIItemMenuPanel>();
            HidePopup<UIItemSplitPanel>();
        }

        // 未打开时不处理, Windows.Hide 在窗口未打开时会报错
        private static void HidePopup<TView>() where TView : class, IView
        {
            if (MvvmRoot.Instance.Windows.TryGet(out TView popup)) popup.Hide();
        }
        #endregion
    }
}
