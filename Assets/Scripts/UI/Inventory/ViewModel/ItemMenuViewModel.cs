/*
* ┌──────────────────────────────────┐
* │  描    述: 物品右键菜单视图模型, 汇总旋转拆分丢弃的可用性并转发操作请求
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
        public IReadOnlyReactiveProperty<bool> CanSplit => m_canSplit;
        public IReadOnlyReactiveProperty<bool> CanDrop => m_canDrop;
        public ISignal Dismissed => m_dismissed;
        public ISignal SplitRequested => m_splitRequested;
        public ISignal DropRequested => m_dropRequested;

        // 拆分弹窗沿用菜单当前锁定的操作对象
        public int TargetContainerId => m_containerId;
        public int TargetInstanceId => m_instanceId;

        private readonly IInventoryModel m_model;
        private readonly ReactiveProperty<bool> m_canRotate;
        private readonly ReactiveProperty<bool> m_canSplit;
        private readonly ReactiveProperty<bool> m_canDrop;
        private readonly Signal m_dismissed;
        private readonly Signal m_splitRequested;
        private readonly Signal m_dropRequested;

        private int m_containerId;
        private int m_instanceId = NO_ITEM;

        public ItemMenuViewModel() : this(MvvmRoot.Instance.Models.Get<IInventoryModel>())
        {
        }

        public ItemMenuViewModel(IInventoryModel model)
        {
            m_model = model;

            m_canRotate = CreateProperty(false);
            m_canSplit = CreateProperty(false);
            m_canDrop = CreateProperty(false);
            m_dismissed = CreateSignal();
            m_splitRequested = CreateSignal();
            m_dropRequested = CreateSignal();
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
            RefreshActions();
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

        // 请求拆分当前物品, 菜单收起并打开拆分弹窗
        public void Split()
        {
            if (m_instanceId == NO_ITEM) return;

            m_splitRequested.Fire();
            m_dismissed.Fire();
        }

        // 请求丢弃当前物品: 落点与登记都不在视图模型里, 交给运行期宿主, 菜单这里只收起
        public void Drop()
        {
            if (m_instanceId == NO_ITEM) return;

            m_dropRequested.Fire();
            m_dismissed.Fire();
        }
        #endregion

        #region Private Function
        // 一次查询同时刷新三项操作的可用性, 避免重复走容器与配置表
        private void RefreshActions()
        {
            bool canRotate = false;
            bool canSplit = false;
            bool canDrop = false;

            if (m_model.IsContainerRegistered(m_containerId) &&
                m_model.GetContainerVO(m_containerId).TryGetItem(m_instanceId, out InventoryItemVO item))
            {
                canDrop = true;

                if (ItemRegistry.TryGet(item.ItemId, out InventoryItemConfigSO config))
                    canRotate = config.CanRotate;

                // 只剩一份时无可拆分的余量, 拆分按钮置灰
                canSplit = item.StackCount > 1;
            }

            m_canRotate.Set(canRotate);
            m_canSplit.Set(canSplit);
            m_canDrop.Set(canDrop);
        }
        #endregion
    }
}
