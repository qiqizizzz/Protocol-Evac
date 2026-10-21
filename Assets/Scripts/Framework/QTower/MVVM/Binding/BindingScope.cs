/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM绑定作用域，管理一组绑定的登记、启动与父子级联收敛          
 * │  类    名: BindingScope.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using Framework.QTower.MVVM.ViewModel;
using UnityEngine;
using Utils.log;

namespace Framework.QTower.MVVM.Binding
{
    /// <summary>
    /// 绑定作用域：挂在界面根节点上，统一管理一组绑定的启动与收敛
    /// </summary>
    public sealed class BindingScope : MonoBehaviour
    {
        [Tooltip("true 时由 GameObject 激活状态自动 Begin/End")]
        [SerializeField] private bool AutoDriveByActive = true;

        public bool IsActive => m_isActive;
        public int ActiveBindingCount => m_activeBindingCount;

        /// <summary>
        /// 作用域销毁时自动取消的令牌，首次 Begin 之前为 None
        /// </summary>
        public CancellationToken Token => m_cts != null ? m_cts.Token : CancellationToken.None;

        private readonly List<BindingEntry> m_entries = new List<BindingEntry>();
        private readonly List<BindingScope> m_children = new List<BindingScope>();

        private CancellationTokenSource m_cts;
        private bool m_isActive;
        private bool m_hasBegun;
        private int m_activeBindingCount;
        private ViewModelLease m_lease;
        private bool m_isDisposed;

        #region 生命周期
        private void OnEnable()
        {
            if (!AutoDriveByActive || m_isActive) return;

            Activate();
        }

        private void OnDisable()
        {
            if (!AutoDriveByActive || !m_isActive) return;

            Deactivate();
        }


        private void OnDestroy()
        {
            m_isDisposed = true;

            if (m_isActive) Deactivate();

            m_entries.Clear();
            m_children.Clear();
            m_lease?.MarkDisposed();
            m_lease = null;


            if (m_cts == null) return;
            m_cts.Cancel();
            m_cts.Dispose();
            m_cts = null;
        }
        #endregion


        #region 主要函数
        /// <summary>
        /// 取得目标上的作用域，没有则挂一个
        /// </summary>
        public static BindingScope GetOrCreate(GameObject target)
        {
            if (target == null)
            {
                QLog.Error("绑定作用域获取失败：目标 GameObject 为空");
                return null;
            }

            if (!target.TryGetComponent(out BindingScope scope))
                scope = target.AddComponent<BindingScope>();

            return scope;
        }

        /// <summary>
        /// 绑定只读响应式属性：值变化时回调 apply，直到 End
        /// </summary>
        /// <param name="property">ViewModel侧的只读响应式属性</param>
        /// <param name="apply">View侧接收回调</param>
        public void Bind<T>(IReadOnlyReactiveProperty<T> property, Action<T> apply)
        {
            if (!CheckCanBind()) return;

            if (property == null)
            {
                QLog.Error("绑定失败：只读响应式属性为空");
                return;
            }

            if (apply == null)
            {
                QLog.Error("绑定失败：回调为空");
                return;
            }

            CancellationTokenSource entryCts = null;

            AddEntry(new BindingEntry(
                () =>
                {
                    entryCts = CancellationTokenSource.CreateLinkedTokenSource(m_cts.Token);
                    m_activeBindingCount++;
                    Binding.Run<T>(property, apply, entryCts.Token, () => m_activeBindingCount--);
                },
                () =>
                {
                    if (entryCts == null) return;

                    entryCts.Cancel();
                    entryCts.Dispose();
                    entryCts = null;
                }));
        }

        /// <summary>
        /// 绑定信号：每次 Fire 回调 handler，不进异步枚举
        /// </summary>
        /// <param name="signal">ViewModel侧的信号</param>
        /// <param name="handler">View侧接收回调</param>
        public void Bind(ISignal signal, Action handler)
        {
            if (!CheckCanBind()) return;

            if (signal == null)
            {
                QLog.Error("绑定失败：信号为空");
                return;
            }

            if (handler == null)
            {
                QLog.Error("绑定失败：回调为空");
                return;
            }

            IDisposable subscription = null;

            AddEntry(new BindingEntry(
                () =>
                {
                    m_activeBindingCount++;
                    subscription = signal.Subscribe(handler);
                },
                () =>
                {
                    if (subscription == null) return;

                    subscription.Dispose();
                    subscription = null;
                    m_activeBindingCount--;
                }));
        }

