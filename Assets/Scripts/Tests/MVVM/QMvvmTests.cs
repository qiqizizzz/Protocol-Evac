﻿﻿/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM生命周期测试，覆盖三态迁移、租约顺序、ViewBase驱动与模型仓库窗口服务
 * │  类    名: QMvvmTests.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Framework.QTower.MVVM.Binding;
using Framework.QTower.MVVM.Model;
using Framework.QTower.MVVM.Model.Interfaces;
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

    // 两个模型契约探针, 用于验证仓库按接口登记而不是按实现类型
    internal interface IProbeModelA : IModel
    {
    }

    internal interface IProbeModelB : IModel
    {
    }

    // 模型层探针, 把注册与注销按到达顺序记进共享日志
    internal sealed class ProbeModel : IProbeModelA, IProbeModelB
    {
        private readonly List<string> m_journal;
        private readonly string m_name;

        public ProbeModel(string name, List<string> journal)
        {
            m_name = name;
            m_journal = journal;
        }

        public void OnRegister() => m_journal.Add($"{m_name}.Register");

        public void OnUnregister() => m_journal.Add($"{m_name}.Unregister");
    }

        // 非泛型注册用的第三个契约, ProbeModel 并未实现它
    internal interface IProbeModelC : IModel
    {
    }

    // 反射装配探针日志, 模块与模型都往这里追加
    internal static class QMvvmProbeJournal
    {
        internal static readonly List<string> Lines = new List<string>();

        public static void Clear() => Lines.Clear();

        public static void Add(string line) => Lines.Add(line);

        public static string Text() => string.Join(">", Lines);
    }

    // 业务装配类探针: 静态 Register(MvvmRoot), 由 MvvmHost 反射发现
    [MvvmModule]
    internal static class QMvvmProbeModule
    {
        public static void Register(MvvmRoot root)
        {
            QMvvmProbeJournal.Add("Module.Enter");
            root.Models.Register<IProbeModelA>(new ProbeModel("Module", QMvvmProbeJournal.Lines));
        }
    }

    // Order 决定装配先后的两个探针
    [MvvmModule(11)]
    internal static class QMvvmProbeModuleLow
    {
        public static void Register(MvvmRoot root) => QMvvmProbeJournal.Add("Low");
    }

    [MvvmModule(12)]
    internal static class QMvvmProbeModuleHigh
    {
        public static void Register(MvvmRoot root) => QMvvmProbeJournal.Add("High");
    }

    // 缺少 Register 方法的探针
    [MvvmModule]
    internal static class QMvvmProbeModuleMissing
    {
    }

    // 装配时抛异常的探针
    [MvvmModule]
    internal static class QMvvmProbeModuleThrowing
    {
        public static void Register(MvvmRoot root) => throw new InvalidOperationException("探针装配断裂");
    }

