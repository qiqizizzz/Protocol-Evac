/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM列表单元绑定测试，覆盖父子作用域级联、单元复用边界与异常回调影响面
 * │  类    名: QMvvmCellScopeTests.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Framework.QTower.MVVM.Binding;
using Framework.QTower.MVVM.ViewModel;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.MVVM
{
    // 网格单元探针模型, 按到达顺序记录框架生命周期回调
    internal sealed class CellProbeViewModel : ViewModelBase
    {
        private readonly List<string> m_sequence = new List<string>();
        private readonly ReactiveProperty<string> m_label;
        private readonly ReactiveProperty<int> m_count;
        private readonly Signal m_flash;

        public IReadOnlyReactiveProperty<string> Label => m_label;
        public IReadOnlyReactiveProperty<int> Count => m_count;
        public ISignal Flash => m_flash;
        public string Sequence => string.Join(">", m_sequence);

        public CellProbeViewModel(string initialLabel)
        {
            m_label = CreateProperty(initialLabel);
            m_count = CreateProperty(0);
            m_flash = CreateSignal();
        }

        // 测试驱动的属性写入, 对外只暴露只读接口
        public void SetLabel(string label) => m_label.Set(label);

        public void SetCount(int count) => m_count.Set(count);

        public void FireFlash() => m_flash.Fire();

        protected override void OnBindCore() => m_sequence.Add("OnBind");

        protected override void OnUnbindCore() => m_sequence.Add("OnUnbind");

        protected override void OnDisposeCore() => m_sequence.Add("OnDispose");
    }

    /// <summary>
    /// MVVM 列表单元测试：背包格子这类「面板 + 大量行物体」场景的作用域级联与复用边界
    /// </summary>
    public class QMvvmCellScopeTests
    {
        private readonly List<GameObject> m_objects = new List<GameObject>();

        // 造一个挂在宿主上的独立作用域, 时序对齐运行时的 Awake -> OnEnable
        private BindingScope NewScope(string objectName)
        {
            GameObject host = NewHost(objectName);
            BindingScope scope = host.AddComponent<BindingScope>();
            DriveInstantiate(host);
            return scope;
        }

        // 造一个单元物体并登记为父作用域的子女
        private BindingScope NewChildScope(BindingScope parent, string cellName)
        {
            GameObject cell = NewHost(cellName);
            cell.transform.SetParent(parent.transform, false);
            BindingScope child = parent.CreateChild(cell);
            DriveInstantiate(cell);
            return child;
        }

        private GameObject NewHost(string objectName)
        {
            GameObject host = new GameObject(objectName);
            m_objects.Add(host);
            return host;
        }

        // EditMode 不自动派发 Unity 生命周期消息, 下面几个方法把时序对齐到运行时
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

        private static void DriveInstantiate(GameObject host)
        {
            InvokeMessage(host, "Awake");
            InvokeMessage(host, "OnEnable");
        }

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
            for (int i = m_objects.Count - 1; i >= 0; i--)
            {
                GameObject target = m_objects[i];
                m_objects.RemoveAt(i);
                if (target == null) continue;

                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ParentEnd_CascadesToChild_AndChildReplaysLatestValue()
        {
            BindingScope parent = NewScope("Panel");
            BindingScope child = NewChildScope(parent, "Cell_0_0");
            CellProbeViewModel viewModel = new CellProbeViewModel("empty");
            child.Bind(viewModel);

            int applied = 0;
            string last = null;
            child.Bind(viewModel.Label, value => { applied++; last = value; });
            Assert.That(applied, Is.EqualTo(1), "登记即回放当前值");
            Assert.That(child.IsActive, Is.True);

            parent.End();
            Assert.That(child.IsActive, Is.False, "父作用域收敛时子作用域必须先收敛");

            viewModel.SetLabel("rifle");
            Assert.That(applied, Is.EqualTo(1), "隐藏期间单元不应收到回调");
            Assert.That(last, Is.EqualTo("empty"));

            parent.Begin();
            Assert.That(child.IsActive, Is.True, "父重开时子作用域应一起重放");
            Assert.That(applied, Is.EqualTo(2), "重开只补一次最新值");
            Assert.That(last, Is.EqualTo("rifle"));
        }

        [Test]
        public void CellDestroyed_KeepsPanelBindingsAlive()
        {
            BindingScope parent = NewScope("Panel");
            CellProbeViewModel panelModel = new CellProbeViewModel("panel");
            parent.Bind(panelModel);

            int panelHits = 0;
            parent.Bind(panelModel.Label, _ => panelHits++);

            BindingScope child = NewChildScope(parent, "Cell_0_0");
            child.Bind(new CellProbeViewModel("cell"));

            DriveDestroy(child.gameObject);
            UnityEngine.Object.DestroyImmediate(child.gameObject);

            panelModel.SetLabel("changed");
            Assert.That(panelHits, Is.EqualTo(2), "单元物体被销毁不应影响面板自身的绑定");

            Assert.DoesNotThrow(() => parent.End(), "子登记表里的空壳必须被安全剔除");
            Assert.That(parent.IsActive, Is.False);
        }

        [Test]
        public void CellScope_SecondBind_IsRejectedAndKeepsFirstViewModel()
        {
            BindingScope parent = NewScope("Panel");
            BindingScope child = NewChildScope(parent, "Cell_0_0");
            CellProbeViewModel first = new CellProbeViewModel("first");
            CellProbeViewModel second = new CellProbeViewModel("second");

            ViewModelLease lease = child.Bind(first, ViewModelOwnership.External);
            LogAssert.Expect(LogType.Error, new Regex("作用域已持有视图模型租约"));
            ViewModelLease again = child.Bind(second, ViewModelOwnership.External);

            Assert.That(again, Is.SameAs(lease), "重复绑定返回已有租约");
            Assert.That(second.IsBound, Is.False, "被拒的模型不应进入绑定态");
            Assert.That(first.IsBound, Is.True);
        }

        [Test]
        public void ExternalLease_TornDownCellViewModelCannotBeReused()
        {
            BindingScope parent = NewScope("Panel");
            BindingScope child = NewChildScope(parent, "Cell_0_0");
            CellProbeViewModel shared = new CellProbeViewModel("shared");
            ViewModelLease lease = child.Bind(shared, ViewModelOwnership.External);

            LogAssert.Expect(LogType.Log, new Regex("External 视图模型仅解绑"));
            lease.Dispose();

            Assert.That(shared.Sequence, Is.EqualTo("OnBind>OnUnbind>OnDispose"));
            Assert.That(shared.IsDisposed, Is.True, "External 只免 Dispose(), 免不掉 OnDispose");

            LogAssert.Expect(LogType.Error, new Regex("视图模型已被释放"));
            Assert.That(NewScope("OtherPanel").Bind(shared, ViewModelOwnership.External), Is.Null,
                "拆过的单元模型不能交给下一个单元");
        }

        [Test]
        public void HideThenShow_SignalBinding_DoesNotDoubleSubscribe()
        {
            BindingScope scope = NewScope("PanelScope");
            CellProbeViewModel viewModel = new CellProbeViewModel("v");
            scope.Bind(viewModel);

            int hits = 0;
            scope.Bind(viewModel.Flash, () => hits++);
            Assert.That(scope.ActiveBindingCount, Is.EqualTo(1), "一条信号绑定占一个活跃计数");

            viewModel.FireFlash();
            Assert.That(hits, Is.EqualTo(1));

            DriveActiveState(scope.gameObject, false);
            viewModel.FireFlash();
            Assert.That(hits, Is.EqualTo(1), "隐藏期间信号不应回调单元");

            DriveActiveState(scope.gameObject, true);
            viewModel.FireFlash();
            Assert.That(hits, Is.EqualTo(2), "重开只补订阅一次");
            Assert.That(scope.ActiveBindingCount, Is.EqualTo(1), "重放不应让活跃计数漂移");

            scope.NotifyClosed();
            scope.NotifyReopened();
            viewModel.FireFlash();
            Assert.That(hits, Is.EqualTo(3), "关闭重开同样不重复订阅");
        }

        [Test]
        public void HiddenWrites_ReplayOnlyTheLatestValueOnShow()
        {
            BindingScope scope = NewScope("PanelScope");
            CellProbeViewModel viewModel = new CellProbeViewModel("v0");
            scope.Bind(viewModel);

            List<string> applied = new List<string>();
            scope.Bind(viewModel.Label, value => applied.Add(value));
            Assert.That(applied, Is.EqualTo(new[] { "v0" }));

            DriveActiveState(scope.gameObject, false);
            viewModel.SetLabel("v1");
            viewModel.SetLabel("v2");
            viewModel.SetLabel("v3");
            Assert.That(applied.Count, Is.EqualTo(1), "隐藏期间的写入不回调 View");

            DriveActiveState(scope.gameObject, true);
            Assert.That(applied, Is.EqualTo(new[] { "v0", "v3" }), "重开按最新值补一次, 中间值不重放");
        }

        [Test]
        public void ThrowingApply_KillsOnlyThatBinding_OthersKeepWorking()
        {
            // 异常回调的堆栈由 UniTask 兜底记录, 本用例只关心可观测行为, 不让日志判定失败
            LogAssert.ignoreFailingMessages = true;
            BindingScope scope = NewScope("PanelScope");
            CellProbeViewModel viewModel = new CellProbeViewModel("ok");
            scope.Bind(viewModel);

            int brokenHits = 0;
            int aliveHits = 0;
            scope.Bind(viewModel.Label, value =>
            {
                brokenHits++;
                if (value == "boom") throw new InvalidOperationException("单元渲染异常");
            });
            scope.Bind(viewModel.Count, _ => aliveHits++);

            try
            {
                viewModel.SetLabel("boom");
                Assert.That(brokenHits, Is.EqualTo(2), "异常前回调本身已到达");

                viewModel.SetLabel("after");
                Assert.That(brokenHits, Is.EqualTo(2), "异常之后该条绑定不再收到回调");

                viewModel.SetCount(5);
                Assert.That(aliveHits, Is.EqualTo(2), "同作用域的其它绑定不受影响");
                Assert.That(scope.IsActive, Is.True, "单元渲染异常不应让面板作用域失活");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        [Test]
        public void CreateChild_OnOwnGameObject_ReturnsItself()
        {
            BindingScope parent = NewScope("Panel");

            Assert.That(parent.CreateChild(parent.gameObject), Is.SameAs(parent), "同一物体不给自己当子女");
        }
    }
}