        /// <summary>
        /// 创建子作用域并登记，本作用域 End 时子作用域先收敛
        /// </summary>
        public BindingScope CreateChild(GameObject target)
        {
            BindingScope child = GetOrCreate(target);
            if (child == null || child == this) return child;

            if (!m_children.Contains(child))
                m_children.Add(child);

            // 补 Begin：覆盖「父已激活之后才创建子作用域」的时序
            if (m_isActive && !child.m_isActive)
                child.Begin();

            return child;
        }

        /// <summary>
        /// 启动全部登记项
        /// </summary>
        public void Begin()
        {
            if (m_isActive)
            {
                QLog.Error("BindingScope已激活,重复Begin被忽略(自动模式下无需手动Begin)");
                return;
            }

            Activate();
        }

        /// <summary>
        /// 收敛全部登记项，登记关系保留，下次 Begin 重放
        /// </summary>
        public void End()
        {
            if (!m_isActive)
            {
                QLog.Error("BindingScope未激活,End被忽略");
                return;
            }

            Deactivate();
        }

        /// <summary>
        /// 绑定视图模型并接管其生死, 本作用域负责完整的收敛顺序
        /// </summary>
        /// <param name="viewModel">View侧要绑定的视图模型</param>
        public ViewModelLease Bind(IViewModelLifecycle viewModel) => Bind(viewModel, ViewModelOwnership.Owned);

        /// <summary>
        /// 绑定视图模型并声明生死归属, External 只解绑不释放
        /// </summary>
        /// <param name="viewModel">View侧要绑定的视图模型</param>
        /// <param name="ownership">视图模型的生死归属</param>
        public ViewModelLease Bind(IViewModelLifecycle viewModel, ViewModelOwnership ownership)
        {
            if (m_isDisposed)
            {
                QLog.Error("绑定失败：绑定作用域已销毁, 视图模型未纳入管理");
                return null;
            }

            if (viewModel == null)
            {
                QLog.Error("绑定失败：视图模型为空");
                return null;
            }

            if (m_lease != null)
            {
                QLog.Error($"绑定失败：作用域已持有视图模型租约, 重复Bind被忽略：{viewModel.GetType().Name}");
                return m_lease;
            }

            m_lease = new ViewModelLease(this, viewModel, ownership);

            viewModel.OnBind();

            // 挂上组件时 OnEnable 未必已经跑过, 这里补一次, 避免绑定登记完却永不激活
            EnsureScopeRunning();
            return m_lease;
        }

        /// <summary>
        /// 面板关闭时通知视图模型停止广播, 绑定条目与租约都不动
        /// </summary>
        public void NotifyClosed()
        {
            if (m_lease == null || m_lease.IsDisposed) return;

            m_lease.NotifyClosed();
        }
        #endregion

        #region 内部函数
        private bool CheckCanBind()
        {
            if (m_isActive || !m_hasBegun) return true;

            QLog.Error("绑定失败：BindingScope已End且未重新Begin,此窗口的绑定会被丢弃");
            return false;
        }

        // 只驱动状态, 资源释放一律交给租约
        private void EnsureScopeRunning()
        {
            if (m_isActive) return;

            if (!m_hasBegun)
            {
                Activate();
                return;
            }

            Begin();
        }


        private void AddEntry(BindingEntry entry)
        {
            m_entries.Add(entry);
            if (m_isActive)
                entry.Start();
        }

        private void Activate()
        {
            if (m_isActive) return;

            // 租约已拆除却再次激活, 说明面板被直接 Open 复用, 必须先重绑
            if (m_lease != null && m_lease.IsDisposed)
            {
                QLog.Error("激活失败：绑定作用域的视图模型租约已拆除, 请重新 Bind 视图模型后再打开");
                return;
            }

            m_isActive = true;
            m_hasBegun = true;
            if (m_cts == null)
                m_cts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            for (int i = 0; i < m_entries.Count; i++)
                m_entries[i].Start();

            CascadeChildScope(true);
        }

        private void Deactivate()
        {
            if (!m_isActive) return;

            // 子先收，避免父停止后子作用域仍能回调 apply
            CascadeChildScope(false);

            for (int i = 0; i < m_entries.Count; i++)
                m_entries[i].Stop();

            m_isActive = false;
        }

        // 逆序遍历：End 时保证后创建的子先收敛
        private void CascadeChildScope(bool begin)
        {
            for (int i = m_children.Count - 1; i >= 0; i--)
            {
                BindingScope child = m_children[i];
                if (child == null)
                {
                    m_children.RemoveAt(i);
                    continue;
                }

                if (begin)
                {
                    if (!child.m_isActive) child.Begin();
                }
                else if (child.m_isActive)
                {
                    child.End();
                }
            }
        }
        #endregion
    }
}