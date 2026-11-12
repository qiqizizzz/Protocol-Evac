/*
 * ┌──────────────────────────────────┐
 * │  描    述: 战备面板视图, 装配固定载体与右侧动态物资箱载体并注入对应子视图模型
 * │  类    名: UIEquipmentPanel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Cysharp.Threading.Tasks;
using Framework.QTower.MVVM.Binding;
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
        //右侧物资箱独立列节点路径, 打开的物资箱载体只挂在这里
        private const string LOOT_ROOT_PATH = "Layout_Loot";
        //左侧独立装备槽节点路径
        private const string EQUIPMENT_SLOTS_PATH = "Layout_EquipmentSlots";

        //动态物资箱载体的节点名, 只为在层级里认得出来
        private const string LOOT_CARRIER_NAME = "Carrier_Loot";

        [Header("载体设置")]
        [SerializeField] private UIInventoryCarrier[] Containers;

        [Header("物资箱载体")]
        [Tooltip("右侧动态物资箱载体的模板, 必须常驻停用, 开箱时按它实例化")]
        [SerializeField] private UIInventoryCarrier LootCarrierTemplate;

        private ScrollRect m_capacityScroll;
        private RectTransform m_lootRoot;
        private RectTransform m_equipmentSlots;
        private UIInventoryCarrier m_lootCarrier; //当前挂在右侧的物资箱载体, 未开箱时为空

        // 记录弹窗当前归属容器, 仅该容器的点击与拖拽可隐藏弹窗
        private InventoryViewModel m_popupOwner;
        private bool m_detailOpening;
        private bool m_menuOpening;

        // 弹窗代号: 每次收起都推进它, 首次加载还没回来的打开请求据此作废
        private int m_popupGeneration;

        #region 生命周期
        protected override EquipmentPanelViewModel CreateViewModel() => new EquipmentPanelViewModel();

        protected override void OnAwake()
        {
            m_capacityScroll = FindComponent<ScrollRect>(SCROLL_VIEW_PATH);
            m_lootRoot = FindComponent<RectTransform>(LOOT_ROOT_PATH);
            m_equipmentSlots = FindComponent<RectTransform>(EQUIPMENT_SLOTS_PATH);

            if (LootCarrierTemplate != null && LootCarrierTemplate.gameObject.activeSelf)
                QLog.Error($"{nameof(LootCarrierTemplate)} 模板处于激活态, 会在右侧列里白占一块, 请停用该节点");

            if (m_lootRoot == null)
                QLog.Error($"右侧物资箱列未找到, 开箱载体无处可挂, 请检查节点 {LOOT_ROOT_PATH}");
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
                BindPopupRequests(Scope, container);
            }

            Scope.Bind(viewModel.LootContainerChanged, RefreshLootCarrier);
            RefreshLootCarrier();
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
        // 载体的详情、菜单与关闭请求统一由面板编排, 订阅挂在谁的作用域上就随谁收敛
        private void BindPopupRequests(BindingScope scope, InventoryViewModel container)
        {
            scope.Bind(container.DetailRequested, () => OpenDetailPopup(container));
            scope.Bind(container.MenuRequested, () => OpenMenuPopup(container));
            scope.Bind(container.PopupClosed, () => CloseOwnedPopups(container));
        }

        // 物资箱开关只换载体不换模型容器: 先收掉旧载体, 再按当前视图模型决定挂不挂新的
        private void RefreshLootCarrier()
        {
            DestroyLootCarrier();

            InventoryViewModel container = ViewModel.LootContainer;
            if (container == null) return;

            if (LootCarrierTemplate == null || m_lootRoot == null)
            {
                QLog.Error($"未配置 {nameof(LootCarrierTemplate)} 或节点 {LOOT_ROOT_PATH}, 物资箱载体无法显示");
                return;
            }

            GameObject carrierObject = Instantiate(LootCarrierTemplate.gameObject, m_lootRoot);
            carrierObject.name = LOOT_CARRIER_NAME;

            m_lootCarrier = carrierObject.GetComponent<UIInventoryCarrier>();
            Scope.CreateChild(carrierObject);

            // 模板常驻停用, 克隆体同样不会触发 Awake, 先激活补齐载体的节点缓存再显示, 否则绑定会打在空引用上
            carrierObject.SetActive(true);
            m_lootCarrier.Show(container);

            // 动态载体随开随毁, 订阅挂在它自己的作用域上, 销毁时一并收敛, 不给面板留悬空条目
            BindPopupRequests(m_lootCarrier.Scope, container);
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_lootRoot);
        }

        // 停用立即生效以先收敛绑定, 真正的销毁 Unity 排在帧末
        private void DestroyLootCarrier()
        {
            if (m_lootCarrier == null) return;

            GameObject carrierObject = m_lootCarrier.gameObject;
            m_lootCarrier = null;

            carrierObject.SetActive(false);
            Destroy(carrierObject);
        }

        // 左键详情：详情与菜单同时只保留一个
        private void OpenDetailPopup(InventoryViewModel container)
        {
            m_popupOwner = container;

            // 预制体首次加载需等待一帧, 期间的重复请求忽略, 避免同一键重复注册
            if (m_detailOpening) return;
            m_detailOpening = true;

            HidePopup<UIItemMenuPanel>();
            ShowDetailAsync(container, m_popupGeneration).Forget();
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

        // 详情窗口首次打开要等实例化回来, 期间可能被双击打断, 靠代号判断这次还要不要显示
        private async UniTaskVoid ShowDetailAsync(InventoryViewModel container, int generation)
        {
            if (!TryGetItemScreenRect(container, out Rect itemScreenRect, out bool preferLeft))
            {
                m_detailOpening = false;
                return;
            }

            await MvvmRoot.Instance.Windows.ShowAsync<UIItemDetailPanel>(container.ContainerId,
                container.DetailInstanceId.Value, itemScreenRect, preferLeft);

            m_detailOpening = false;

            // 等待期间浮层已被收起: 窗口刚注册就立刻收走, 不留一双击就赖在屏幕上的残影
            if (generation != m_popupGeneration) HidePopup<UIItemDetailPanel>();
        }

        private async UniTaskVoid ShowMenuAsync(InventoryViewModel container)
        {
            await MvvmRoot.Instance.Windows.ShowAsync<UIItemMenuPanel>(container.ContainerId,
                container.SelectedInstanceId.Value, Input.mousePosition);

            m_menuOpening = false;
        }

        // 详情定位以物品完整占格为准, 与鼠标落在物品内的具体位置无关
        private bool TryGetItemScreenRect(InventoryViewModel container, out Rect itemScreenRect,
            out bool preferLeft)
        {
            itemScreenRect = default;
            preferLeft = false;

            UIInventoryCarrier carrier = FindCarrier(container);
            if (carrier == null)
            {
                QLog.Error($"详情物品所属载体不存在, 容器 {container.ContainerId}");
                return false;
            }

            // 左侧装备槽的详情放左边, 右侧容量区与物资箱放右边
            preferLeft = carrier.transform.IsChildOf(m_equipmentSlots);
            if (carrier.TryGetItemScreenRect(container.DetailInstanceId.Value, out itemScreenRect))
                return true;

            QLog.Error($"详情物品视图不存在, 容器 {container.ContainerId}, 实例 {container.DetailInstanceId.Value}");
            return false;
        }

        // 反查容器对应的载体, 固定载体找不到再看动态物资箱载体
        private UIInventoryCarrier FindCarrier(InventoryViewModel container)
        {
            for (int i = 0; i < Containers.Length; i++)
                if (ReferenceEquals(Containers[i].ViewModel, container)) return Containers[i];

            if (m_lootCarrier != null && ReferenceEquals(m_lootCarrier.ViewModel, container)) return m_lootCarrier;

            return null;
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
            m_popupGeneration++;
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
