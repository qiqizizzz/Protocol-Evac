/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM生命周期测试，覆盖三态迁移、租约顺序与ViewBase驱动
 * │  类    名: QMvvmTests.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Framework.QTower.MVVM.Binding;
using Framework.QTower.MVVM.View;
using Framework.QTower.MVVM.ViewModel;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.MVVM
{
    // 生命周期探针视图模型, 按到达顺序记录框架回调
    internal sealed class LifecycleProbeViewModel : ViewModelBase
    {
        private readonly List<string> m_sequence = new List<string>();
        private readonly ReactiveProperty<int> m_count;

        public LifecycleProbeViewModel()
        {
            m_count = CreateProperty(0);
        }

        public IReadOnlyReactiveProperty<int> Count => m_count;
        public int BindCount { get; private set; }
        public string Sequence => string.Join(">", m_sequence);

        // 由测试驱动的属性写入, 对外只暴露只读接口
        public void SetCount(int value) => m_count.Value = value;

        protected override void OnBindCore()
        {
            BindCount++;
            m_sequence.Add("OnBind");
        }

        protected override void OnUnbindCore() => m_sequence.Add("OnUnbind");

        protected override void OnDisposeCore() => m_sequence.Add("OnDispose");

        public override void Dispose()
        {
            m_sequence.Add("Dispose");
            base.Dispose();
        }
    }

    // 生命周期探针面板, 只写 ViewBase 的两个抽象方法
    internal sealed class ProbePanelView : ViewBase<LifecycleProbeViewModel>
    {
        public int BindCalls { get; private set; }
        public int ShowCalls { get; private set; }
        public int CloseCalls { get; private set; }
        public int LastApplied { get; set; }

        protected override LifecycleProbeViewModel CreateViewModel() => new LifecycleProbeViewModel();

        protected override void BindViewModel(LifecycleProbeViewModel viewModel)
        {
            BindCalls++;
            Scope.Bind(viewModel.Count, value => LastApplied = value);
        }

        protected override void OnShow(params object[] args) => ShowCalls++;

        protected override void OnClose(params object[] args) => CloseCalls++;
    }

    /// <summary>
    /// MVVM 生命周期测试：三态迁移、租约拆除顺序与 ViewBase 驱动
    /// </summary>
    public class QMvvmTests
    {
        private readonly List<GameObject> m_objects = new List<GameObject>();

        // 造一个独立宿主, 面板与作用域都挂在它上面
        private static GameObject NewHost()
        {
            GameObject host = new GameObject("MvvmLifecycleHost");
            return host;
        }

        private BindingScope NewScope()
        {
            GameObject host = NewHost();
            m_objects.Add(host);
            return host.AddComponent<BindingScope>();
        }

        private TView NewView<TView>() where TView : Component
        {
            GameObject host = NewHost();
            m_objects.Add(host);
            return host.AddComponent<TView>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = m_objects.Count - 1; i >= 0; i--)
            {
                GameObject target = m_objects[i];
                m_objects.RemoveAt(i);
                if (target == null) continue;

                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void Bind_CallsOnBind_And_ActivatesScope()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();

            ViewModelLease lease = scope.Bind(viewModel);

            Assert.That(lease, Is.Not.Null);
            Assert.That(lease.Ownership, Is.EqualTo(ViewModelOwnership.Owned));
            Assert.That(scope.IsActive, Is.True, "绑定后作用域应处于激活态");
            Assert.That(viewModel.IsBound, Is.True);
            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind"));
        }

        [Test]
        public void Bind_RejectsViewModelThatIsAlreadyBound()
        {
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            NewScope().Bind(viewModel);

            LogAssert.Expect(LogType.Error, new Regex("已处于绑定态"));
            Assert.That(NewScope().Bind(viewModel), Is.Null, "已绑定的模型不允许接到第二个作用域");
            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind"), "被拒绝的绑定不应二次 OnBind");
        }

        [Test]
        public void Bind_RejectsDisposedViewModelAndTornDownScope()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            scope.Bind(viewModel).Dispose();

            LogAssert.Expect(LogType.Error, new Regex("视图模型已被释放"));
            Assert.That(NewScope().Bind(viewModel), Is.Null, "已拆除的模型不得重新接上");

            LogAssert.Expect(LogType.Error, new Regex("作用域已完成拆除"));
            Assert.That(scope.Bind(new LifecycleProbeViewModel()), Is.Null, "拆完的租约不可复用");
        }

        [Test]
        public void NotifyClosed_IsIdempotent_AndReopenRestoresBoundState()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            ViewModelLease lease = scope.Bind(viewModel);

            scope.NotifyClosed();
            scope.NotifyClosed();

            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind>OnUnbind"), "关闭只应通知一次");
            Assert.That(lease.IsDisposed, Is.False, "关闭不拆除租约");

            scope.NotifyReopened();
            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind>OnUnbind>OnBind"));
            Assert.That(viewModel.BindCount, Is.EqualTo(2));
            Assert.That(viewModel.IsBound, Is.True);
        }

        [Test]
        public void EndAndBegin_ReplayBindings_WithoutTouchingViewModel()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            scope.Bind(viewModel);

            int lastApplied = 0;
            scope.Bind(viewModel.Count, value => lastApplied = value);
            Assert.That(lastApplied, Is.EqualTo(0), "绑定时应立即回放当前值");

            viewModel.SetCount(7);
            Assert.That(lastApplied, Is.EqualTo(7));

            // 隐藏面板：GameObject 失活等价于 End, 视图模型保持绑定态
            scope.gameObject.SetActive(false);
            Assert.That(scope.IsActive, Is.False);
            Assert.That(viewModel.IsBound, Is.True, "隐藏不该解绑");

            lastApplied = -1;
            scope.gameObject.SetActive(true);
            Assert.That(lastApplied, Is.EqualTo(7), "重新激活应重放绑定");
            Assert.That(viewModel.BindCount, Is.EqualTo(1), "隐藏重开不得二次 OnBind");
        }

        [Test]
        public void OwnedTeardown_FollowsFixedOrder()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            ViewModelLease lease = scope.Bind(viewModel);

            lease.Dispose();

            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind>OnUnbind>OnDispose>Dispose"));
            Assert.That(lease.IsDisposed, Is.True);
            Assert.That(viewModel.IsDisposed, Is.True);
            Assert.That(scope.IsActive, Is.False, "拆除应先收敛全部绑定");
        }

        [Test]
        public void ExternalTeardown_UnbindsButNeverReleases()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            ViewModelLease lease = scope.Bind(viewModel, ViewModelOwnership.External);

            lease.Dispose();

            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind>OnUnbind>OnDispose"), "External 的生死归注入方");
            Assert.That(lease.IsDisposed, Is.True);
        }

        [Test]
        public void ScopeDestroy_TearsDownOwnedLeaseCompletely()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            scope.Bind(viewModel);

            GameObject host = scope.gameObject;
            m_objects.Remove(host);

            Object.DestroyImmediate(host);

            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind>OnUnbind>OnDispose>Dispose"), "作用域自毁要带走 Owned 模型");
        }

        [Test]
        public void Begin_AfterTeardown_DoesNotReactivate()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel viewModel = new LifecycleProbeViewModel();
            scope.Bind(viewModel).Dispose();

            LogAssert.Expect(LogType.Error, new Regex("租约已拆除"));
            scope.Begin();

            Assert.That(scope.IsActive, Is.False);
        }

        [Test]
        public void SecondBind_ReturnsExistingLease_And_IgnoresNewViewModel()
        {
            BindingScope scope = NewScope();
            LifecycleProbeViewModel first = new LifecycleProbeViewModel();
            ViewModelLease lease = scope.Bind(first);
            LifecycleProbeViewModel other = new LifecycleProbeViewModel();

            LogAssert.Expect(LogType.Error, new Regex("已持有视图模型租约"));
            ViewModelLease second = scope.Bind(other);

            Assert.That(second, Is.SameAs(lease));
            Assert.That(first.Sequence, Is.EqualTo("OnBind"));
            Assert.That(other.IsBound, Is.False, "被拒绝的模型不该进入绑定态");
        }

        [Test]
        public void ViewBase_ShowBindsOnce_And_HideReplaysWithoutRebind()
        {
            ProbePanelView view = NewView<ProbePanelView>();

            view.Show();
            Assert.That(view.BindCalls, Is.EqualTo(1));
            Assert.That(view.IsBound, Is.True);
            Assert.That(view.ViewModel.BindCount, Is.EqualTo(1));
            Assert.That(view.LastApplied, Is.EqualTo(0), "Show 当帧应完成首值回放");

            view.ViewModel.SetCount(3);
            Assert.That(view.LastApplied, Is.EqualTo(3));

            view.Hide();
            Assert.That(view.IsBound, Is.True, "Hide 只断显示, 不该解绑");

            view.LastApplied = -1;
            view.Show();
            Assert.That(view.BindCalls, Is.EqualTo(1), "重开不该重复登记绑定");
            Assert.That(view.LastApplied, Is.EqualTo(3), "重开应重放已有绑定");
            Assert.That(view.ViewModel.BindCount, Is.EqualTo(1), "隐藏重开不得二次 OnBind");
        }

        [Test]
        public void ViewBase_CloseThenShow_RebindsSameViewModelInstance()
        {
            ProbePanelView view = NewView<ProbePanelView>();

            view.Show();
            LifecycleProbeViewModel viewModel = view.ViewModel;
            Assert.That(viewModel, Is.Not.Null);

            view.Close();
            Assert.That(view.IsBound, Is.False, "Close 应让视图模型进入解绑态");
            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind>OnUnbind"));
            Assert.That(viewModel.IsDisposed, Is.False, "Close 不销毁视图模型");

            view.LastApplied = -1;
            view.Show();

            Assert.That(view.IsBound, Is.True);
            Assert.That(view.BindCalls, Is.EqualTo(1), "Close 后重开不该重复登记绑定");
            Assert.That(view.ViewModel, Is.SameAs(viewModel), "重开复用同一个视图模型实例");
            Assert.That(viewModel.BindCount, Is.EqualTo(2), "Close 后重开要重新 OnBind");
            Assert.That(view.LastApplied, Is.EqualTo(0), "重开应重放已有绑定");
        }

        [Test]
        public void ViewBase_DestroyReleasesOwnedViewModel()
        {
            ProbePanelView view = NewView<ProbePanelView>();
            view.Show();

            LifecycleProbeViewModel viewModel = view.ViewModel;
            GameObject host = view.gameObject;
            m_objects.Remove(host);

            Object.DestroyImmediate(host);

            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind>OnUnbind>OnDispose>Dispose"));
        }
    }
}
