/*
 * ┌──────────────────────────────────┐
 * │  描    述: 窗口服务，负责 MVVM 视图的注册、实例缓存与显示关闭
 * │  类    名: WindowService.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Framework.QTower.MVVM.View.Interfaces;
using UnityEngine;
using Utils.log;

namespace Framework.QTower.MVVM.View
{
    /// <summary>
    /// 窗口服务：视图创建走工厂委托，本类不感知资源加载方式，也不依赖任何 MVC 组件
    /// </summary>
    public sealed class WindowService
    {
        private readonly Dictionary<Type, Func<Transform, UniTask<GameObject>>> m_factories =
            new Dictionary<Type, Func<Transform, UniTask<GameObject>>>();
        private readonly Dictionary<Type, IView> m_instances = new Dictionary<Type, IView>();
        private readonly List<Type> m_openOrder = new List<Type>();

        private readonly Transform m_root;

        private bool m_isDestroyed;

        public Transform Root => m_root;
        public int InstanceCount => m_instances.Count;

        public WindowService(Transform root)
        {
            if (root == null)
                QLog.Error("窗口服务创建失败：根节点为空");

            m_root = root;
        }

        /// <summary>
        /// 注册同步视图工厂，适用于运行期搭建或已加载好的预制体
        /// </summary>
        /// <param name="factory">传入根节点, 返回视图物体</param>
        public void Register<TView>(Func<Transform, GameObject> factory) where TView : Component, IView
            => Register(typeof(TView), factory);

        /// <summary>
        /// 非泛型注册入口，供反射式装配调用，校验规则与泛型版完全一致
        /// </summary>
        /// <param name="viewClass">视图组件类型，必须同时是 Component 与 IView</param>
        /// <param name="factory">传入根节点, 返回视图物体</param>
        public void Register(Type viewClass, Func<Transform, GameObject> factory)
            => RegisterAsync(viewClass, factory == null
                ? null
                : (Func<Transform, UniTask<GameObject>>)(parent => UniTask.FromResult(factory(parent))));

        /// <summary>
        /// 注册异步视图工厂，适用于 Addressables 等需要等待的资源加载
        /// </summary>
        /// <param name="factory">传入根节点, 返回视图物体的异步任务</param>
        public void RegisterAsync<TView>(Func<Transform, UniTask<GameObject>> factory) where TView : Component, IView
            => RegisterAsync(typeof(TView), factory);

        /// <summary>
        /// 非泛型异步注册入口，供反射式装配调用
        /// </summary>
        /// <param name="viewClass">视图组件类型，必须同时是 Component 与 IView</param>
        /// <param name="factory">传入根节点, 返回视图物体的异步任务</param>
        public void RegisterAsync(Type viewClass, Func<Transform, UniTask<GameObject>> factory)
        {
            if (m_isDestroyed)
            {
                QLog.Error($"注册失败：窗口服务已销毁，目标 {NameOf(viewClass)}");
                return;
            }

            if (viewClass == null)
            {
                QLog.Error("注册失败：视图类型为空");
                return;
            }

            if (!typeof(Component).IsAssignableFrom(viewClass) || !typeof(IView).IsAssignableFrom(viewClass))
            {
                QLog.Error($"注册失败：{viewClass.Name} 不是挂载在物体上的 IView 组件");
                return;
            }

            if (factory == null)
            {
                QLog.Error($"注册失败：{viewClass.Name} 的视图工厂为空");
                return;
            }

            if (m_factories.ContainsKey(viewClass))
            {
                QLog.Error($"注册失败：{viewClass.Name} 已注册过视图工厂");
                return;
            }

            m_factories.Add(viewClass, factory);
        }
/// <summary>
        /// 显示视图，未创建则走工厂实例化，已创建则直接复用
        /// </summary>
        /// <param name="args">打开参数</param>
        public async UniTask<TView> ShowAsync<TView>(params object[] args) where TView : Component, IView
        {
            Type viewClass = typeof(TView);
            if (m_isDestroyed)
            {
                QLog.Error($"显示失败：窗口服务已销毁，目标 {viewClass.Name}");
                return null;
            }

            // 实例可能已被外部 Destroy, 空壳缓存先剔除, 下面直接走工厂重建
            EvictIfDead(viewClass);

            if (m_instances.TryGetValue(viewClass, out IView cached))
            {
                TView reusable = cached as TView;
                if (reusable == null)
                {
                    QLog.Error($"显示失败：{viewClass.Name} 缓存类型不匹配，实际 {cached.GetType().Name}");
                    return null;
                }

                reusable.Show(args);
                TrackOpen(viewClass);
                return reusable;
            }

            if (!m_factories.TryGetValue(viewClass, out Func<Transform, UniTask<GameObject>> factory))
            {
                QLog.Error($"显示失败：{viewClass.Name} 未注册视图工厂");
                return null;
            }

            GameObject viewObject = await factory(m_root);
            if (viewObject == null)
            {
                QLog.Error($"显示失败：{viewClass.Name} 的视图工厂未产出物体");
                return null;
            }

            // 等待期间服务被销毁, 就地销毁产物, 不留孤儿节点
            if (m_isDestroyed)
            {
                UnityEngine.Object.Destroy(viewObject);
                return null;
            }

            TView view = viewObject.GetComponent<TView>();
            if (view == null)
            {
                QLog.Error($"显示失败：{viewObject.name} 上没有视图组件 {viewClass.Name}");
                UnityEngine.Object.Destroy(viewObject);
                return null;
            }

            m_instances.Add(viewClass, view);
            view.Show(args);
            TrackOpen(viewClass);
            return view;
        }

        /// <summary>
        /// 取已打开的视图实例，未打开时直接报错
        /// </summary>
        public TView Get<TView>() where TView : class, IView
        {
            EvictIfDead(typeof(TView));

            if (m_instances.TryGetValue(typeof(TView), out IView view))
                return view as TView;

            QLog.Error($"获取失败：{typeof(TView).Name} 尚未打开");
            return null;
        }

        public bool TryGet<TView>(out TView view) where TView : class, IView
        {
            EvictIfDead(typeof(TView));

            view = null;
            return m_instances.TryGetValue(typeof(TView), out IView cached) && (view = cached as TView) != null;
        }

        /// <summary>
        /// 隐藏视图，实例与视图模型状态全部保留
        /// </summary>
        public void Hide<TView>() where TView : class, IView
        {
            if (!TryGet(out TView view))
            {
                QLog.Error($"隐藏失败：{typeof(TView).Name} 尚未打开");
                return;
            }

            view.Hide();
        }

        /// <summary>
        /// 关闭视图，视图模型解绑，实例保留供下次复用
        /// </summary>
        /// <param name="args">关闭参数</param>
        public void Close<TView>(params object[] args) where TView : class, IView
        {
            if (!TryGet(out TView view))
            {
                QLog.Error($"关闭失败：{typeof(TView).Name} 尚未打开");
                return;
            }

            view.Close(args);
            m_openOrder.Remove(typeof(TView));
        }

        // 按打开顺序的逆序隐藏全部视图
        public void HideAll()
        {
            for (int i = m_openOrder.Count - 1; i >= 0; i--)
            {
                if (m_instances.TryGetValue(m_openOrder[i], out IView view))
                    view.Hide();
            }
        }

        // 按打开顺序的逆序关闭全部视图
        public void CloseAll()
        {
            List<Type> order = new List<Type>(m_openOrder);
            for (int i = order.Count - 1; i >= 0; i--)
            {
                if (!m_instances.TryGetValue(order[i], out IView view)) continue;

                view.Close();
            }

            m_openOrder.Clear();
        }

        /// <summary>
        /// 销毁全部视图实例并清空注册，根节点随之交还给上层
        /// </summary>
        public void DestroyAll()
        {
            if (m_isDestroyed) return;

            foreach (IView view in m_instances.Values)
            {
                if (view is Component component && component != null)
                    UnityEngine.Object.Destroy(component.gameObject);
            }

            m_instances.Clear();
            m_openOrder.Clear();
            m_factories.Clear();
            m_isDestroyed = true;
        }

        // 剔除已被 Destroy 的视图缓存：组件销毁后字典里只剩空壳引用，留着会让后续 Show 永远拿不到实例
        private bool EvictIfDead(Type viewClass)
        {
            if (!m_instances.TryGetValue(viewClass, out IView cached)) return false;

            // 非组件视图不受 Unity 生命周期影响, 一律视为存活
            if (cached is not Component component || component != null) return false;

            m_instances.Remove(viewClass);
            m_openOrder.Remove(viewClass);
            QLog.Warning($"视图实例已被销毁，已剔除缓存：{viewClass.Name}");

            return true;
        }

        // 反射入口可能传空类型, 日志给可定位的占位而不是抛空引用
        private static string NameOf(Type viewClass) => viewClass == null ? "空类型" : viewClass.Name;

        private void TrackOpen(Type viewClass)
        {
            if (m_openOrder.Contains(viewClass)) return;

            m_openOrder.Add(viewClass);
        }
    }
}
