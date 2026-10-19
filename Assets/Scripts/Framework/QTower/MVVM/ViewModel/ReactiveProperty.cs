/*
 * ┌──────────────────────────────────┐
 * │  描    述: 响应式属性                      
 * │  类    名: ReactiveProperty.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using Utils.log;

namespace Framework.QTower.MVVM.ViewModel
{
    public sealed class ReactiveProperty<T> : IReactiveProperty<T>
    {
        private const int MAX_REENTRANT_ROUNDS = 8; //最大重入轮数, 防止无限循环

        private static readonly IEqualityComparer<T> m_comparer = EqualityComparer<T>.Default;

        private readonly AsyncReactiveProperty<T> m_source;
        
        private bool m_hasValue;
        private bool m_isPublishing;
        private bool m_hasPendingValue;
        private bool m_disposeRequested;
        private bool m_isDisposed;
        private T m_pendingValue;

        public T Value
        {
            get => m_source.Value;
            set => Publish(value);
        }

        public bool HasValue => m_hasValue;
        public bool IsDisposed => m_isDisposed;
        
        public ReactiveProperty()
        {
            m_source = new AsyncReactiveProperty<T>(default);
        }

        public ReactiveProperty(T initialValue)
        {
            m_source = new AsyncReactiveProperty<T>(initialValue);
            m_hasValue = true;
        }

        public void Set(T value) => Publish(value);

        public bool TrySet(T value)
        {
            if(m_hasValue && m_comparer.Equals(m_source.Value, value))
                return false;
            
            Publish(value);
            return true;
        }

        public void Refresh()
        {
            if(!m_hasValue) return;
            
            Publish(m_source.Value);
        }

        public IReadOnlyReactiveProperty<T> AsReadOnly() => this;
        
        public IUniTaskAsyncEnumerable<T> WithoutCurrent() => m_source.WithoutCurrent();

        public UniTask<T> WaitAsync(CancellationToken cancellationToken = default) =>
            m_source.WaitAsync(cancellationToken);

        //取异步枚举器, 尚未赋值过时不重放默认值, 从第一次真实赋值开始推送
        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return m_hasValue
                ? m_source.GetAsyncEnumerator(cancellationToken)
                : m_source.WithoutCurrent().GetAsyncEnumerator(cancellationToken);
        }

        public void Dispose()
        {
            if(m_isDisposed) return;

            if (m_isPublishing)
            {
                m_disposeRequested = true;
                return;
            }

            m_isDisposed = true;
            m_hasPendingValue = false;
            m_source.Dispose();
        }

        public override string ToString() => m_source.Value?.ToString() ?? string.Empty;

        private void Publish(T value)
        {
            if(m_isDisposed)
            {
                QLog.Error($"属性已释放，写入被忽略：{value}");
                return;
            }

            m_hasValue = true;

            if (m_isPublishing)
            {
                m_pendingValue = value;
                m_hasPendingValue = true;
                return;
            }

            m_isPublishing = true;

            try
            {
                m_source.Value = value;

                int rounds = 0;
                while (m_hasPendingValue)
                {
                    if (++rounds >= MAX_REENTRANT_ROUNDS)
                    {
                        m_hasPendingValue = false;
                        QLog.Error("订阅回调中持续回写自身, 已停止补发, 请检查是否存在循环赋值");
                        break;
                    }

                    T pendingValue = m_pendingValue;
                    m_hasPendingValue = false;
                    m_source.Value = pendingValue;
                }
            }
            finally
            {
                m_isPublishing = false;
            }
            
            if(m_disposeRequested) 
                Dispose();
        }
    }
}