/*
* ┌──────────────────────────────────┐
* │  描    述: 物品右键菜单视图模型
* │  类    名: ItemMenuViewModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Framework.QTower.MVVM.View;
using Framework.QTower.MVVM.ViewModel;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Rule.Extensions;
using Module.Inventory.Core.Rule.Type;
using Utils.log;

namespace UI.Inventory
{
    public class ItemMenuViewModel : ViewModelBase
    {
        private const int NO_ITEM = -1; //当前没有要操作的物品

        public IReadOnlyReactiveProperty<bool> CanRotate => m_canRotate;
        public ISignal Dismissed => m_dismissed;

        private readonly IInventoryModel m_model;
        private readonly ReactiveProperty<bool> m_canRotate;
        private readonly Signal m_dismissed;

        private int m_containerId;
        private int m_instanceId = NO_ITEM;

        public ItemMenuViewModel() : this(MvvmRoot.Instance.Models.Get<IInventoryModel>())
        {
        }

        public ItemMenuViewModel(IInventoryModel model)
        {
            m_model = model;

            m_canRotate = CreateProperty(false);
            m_dismissed = CreateSignal();
        }

        #region Public Function
        /// <summary>
        /// 打开某容器里一件物品的菜单
        /// </summary>
        /// <param name="containerId">物品所在容器号</param>
        /// <param name="instanceId">物品实例号</param>
        public void Open(int containerId, int instanceId)
        {
            m_containerId = containerId;
            m_instanceId = instanceId;
            RefreshCanRotate();
        }

        // 点击空白处收起菜单, 不执行任何操作
        public void Dismiss() => m_dismissed.Fire();

        // 旋转物品, 无论成功与否都关闭菜单, 失败原因由日志输出
        public void Rotate()
        {
            if (m_instanceId == NO_ITEM) return;

            InventoryMoveResult result = m_model.TryRotateItem(m_containerId, m_instanceId);
            if (!result.IsSuccessOrSame()) QLog.Warning($"物品旋转被拒：{result}");

            m_dismissed.Fire();
        }
        #endregion

        #region Private Function
        private void RefreshCanRotate()
        {
            bool canRotate = false;

            if (m_model.IsContainerRegistered(m_containerId) &&
                m_model.GetContainerVO(m_containerId).TryGetItem(m_instanceId, out InventoryItemVO item) &&
                ItemRegistry.TryGet(item.ItemId, out InventoryItemConfigSO config))
                canRotate = config.CanRotate;

            m_canRotate.Set(canRotate);
        }
        #endregion
    }
}
