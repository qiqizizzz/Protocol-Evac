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

        //弹窗当前归属哪个容器, 只有它自己的点击与拖拽有权把弹窗收走
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

                //弹窗显隐只由面板编排, 载体只管发出「我要看详情」「我要收弹窗」的请求
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

        //战备面板收起时弹窗一并走, 否则按 B 关掉面板后详情还挂在屏幕上
        protected override void OnHide()
        {
            m_popupOwner = null;
            ClosePopups();
        }
        #endregion

        #region Private Function
        //左键详情: 详情与菜单同一时间只留一张
        private void OpenDetailPopup(InventoryViewModel container)
        {
            m_popupOwner = container;

            //预制体首次加载要等一帧, 这期间重复请求直接丢弃, 否则窗口服务会被同键注册两次
            if (m_detailOpening) return;
            m_detailOpening = true;

            HidePopup<UIItemMenuPanel>();
            ShowDetailAsync(container).Forget();
        }

        //右键菜单: 视图模型已经在 OpenMenuAt 里把该物品置为选中
        private void OpenMenuPopup(InventoryViewModel container)
        {
            m_popupOwner = container;
            if (m_menuOpening) return;
            m_menuOpening = true;

            HidePopup<UIItemDetailPanel>();
            ShowMenuAsync(container).Forget();
        }

        private async UniTaskVoid ShowDetailAsync(InventoryViewModel container)
        {
            await MvvmRoot.Instance.Windows.ShowAsync<UIItemDetailPanel>(container.ContainerId,
                container.DetailInstanceId.Value, Input.mousePosition);

            m_detailOpening = false;
        }

        private async UniTaskVoid ShowMenuAsync(InventoryViewModel container)
        {
            await MvvmRoot.Instance.Windows.ShowAsync<UIItemMenuPanel>(container.ContainerId,
                container.SelectedInstanceId.Value, Input.mousePosition);

            m_menuOpening = false;
        }

        //反选、点空白、起拖都只收起属于自己那个容器的弹窗
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
        }

        //没打开过就不动它, Windows.Hide 在未打开时会报错
        private static void HidePopup<TView>() where TView : class, IView
        {
            if (MvvmRoot.Instance.Windows.TryGet(out TView popup)) popup.Hide();
        }
        #endregion
    }
}
