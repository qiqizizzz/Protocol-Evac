/*
 * ┌──────────────────────────────────┐
 * │  描    述: 可释放成员集合
 * │  类    名: ReactiveBundle.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Framework.QTower.MVVM.ViewModel;
using Utils.log;

namespace Framework.QTower.MVVM.ViewModel
{
    public sealed class ReactiveBundle : IDisposable
    {
        private readonly List<IDisposable> m_items = new List<IDisposable>();

        private bool m_isDisposed;

        public int Count => m_items.Count;

        public bool IsDisposed => m_isDisposed;

        /// <summary>
        /// 纳入集合管理并原样返回，便于字段初始化时顺手登记
        /// </summary>
        public T Add<T>(T item) where T : class, IDisposable
        {
            if (item == null)
            {
                QLog.Error("纳入了空成员引用，请检查调用处");
                return item;
            }

            if (m_isDisposed)
            {
                QLog.Error($"集合已释放，成员未纳入管理：{typeof(T).Name}");
                return item;
            }

            m_items.Add(item);
            return item;
        }

        public ReactiveProperty<T> CreateReactiveProperty<T>() => Add(new ReactiveProperty<T>());
        public ReactiveProperty<T> CreateReactiveProperty<T>(T initialValue) => Add(new ReactiveProperty<T>(initialValue));
        public Signal CreateSignal() => Add(new Signal());

        public void Dispose()
        {
            if (m_isDisposed) return;

            m_isDisposed = true;

            // 倒序释放，后创建的成员先销毁，单个异常不中断其余成员
            for (int i = m_items.Count - 1; i >= 0; i--)
            {
                try
                {
                    m_items[i].Dispose();
                }
                catch (Exception exception)
                {
                    QLog.Error($"释放成员异常：{exception}");
                }
            }

            m_items.Clear();
        }
    }
}