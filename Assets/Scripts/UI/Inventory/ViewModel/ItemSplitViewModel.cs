/*
* ┌──────────────────────────────────┐
* │  描    述: 物品拆分视图模型, 维护拆出数量与滑条比例并驱动模型完成拆分
* │  类    名: ItemSplitViewModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Framework.QTower.MVVM.View;
using Framework.QTower.MVVM.ViewModel;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using Module.Inventory.Core.Rule.Type;
using UnityEngine;
using Utils.log;

namespace UI.Inventory
{
    public class ItemSplitViewModel : ViewModelBase
    {
        // ==================== 常量与静态字段 ====================
        private const int NO_ITEM = -1; //当前没有要拆分的物品
        private const int MIN_SPLIT_STACK = 2; //少于此数量就没有可拆分的余量
        private const int MIN_REMAIN_COUNT = 1; //原堆拆分后至少保留的数量
        private const float DEFAULT_RATIO = 0.5f; //打开弹窗时的默认拆分比例

        // ==================== 属性 ====================
        public IReadOnlyReactiveProperty<string> CountText => m_countText;
        public IReadOnlyReactiveProperty<float> Ratio => m_ratio;
        public ISignal Dismissed => m_dismissed;

        // ==================== 字段[私有] ====================
        private readonly IInventoryModel m_model;
        private readonly ReactiveProperty<string> m_countText;
        private readonly ReactiveProperty<float> m_ratio;
        private readonly Signal m_dismissed;

        private int m_containerId;
        private int m_instanceId = NO_ITEM;
        private int m_totalCount; //原堆当前数量
        private int m_splitCount; //本次要拆出的数量
        private int m_span; //可拆出数量的取值档数, 等于原堆数量减一
        private bool m_isConfirming; //正在执行拆分, 期间不因数量变化自动收起

        public ItemSplitViewModel() : this(MvvmRoot.Instance.Models.Get<IInventoryModel>())
        {
        }

        public ItemSplitViewModel(IInventoryModel model)
        {
            m_model = model;

            m_countText = CreateProperty(string.Empty);
            m_ratio = CreateProperty(DEFAULT_RATIO);
            m_dismissed = CreateSignal();
        }

        // ==================== 生命周期 ====================
        protected override void OnBindCore()
        {
            m_model.OnChanged += HandleModelChanged;
        }

        protected override void OnUnbindCore()
        {
            m_model.OnChanged -= HandleModelChanged;
        }

        // ==================== Public Function ====================
        /// <summary>
        /// 打开某容器里一件物品的拆分弹窗, 拆出数量默认取原堆的一半
        /// </summary>
        /// <param name="containerId">物品所在容器号</param>
        /// <param name="instanceId">物品实例号</param>
        public void Open(int containerId, int instanceId)
        {
            m_containerId = containerId;
            m_instanceId = instanceId;
            m_splitCount = 1;
            m_span = 1;
            Refresh();
            SetRatio(DEFAULT_RATIO);
        }

        // 点击空白处或取消按钮收起弹窗, 不执行拆分
        public void Dismiss() => m_dismissed.Fire();

        /// <summary>
        /// 拖动滑条设定拆出数量, 结果取整到整份后回写滑条位置
        /// </summary>
        /// <param name="ratio">滑条比例, 取值 0 到 1</param>
        public void SetRatio(float ratio)
        {
            int count = Mathf.CeilToInt(Mathf.Clamp01(ratio) * m_span);
            ApplyCount(Mathf.Clamp(count, 1, m_span));
        }

        /// <summary>
        /// 加减按钮推进拆出数量
        /// </summary>
        /// <param name="delta">数量增量, 加号传 1 减号传 -1</param>
        public void Step(int delta) => ApplyCount(Mathf.Clamp(m_splitCount + delta, 1, m_span));

        // 确认拆分, 无论成功与否都收起弹窗, 失败原因由日志输出
        public void Confirm()
        {
            if (m_instanceId == NO_ITEM) return;

            m_isConfirming = true;
            InventoryMoveResult result = m_model.TrySplitItem(m_containerId, m_instanceId, m_splitCount, out _);
            m_isConfirming = false;

            if (result != InventoryMoveResult.Success) QLog.Warning($"物品拆分被拒：{result}");

            m_dismissed.Fire();
        }

        // ==================== Private Function ====================
        private void HandleModelChanged(int containerId)
        {
            if (containerId != m_containerId || m_instanceId == NO_ITEM) return;

            Refresh();
        }

        // 从模型读取最新数量, 物品消失或不足两份时收起弹窗
        private void Refresh()
        {
            InventoryContainerVO vo = m_model.IsContainerRegistered(m_containerId)
                ? m_model.GetContainerVO(m_containerId)
                : InventoryContainerVO.Empty;

            if (!vo.TryGetItem(m_instanceId, out InventoryItemVO item) || item.StackCount < MIN_SPLIT_STACK)
            {
                // 确认拆分自身也会广播数量变化, 此时收起交给 Confirm 统一处理
                if (!m_isConfirming) m_dismissed.Fire();
                return;
            }

            m_totalCount = item.StackCount;
            m_span = Mathf.Max(m_totalCount - MIN_REMAIN_COUNT, 1);
            ApplyCount(Mathf.Clamp(m_splitCount, 1, m_span));
        }

        // 写入拆出数量并同步文案与滑条位置
        private void ApplyCount(int count)
        {
            m_splitCount = count;
            m_ratio.Set((float)m_splitCount / m_span);
            m_countText.Set($"数量：{m_splitCount}/{m_totalCount}");
        }
    }
}
