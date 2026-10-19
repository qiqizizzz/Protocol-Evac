/*
 * ┌──────────────────────────────────┐
 * │  描    述: 信号实现
 * │  类    名: Signal.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using Utils.log;

namespace Framework.QTower.MVVM.ViewModel
{
    public sealed class Signal : ISignal
    {
        private event Action m_handlers;

        private bool m_isDisposed;

        public void Fire()
        {
            if (m_isDisposed)
            {
                QLog.Error("信号已释放，Fire 被忽略");
                return;
            }

            // 取快照再逐个调用，回调内部增删订阅不会影响本轮遍历
            Action snapshot = m_handlers;
            if (snapshot == null) return;

            foreach (Delegate entry in snapshot.GetInvocationList())
            {
                try
                {
                    ((Action)entry)();
                }
                catch (Exception exception)
                {
                    QLog.Error($"信号回调异常：{exception}");
                }
            }
        }
        
        public void Dispose()
        {
            if (m_isDisposed) return;

            m_isDisposed = true;
            m_handlers = null;
        }

        public IDisposable Subscribe(Action handler)
        {
            if (handler == null)
            {
                QLog.Error("订阅信号传入了空回调，请检查绑定代码");
                return new Subscription(this, null);
            }

            if (m_isDisposed)
            {
                QLog.Error("信号已释放，订阅被忽略");
                return new Subscription(this, null);
            }

            m_handlers += handler;
            return new Subscription(this, handler);
        }
        
        private void Unsubscribe(Action handler)
        {
            if (m_isDisposed || handler == null) return;

            m_handlers -= handler;
        }

        // 订阅句柄，保证同一次订阅最多退订一次
        private sealed class Subscription : IDisposable
        {
            private Signal m_owner;
            private readonly Action m_handler;

            public Subscription(Signal signal, Action callback)
            {
                m_owner = signal;
                m_handler = callback;
            }

            public void Dispose()
            {
                if (m_owner == null) return;

                Signal target = m_owner;
                m_owner = null;
                target.Unsubscribe(m_handler);
            }
        }
    }
}