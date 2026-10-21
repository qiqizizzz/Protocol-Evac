﻿﻿/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM生命周期与租约的编辑器测试, 覆盖三态迁移与拆除顺序
 * │  类    名: QMvvmTests.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Framework.QTower.MVVM.Binding;
using Framework.QTower.MVVM.ViewModel;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using NUnit.Framework;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.MVVM
{
    // 生命周期探针视图模型, 按到达顺序记录框架回调
    internal sealed class LifecycleProbeViewModel : IViewModelLifecycle
    {
        private readonly List<string> m_sequence = new List<string>();
        private readonly ReactiveProperty<int> m_count = new ReactiveProperty<int>(0);

        public IReadOnlyReactiveProperty<int> Count => m_count;
        public int LastApplied { get; set; }
        public int BindCount { get; private set; }
        public bool IsReleased { get; private set; }
        public string Sequence => string.Join(">", m_sequence);

        // 由测试驱动的属性写入, 对外只暴露只读接口
        public void SetCount(int value) => m_count.Value = value;

        public void OnBind()
        {
            BindCount++;
            m_sequence.Add("OnBind");
        }

        public void OnUnbind() => m_sequence.Add("OnUnbind");

        public void OnDispose() => m_sequence.Add("OnDispose");

        public void Dispose()
        {
            IsReleased = true;
            m_sequence.Add("Dispose");
        }
    }

    /// <summary>
    /// MVVM 生命周期测试：三态迁移、拆除顺序与所有权边界
    /// </summary>
    public class QMvvmTests
    {
        private readonly List<BindingScope> m_scopes = new List<BindingScope>();

        // 造一个不在场景里的作用域, 此时 AddComponent 不会触发 OnEnable
        private BindingScope NewScope()
        {
            GameObject host = new GameObject("MvvmLifecycleHost");
            BindingScope scope = host.AddComponent<BindingScope>();
            m_scopes.Add(scope);
            return scope;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = m_scopes.Count - 1; i >= 0; i--)
            {
                BindingScope scope = m_scopes[i];
                m_scopes.RemoveAt(i);
                if (scope == null) continue;

                Object.DestroyImmediate(scope.gameObject);
            }
        }

        [Test]
        public void Bind_CallsOnBindOnce_And_ActivatesScope()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();

            ViewModelLease lease = scope.Bind(viewModel);

            Assert.That(lease, Is.Not.Null);
            Assert.That(lease.Ownership, Is.EqualTo(ViewModelOwnership.Owned));
            Assert.That(scope.IsActive, Is.True, "作用域应在没有 OnEnable 的情况下被兜底激活");
            Assert.That(viewModel.BindCount, Is.EqualTo(1));
            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind"));
        }

        [Test]
        public void HideAndReopen_ReplaysBindings_ButBindsOnlyOnce()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            scope.Bind(viewModel);

            scope.Bind(viewModel.Count, value => viewModel.LastApplied = value);
            Assert.That(viewModel.LastApplied, Is.EqualTo(0), "绑定时应立即回放当前值");

            viewModel.SetCount(7);
            Assert.That(viewModel.LastApplied, Is.EqualTo(7), "激活期间应同步最新值");

            scope.End();
            Assert.That(scope.IsActive, Is.False);
            viewModel.LastApplied = -1;

            scope.Begin();
            Assert.That(viewModel.LastApplied, Is.EqualTo(7), "重开面板应重放全部绑定");
            Assert.That(viewModel.BindCount, Is.EqualTo(1), "重开面板不得二次 OnBind");
            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind"));
        }

        [Test]
        public void NotifyClosed_IsIdempotent_LeaseStaysAlive()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            ViewModelLease lease = scope.Bind(viewModel);

            scope.NotifyClosed();
            scope.NotifyClosed();

            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind>OnUnbind"), "关闭只应通知一次");
            Assert.That(lease.IsDisposed, Is.False);
            Assert.That(viewModel.IsReleased, Is.False);
        }

        [Test]
        public void OwnedTeardown_FollowsFixedOrder()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            ViewModelLease lease = scope.Bind(viewModel);

            lease.Dispose();

            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind>OnUnbind>OnDispose>Dispose"));
            Assert.That(scope.IsActive, Is.False, "绑定必须先于视图模型释放收敛");
            Assert.That(lease.IsDisposed, Is.True);

            // 重复拆除必须无害
            lease.Dispose();
            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind>OnUnbind>OnDispose>Dispose"));
        }

        [Test]
        public void ExternalTeardown_UnbindsButNeverReleases()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            ViewModelLease lease = scope.Bind(viewModel, ViewModelOwnership.External);

            lease.Dispose();

            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind>OnUnbind>OnDispose"));
            Assert.That(viewModel.IsReleased, Is.False, "External 的生死归注入方");
        }

        [Test]
        public void ScopeDestroy_KillsLease_WithoutTouchingExternalViewModel()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            ViewModelLease lease = scope.Bind(viewModel, ViewModelOwnership.External);

            Object.DestroyImmediate(scope.gameObject);

            Assert.That(lease.IsDisposed, Is.True);
            Assert.That(viewModel.IsReleased, Is.False);
            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind"), "作用域自毁不应越权释放外部视图模型");
        }

        [Test]
        public void Begin_AfterTeardown_DoesNotReactivate()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            ViewModelLease lease = scope.Bind(viewModel);

            lease.Dispose();

            LogAssert.Expect(LogType.Error, new Regex("激活失败.*租约已拆除"));
            scope.Begin();

            Assert.That(scope.IsActive, Is.False, "已拆除租约的作用域必须拒绝重新激活");
        }

        [Test]
        public void SecondBind_ReturnsExistingLease_And_IgnoresNewViewModel()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel first = new LifecycleProbeViewModel();
            LifecycleProbeViewModel second = new LifecycleProbeViewModel();
            ViewModelLease lease = scope.Bind(first);

            LogAssert.Expect(LogType.Error, new Regex("重复Bind被忽略"));
            ViewModelLease again = scope.Bind(second);

            Assert.That(again, Is.SameAs(lease));
            Assert.That(second.BindCount, Is.EqualTo(0), "第二个视图模型不应被启动");
        }

        [Test]
        public void ExternalLease_CanBeDisposedAfterOwnedHandoff()
        {
            // 面板把视图模型交给外部接管生死时, 本面板的拆除不得释放它
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            ViewModelLease external = scope.Bind(viewModel, ViewModelOwnership.External);

            external.Dispose();

            LifecycleProbeViewModel same = viewModel;
            Assert.That(same.IsReleased, Is.False);
            Assert.That(scope.Bind(viewModel, ViewModelOwnership.External), Is.SameAs(external), "同一作用域重复Bind返回已有租约");
        }
    }
}
