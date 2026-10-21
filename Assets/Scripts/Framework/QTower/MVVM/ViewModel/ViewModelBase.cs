/*
 * ┌──────────────────────────────────┐
 * │  描    述: 视图模型基类，统一三态迁移与响应式成员的托管释放
 * │  类    名: ViewModelBase.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Threading;
using Utils.log;

namespace Framework.QTower.MVVM.ViewModel
{
    /// <summary>
    /// 视图模型基类：OnBind/OnUnbind/OnDispose 由框架驱动且自带幂等守卫，子类只写业务
    /// </summary>
    public abstract class ViewModelBase : IViewModel
    {
        // 响应式成员全部登记在这里，释放由基类统一负责，子类不用写 Dispose 明细
        private readonly ReactiveBundle m_bundle = new ReactiveBundle();

        private CancellationTokenSource m_bindCancellation;

        private bool m_isBound;
        private bool m_isDisposed;

        public bool IsBound => m_isBound;
        public bool IsDisposed => m_isDisposed;

        /// <summary>
        /// 绑定期内有效的令牌：OnUnbind 时取消，供模型侧异步任务收敛
        /// </summary>
        public CancellationToken BindToken => m_bindCancellation == null ? CancellationToken.None : m_bindCancellation.Token;

        /// <summary>
        /// 响应式成员集合，派生类用它创建属性与信号
        /// </summary>
        protected ReactiveBundle Bundle => m_bundle;

        /// <summary>
        /// 创建并托管一个响应式属性
        /// </summary>
        /// <param name="initialValue">初始值</param>
        protected ReactiveProperty<T> CreateProperty<T>(T initialValue) => m_bundle.CreateReactiveProperty(initialValue);

        /// <summary>
        /// 创建并托管一个信号
        /// </summary>
        protected Signal CreateSignal() => m_bundle.CreateSignal();

        /// <summary>
        /// 框架入口：View 接上时调用，重复绑定直接报错
        /// </summary>
        public void OnBind()
        {
            if (m_isDisposed)
            {
                QLog.Error($"绑定被忽略：视图模型已释放 {GetType().Name}");
                return;
            }

            if (m_isBound)
            {
                QLog.Error($"绑定被忽略：视图模型已处于绑定态 {GetType().Name}");
                return;
            }

            m_isBound = true;
            m_bindCancellation = new CancellationTokenSource();
            OnBindCore();
        }

        /// <summary>
        /// 框架入口：View 断开时调用，状态保留供下次绑定复用
        /// </summary>
        public void OnUnbind()
        {
            if (!m_isBound) return;

            m_isBound = false;
            CancelBindToken();
            OnUnbindCore();
        }

        /// <summary>
        /// 框架入口：面板销毁时调用，先于 Dispose
        /// </summary>
        public void OnDispose()
        {
            if (m_isDisposed) return;

            m_isDisposed = true;
            if (!m_isBound)
            {
                OnDisposeCore();
                return;
            }

            m_isBound = false;
            CancelBindToken();
            OnDisposeCore();
        }

        public virtual void Dispose()
        {
            CancelBindToken();

            if (m_bundle.IsDisposed) return;

            m_bundle.Dispose();
        }

        // 子类在此启动订阅与广播
        protected virtual void OnBindCore()
        {
        }

        // 子类在此停止广播，不销毁状态
        protected virtual void OnUnbindCore()
        {
        }

        // 子类在此释放自有资源
        protected virtual void OnDisposeCore()
        {
        }

        private void CancelBindToken()
        {
            if (m_bindCancellation == null) return;

            m_bindCancellation.Cancel();
            m_bindCancellation.Dispose();
            m_bindCancellation = null;
        }
    }
}
