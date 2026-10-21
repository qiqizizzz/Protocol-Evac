/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM验收测试视图模型，实现三态生命周期并暴露属性与信号供绑定
 * │  类    名: MvvmTestViewModel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using Framework.QTower.MVVM.ViewModel;
using Framework.QTower.MVVM.ViewModel.Interfaces;

namespace UI.MvvmTest
{
    /// <summary>
    /// MVVM 验收测试模型：只暴露只读属性与信号，写入入口全部收敛在本类方法内
    /// </summary>
    public sealed class MvvmTestViewModel : IViewModelLifecycle
    {
        private const string DEFAULT_TITLE = "MVVM 验收测试面板";
        private const int DEFAULT_COUNT = 0;
        private const bool DEFAULT_LOCK_ON = false;

        // ReactiveBundle 是 sealed，视图模型只能组合它来托管响应式成员
        private readonly ReactiveBundle m_bundle = new ReactiveBundle();

        private readonly ReactiveProperty<string> m_title;
        private readonly ReactiveProperty<int> m_count;
        private readonly ReactiveProperty<bool> m_isLockOn;
        private readonly Signal m_ping;

        private bool m_isBound;
        private bool m_isDisposed;

        public bool IsBound => m_isBound;
        public bool IsDisposed => m_isDisposed;

        public IReadOnlyReactiveProperty<string> Title => m_title;
        public IReadOnlyReactiveProperty<int> Count => m_count;
        public IReadOnlyReactiveProperty<bool> IsLockOn => m_isLockOn;
        public ISignal Ping => m_ping;

        public MvvmTestViewModel()
        {
            m_title = m_bundle.CreateReactiveProperty(DEFAULT_TITLE);
            m_count = m_bundle.CreateReactiveProperty(DEFAULT_COUNT);
            m_isLockOn = m_bundle.CreateReactiveProperty(DEFAULT_LOCK_ON);
            m_ping = m_bundle.CreateSignal();
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

        /// <summary>
        /// 面板打开并完成绑定后由框架调用，只在首次绑定补写标题
        /// </summary>
        public void OnBind()
        {
            if (m_isBound) return;

            m_isBound = true;
            m_title.Set(DEFAULT_TITLE);
        }

        /// <summary>
        /// 面板关闭后由框架调用，状态保留供下次打开直接复用
        /// </summary>
        public void OnUnbind()
        {
            m_isBound = false;
        }

        /// <summary>
        /// 面板销毁时由框架调用，先于 IDisposable.Dispose，收敛顺序由租约保证
        /// </summary>
        public void OnDispose()
        {
            m_isDisposed = true;
        }

        public void Dispose()
        {
            if (m_bundle.IsDisposed) return;

            m_bundle.Dispose();
        }
    }
}