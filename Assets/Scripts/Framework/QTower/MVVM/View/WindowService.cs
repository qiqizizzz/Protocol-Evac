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
        {
            RegisterAsync<TView>(parent => UniTask.FromResult(factory(parent)));
        }

        /// <summary>
        /// 注册异步视图工厂，适用于 Addressables 等需要等待的资源加载
        /// </summary>
        /// <param name="factory">传入根节点, 返回视图物体的异步任务</param>
        public void RegisterAsync<TView>(Func<Transform, UniTask<GameObject>> factory) where TView : Component, IView
        {
            if (m_isDestroyed)
            {
                QLog.Error($"注册失败：窗口服务已销毁，目标 {typeof(TView).Name}");
                return;
            }

            if (factory == null)
            {
                QLog.Error($"注册失败：{typeof(TView).Name} 的视图工厂为空");
                return;
            }

            Type viewClass = typeof(TView);
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
            if (m_instances.TryGetValue(typeof(TView), out IView view))
                return view as TView;

            QLog.Error($"获取失败：{typeof(TView).Name} 尚未打开");
            return null;
        }

        public bool TryGet<TView>(out TView view) where TView : class, IView
        {
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

        private void TrackOpen(Type viewClass)
        {
            if (m_openOrder.Contains(viewClass)) return;

            m_openOrder.Add(viewClass);
        }
    }
}
