/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM验收测试视图模型，暴露只读属性与信号并承载全部上行入口
 * │  类    名: MvvmTestViewModel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.ViewModel;
using Framework.QTower.MVVM.ViewModel.Interfaces;

namespace UI.MvvmTest
{
    /// <summary>
    /// MVVM 验收测试视图模型：只对外暴露只读属性与信号，写入入口全部收敛在本类方法内
    /// </summary>
    public sealed class MvvmTestViewModel : ViewModelBase
    {
        private const string DEFAULT_TITLE = "MVVM 验收测试面板";
        private const int DEFAULT_COUNT = 0;
        private const bool DEFAULT_LOCK_ON = false;

        private readonly ReactiveProperty<string> m_title;
        private readonly ReactiveProperty<int> m_count;
        private readonly ReactiveProperty<bool> m_isLockOn;
        private readonly Signal m_ping;

        public IReadOnlyReactiveProperty<string> Title => m_title;
        public IReadOnlyReactiveProperty<int> Count => m_count;
        public IReadOnlyReactiveProperty<bool> IsLockOn => m_isLockOn;
        public ISignal Ping => m_ping;

        public MvvmTestViewModel()
        {
            // 响应式成员由基类的 ReactiveBundle 托管, 释放不用子类插手
            m_title = CreateProperty(DEFAULT_TITLE);
            m_count = CreateProperty(DEFAULT_COUNT);
            m_isLockOn = CreateProperty(DEFAULT_LOCK_ON);
            m_ping = CreateSignal();
        }

        /// <summary>
        /// 上行入口：计数加一，由 View 的按钮点击直接调用
        /// </summary>
        public void Increase() => m_count.Set(m_count.Value + 1);

        /// <summary>
        /// 上行入口：切换锁定状态，值未变化时不通知
        /// </summary>
        public void ToggleLockOn() => m_isLockOn.TrySet(!m_isLockOn.Value);

        /// <summary>
        /// 上行入口：发射信号，验证 Signal 绑定的订阅与收敛
        /// </summary>
        public void RaisePing() => m_ping.Fire();

        /// <summary>
        /// 改写标题，用于验证隐藏期间的写入不会回调 View
        /// </summary>
        /// <param name="title">新标题</param>
        public void SetTitle(string title) => m_title.Set(title);

        // View 每次接上都补写标题, 配合重开时的绑定重放
        protected override void OnBindCore() => m_title.Set(DEFAULT_TITLE);
    }
}