/// <summary>
    /// MVVM 生命周期测试：三态迁移、租约拆除顺序与 ViewBase 驱动
    /// </summary>
    public class QMvvmTests
    {
        private readonly List<GameObject> m_objects = new List<GameObject>();
        private readonly List<GameObject> m_roots = new List<GameObject>();

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
            BindingScope scope = host.AddComponent<BindingScope>();
            DriveInstantiate(host);
            return scope;
        }

        private TView NewView<TView>() where TView : Component
        {
            GameObject host = NewHost();
            m_objects.Add(host);
            TView view = host.AddComponent<TView>();
            DriveInstantiate(host);
            return view;
        }

        private WindowService NewWindowService(out GameObject root)
        {
            root = NewHost();
            m_objects.Add(root);
            return new WindowService(root.transform);
        }

        // 造真实运行根节点: EditMode 不自动派发 Awake, 这里手动对齐, 销毁交给 m_roots 兜底
        private MvvmRoot NewRoot()
        {
            if (MvvmRoot.Instance != null) return MvvmRoot.Instance;

            GameObject host = NewHost();
            m_objects.Add(host);
            host.AddComponent<MvvmRoot>();
            DriveInstantiate(host);
            m_roots.Add(host);

            return MvvmRoot.Instance;
        }

        // 视图工厂探针: 造一个挂好面板组件的物体, 时序与运行时实例化一致
        private ProbePanelView NewPanelObject(Transform parent)
        {
            GameObject panel = new GameObject("ProbePanel", typeof(RectTransform));
            m_objects.Add(panel);
            panel.transform.SetParent(parent, false);
            ProbePanelView view = panel.AddComponent<ProbePanelView>();
            DriveInstantiate(panel);
            return view;
        }

        // EditMode 不会自动派发 Unity 生命周期消息, 下面几个方法把时序对齐到运行时
        // 这里用反射直调而不用 SendMessage: SendMessage 会让引擎多吐一条 ShouldRunBehaviour 断言日志
        private static void InvokeMessage(GameObject host, string methodName)
        {
            foreach (Component component in host.GetComponents<Component>())
            {
                if (component == null) continue;

                MethodInfo method = FindMessageMethod(component.GetType(), methodName);
                method?.Invoke(component, null);
            }
        }

        // 沿继承链找声明了该消息的方法, 私有与保护都算
        private static MethodInfo FindMessageMethod(Type type, string methodName)
        {
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                MethodInfo method = current.GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (method != null) return method;
            }

            return null;
        }

        // 模拟实例化: 挂上组件后 Awake -> OnEnable, ViewBase 正是在 Awake 里拿到作用域
        private static void DriveInstantiate(GameObject host)
        {
            InvokeMessage(host, "Awake");
            InvokeMessage(host, "OnEnable");
        }

        // 模拟显隐: ViewBase 的 SetVisible 已改过 activeSelf, 这里按目标状态补发对应消息
        private static void DriveActiveState(GameObject host, bool isActive)
        {
            host.SetActive(isActive);
            InvokeMessage(host, isActive ? "OnEnable" : "OnDisable");
        }

        // 模拟销毁: 运行时顺序是先 OnDisable 再 OnDestroy
        private static void DriveDestroy(GameObject host)
        {
            if (host.activeSelf)
                InvokeMessage(host, "OnDisable");

            InvokeMessage(host, "OnDestroy");
        }

        [TearDown]
        public void TearDown()
        {
            // 根节点先走 OnDestroy, 让 MvvmRoot 静态实例与服务按运行时顺序拆除
            for (int i = m_roots.Count - 1; i >= 0; i--)
                DriveDestroy(m_roots[i]);

            m_roots.Clear();

            for (int i = m_objects.Count - 1; i >= 0; i--)
            {
                GameObject target = m_objects[i];
                m_objects.RemoveAt(i);
                if (target == null) continue;

                UnityEngine.Object.DestroyImmediate(target);
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
            DriveActiveState(scope.gameObject, false);
            Assert.That(scope.IsActive, Is.False);
            Assert.That(viewModel.IsBound, Is.True, "隐藏不该解绑");

            lastApplied = -1;
            DriveActiveState(scope.gameObject, true);
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

            // EditMode 下 DestroyImmediate 不派发 Unity 消息, 先按运行时顺序补发一遍
            DriveDestroy(host);

            UnityEngine.Object.DestroyImmediate(host);

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
            GameObject host = view.gameObject;

            view.Show();
            DriveActiveState(host, true);
            Assert.That(view.BindCalls, Is.EqualTo(1));
            Assert.That(view.IsBound, Is.True);
            Assert.That(view.Scope.IsActive, Is.True, "Show 后作用域应处于激活态");
            Assert.That(view.ViewModel.BindCount, Is.EqualTo(1));
            Assert.That(view.LastApplied, Is.EqualTo(0), "Show 当帧应完成首值回放");

            view.ViewModel.SetCount(3);
            Assert.That(view.LastApplied, Is.EqualTo(3));

            view.Hide();
            DriveActiveState(host, false);
            Assert.That(view.IsBound, Is.True, "Hide 只断显示, 不该解绑");
            Assert.That(view.Scope.IsActive, Is.False, "Hide 应收敛全部绑定");

            view.LastApplied = -1;
            view.Show();
            DriveActiveState(host, true);
            Assert.That(view.BindCalls, Is.EqualTo(1), "重开不该重复登记绑定");
            Assert.That(view.LastApplied, Is.EqualTo(3), "重开应重放已有绑定");
            Assert.That(view.ViewModel.BindCount, Is.EqualTo(1), "隐藏重开不得二次 OnBind");
        }

        [Test]
        public void ViewBase_CloseThenShow_RebindsSameViewModelInstance()
        {
            ProbePanelView view = NewView<ProbePanelView>();
            GameObject host = view.gameObject;

            view.Show();
            DriveActiveState(host, true);
            LifecycleProbeViewModel viewModel = view.ViewModel;
            Assert.That(viewModel, Is.Not.Null);

            view.Close();
            DriveActiveState(host, false);
            Assert.That(view.IsBound, Is.False, "Close 应让视图模型进入解绑态");
            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind>OnUnbind"));
            Assert.That(viewModel.IsDisposed, Is.False, "Close 不销毁视图模型");
            Assert.That(view.Scope.IsActive, Is.False, "Close 后绑定应全部收敛");

            view.LastApplied = -1;
            view.Show();
            DriveActiveState(host, true);

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
            DriveActiveState(view.gameObject, true);

            LifecycleProbeViewModel viewModel = view.ViewModel;
            GameObject host = view.gameObject;
            m_objects.Remove(host);

            DriveDestroy(host);
            UnityEngine.Object.DestroyImmediate(host);

            Assert.That(viewModel.Sequence, Is.EqualTo("OnBind>OnUnbind>OnDispose>Dispose"));
        }

        [Test]
        public void ModelRegistry_Register_And_GetOrCreate_ReusesInstance()
        {
            List<string> journal = new List<string>();
            ModelRegistry registry = new ModelRegistry();

            ProbeModel first = registry.Register(new ProbeModel("A", journal));
            ProbeModel again = registry.GetOrCreate(() => new ProbeModel("B", journal));

            Assert.That(again, Is.SameAs(first), "GetOrCreate 命中已注册实例不该新建");
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(string.Join(">", journal), Is.EqualTo("A.Register"));
        }

        [Test]
        public void ModelRegistry_Get_Unregistered_Errors_And_TryGet_StaysQuiet()
        {
            ModelRegistry registry = new ModelRegistry();

            LogAssert.Expect(LogType.Error, new Regex("未注册"));
            Assert.That(registry.Get<ProbeModel>(), Is.Null);
            Assert.That(registry.TryGet(out ProbeModel model), Is.False);
            Assert.That(model, Is.Null);
        }

        [Test]
        public void ModelRegistry_Shutdown_UnregistersInReverseOrder()
        {
            List<string> journal = new List<string>();
            ModelRegistry registry = new ModelRegistry();
            registry.Register<IProbeModelA>(new ProbeModel("A", journal));
            registry.Register<IProbeModelB>(new ProbeModel("B", journal));

            registry.Shutdown();

            Assert.That(string.Join(">", journal), Is.EqualTo("A.Register>B.Register>B.Unregister>A.Unregister"));
            Assert.That(registry.IsShutdown, Is.True);
            Assert.That(registry.Count, Is.EqualTo(0));

            LogAssert.Expect(LogType.Error, new Regex("仓库已注销"));
            Assert.That(registry.Register(new ProbeModel("C", journal)), Is.Null, "注销后不再接受注册");
        }

        [Test]
        public void WindowService_ShowAsync_BuildsOnce_And_ReusesCachedInstance()
        {
            WindowService windows = NewWindowService(out GameObject root);
            int created = 0;
            windows.Register<ProbePanelView>(parent =>
            {
                created++;
                return NewPanelObject(parent).gameObject;
            });

            ProbePanelView view = windows.ShowAsync<ProbePanelView>().GetAwaiter().GetResult();

            Assert.That(view, Is.Not.Null);
            Assert.That(view.transform.parent, Is.SameAs(root.transform));
            Assert.That(created, Is.EqualTo(1));
            Assert.That(view.BindCalls, Is.EqualTo(1));
            Assert.That(windows.InstanceCount, Is.EqualTo(1));

            ProbePanelView again = windows.ShowAsync<ProbePanelView>().GetAwaiter().GetResult();

            Assert.That(again, Is.SameAs(view), "第二次显示复用缓存实例");
            Assert.That(created, Is.EqualTo(1), "复用不该再走工厂");
            Assert.That(view.BindCalls, Is.EqualTo(1), "复用不该重建绑定");
        }

        [Test]
        public void WindowService_CloseKeepsInstance_And_ReopenRebinds()
        {
            WindowService windows = NewWindowService(out GameObject root);
            windows.Register<ProbePanelView>(parent => NewPanelObject(parent).gameObject);

            ProbePanelView view = windows.ShowAsync<ProbePanelView>().GetAwaiter().GetResult();
            LifecycleProbeViewModel viewModel = view.ViewModel;

            windows.Close<ProbePanelView>();
            DriveActiveState(view.gameObject, false);
            Assert.That(view.IsBound, Is.False, "Close 应让视图模型解绑");
            Assert.That(windows.InstanceCount, Is.EqualTo(1), "Close 保留实例供复用");

            ProbePanelView reopened = windows.ShowAsync<ProbePanelView>().GetAwaiter().GetResult();
            DriveActiveState(view.gameObject, true);

            Assert.That(reopened, Is.SameAs(view));
            Assert.That(reopened.ViewModel, Is.SameAs(viewModel), "重开复用同一个视图模型");
            Assert.That(viewModel.BindCount, Is.EqualTo(2), "重开要重新 OnBind");
            Assert.That(view.LastApplied, Is.EqualTo(0), "重开应重放已有绑定");
        }

        [Test]
        public void WindowService_UnopenedAndUnregistered_Errors()
        {
            WindowService windows = NewWindowService(out GameObject root);

            LogAssert.Expect(LogType.Error, new Regex("尚未打开"));
            Assert.That(windows.Get<ProbePanelView>(), Is.Null);

            LogAssert.Expect(LogType.Error, new Regex("尚未打开"));
            windows.Hide<ProbePanelView>();

            LogAssert.Expect(LogType.Error, new Regex("未注册视图工厂"));
            Assert.That(windows.ShowAsync<ProbePanelView>().GetAwaiter().GetResult(), Is.Null, "未注册时不该产出实例");

            Assert.That(windows.TryGet<ProbePanelView>(out ProbePanelView view), Is.False);
            Assert.That(view, Is.Null);
        }

        [Test]
        public void WindowService_DestroyedInstance_IsEvictedAndRebuilt()
        {
            WindowService windows = NewWindowService(out GameObject root);
            int created = 0;
            windows.Register<ProbePanelView>(parent =>
            {
                created++;
                return NewPanelObject(parent).gameObject;
            });

            ProbePanelView first = windows.ShowAsync<ProbePanelView>().GetAwaiter().GetResult();
            Assert.That(first, Is.Not.Null);

            // 外部直接销毁面板物体, 缓存里只剩空壳, 判活后必须剔除并重建
            UnityEngine.Object.DestroyImmediate(first.gameObject);

            LogAssert.Expect(LogType.Warning, new Regex("已剔除缓存"));
            Assert.That(windows.TryGet(out ProbePanelView stale), Is.False, "实例已销毁不该继续缓存");

            ProbePanelView second = windows.ShowAsync<ProbePanelView>().GetAwaiter().GetResult();
            Assert.That(second, Is.Not.Null);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(created, Is.EqualTo(2), "销毁后应重新走工厂创建");
        }

        [Test]
        public void ModelRegistry_RegisterNonGeneric_ChecksContract_And_KeepsFirstInstance()
        {
            ModelRegistry registry = new ModelRegistry();
            List<string> journal = new List<string>();

            Assert.That(registry.Register(typeof(IProbeModelA), new ProbeModel("A", journal)), Is.Not.Null);
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(string.Join(">", journal), Is.EqualTo("A.Register"));

            LogAssert.Expect(LogType.Error, new Regex("未实现契约"));
            Assert.That(registry.Register(typeof(IProbeModelC), new ProbeModel("B", journal)), Is.Null, "契约不匹配不该入册");

            LogAssert.Expect(LogType.Error, new Regex("契约类型为空"));
            Assert.That(registry.Register(null, new ProbeModel("C", journal)), Is.Null);

            LogAssert.Expect(LogType.Error, new Regex("实例为空"));
            Assert.That(registry.Register(typeof(IProbeModelA), null), Is.Null);

            LogAssert.Expect(LogType.Error, new Regex("重复注册被忽略"));
            Assert.That(registry.Register(typeof(IProbeModelA), new ProbeModel("D", journal)), Is.Not.Null, "重复注册只报错不覆盖");
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(string.Join(">", journal), Is.EqualTo("A.Register"), "被拒绝的实例不该收到 OnRegister");

            registry.Shutdown();

            LogAssert.Expect(LogType.Error, new Regex("仓库已注销"));
            Assert.That(registry.Register(typeof(IProbeModelB), new ProbeModel("E", journal)), Is.Null);
        }

        [Test]
        public void WindowService_RegisterNonGeneric_RejectsBadArguments_And_StillShowsView()
        {
            WindowService windows = NewWindowService(out GameObject root);

            LogAssert.Expect(LogType.Error, new Regex("视图类型为空"));
            windows.Register(null, null);

            LogAssert.Expect(LogType.Error, new Regex("不是挂载在物体上的 IView 组件"));
            windows.Register(typeof(ProbeModel), parent => NewPanelObject(parent).gameObject);

            LogAssert.Expect(LogType.Error, new Regex("不是挂载在物体上的 IView 组件"));
            windows.Register(typeof(Transform), parent => NewPanelObject(parent).gameObject);

            LogAssert.Expect(LogType.Error, new Regex("视图工厂为空"));
            windows.Register(typeof(ProbePanelView), (Func<Transform, GameObject>)null);

            windows.Register(typeof(ProbePanelView), parent => NewPanelObject(parent).gameObject);

            LogAssert.Expect(LogType.Error, new Regex("已注册过视图工厂"));
            windows.Register(typeof(ProbePanelView), parent => NewPanelObject(parent).gameObject);

            ProbePanelView view = windows.ShowAsync<ProbePanelView>().GetAwaiter().GetResult();
            Assert.That(view, Is.Not.Null, "非泛型注册的工厂应能被泛型 Show 取到");
            Assert.That(windows.InstanceCount, Is.EqualTo(1));
        }

        [Test]
        public void MvvmHost_CollectModuleTypes_FindsProbeModulesAndSortsByOrder()
        {
            List<Type> types = MvvmHost.CollectModuleTypes();

            int low = types.IndexOf(typeof(QMvvmProbeModuleLow));
            int high = types.IndexOf(typeof(QMvvmProbeModuleHigh));

            Assert.That(low, Is.GreaterThanOrEqualTo(0), "测试程序集内的 [MvvmModule] 应被发现");
            Assert.That(high, Is.GreaterThanOrEqualTo(0));
            Assert.That(low, Is.LessThan(high), "Order 小的模块先装配");
        }

        [Test]
        public void MvvmHost_InvokeModule_DrivesStaticRegisterIntoRootServices()
        {
            MvvmRoot root = NewRoot();
            QMvvmProbeJournal.Clear();

            MvvmHost.InvokeModule(typeof(QMvvmProbeModule), root);

            Assert.That(QMvvmProbeJournal.Text(), Does.Contain("Module.Enter"));
            Assert.That(root.Models.Get<IProbeModelA>(), Is.Not.Null, "模块注册的模型应能直接从根节点取到");
        }

        [Test]
        public void MvvmHost_InvokeModule_BadConfiguration_ErrorsWithoutPropagating()
        {
            MvvmRoot root = NewRoot();

            LogAssert.Expect(LogType.Error, new Regex("业务模块类型为空"));
            MvvmHost.InvokeModule(null, null);

            LogAssert.Expect(LogType.Error, new Regex("运行根节点为空"));
            MvvmHost.InvokeModule(typeof(QMvvmProbeModule), null);

            LogAssert.Expect(LogType.Error, new Regex("缺少 public static void Register"));
            MvvmHost.InvokeModule(typeof(QMvvmProbeModuleMissing), root);

            LogAssert.Expect(LogType.Error, new Regex("探针装配断裂"));
            MvvmHost.InvokeModule(typeof(QMvvmProbeModuleThrowing), root);
        }
    }
}
