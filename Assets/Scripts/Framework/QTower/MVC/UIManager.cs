/*
 * ┌──────────────────────────────────┐
 * │  描    述: UI 管理器，负责 UI 视图注册、加载、缓存、打开、关闭与销毁
 * │  类    名: UIManager.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.Controller;
using Framework.QTower.View;
using UnityEngine;

namespace Framework.QTower
{
    public sealed class UIManager
    {
        private readonly Dictionary<ViewType, UIData> m_viewDatas = new();
        private readonly Dictionary<ViewType, UIBase> m_viewCache = new();
        private readonly Dictionary<ViewType, UIBase> m_openViews = new();

        // 注册 UI 的预制体、挂载根节点与所属控制器
        public bool Register<TView>(ViewType viewType, GameObject prefab, Transform parent, BaseController controller)
            where TView : UIBase
        {
            if (m_viewDatas.ContainsKey(viewType))
                return false;

            if (prefab == null)
                return false;

            if (parent == null)
                return false;

            m_viewDatas.Add(viewType, new UIData(typeof(TView), prefab, parent, controller));
            return true;
        }

        // 获取已创建的 UI 视图
        public TView GetView<TView>(ViewType viewType) where TView : UIBase
        {
            return GetView(viewType) as TView;
        }

        public UIBase GetView(ViewType viewType)
        {
            if (m_openViews.TryGetValue(viewType, out UIBase openView))
                return openView;

            m_viewCache.TryGetValue(viewType, out UIBase cachedView);
            return cachedView;
        }

        public bool IsOpen(ViewType viewType)
        {
            return m_openViews.ContainsKey(viewType);
        }

        // 加载并打开指定 UI，首次加载时自动挂载对应 View 脚本
        public TView Open<TView>(ViewType viewType, params object[] args) where TView : UIBase
        {
            return Open(viewType, args) as TView;
        }

        public UIBase Open(ViewType viewType, params object[] args)
        {
            if (m_openViews.TryGetValue(viewType, out UIBase openView))
                return openView;

            if (!m_viewDatas.TryGetValue(viewType, out UIData viewData))
                return null;

            UIBase view = GetOrCreateView(viewType, viewData);
            if (view == null)
                return null;

            m_openViews.Add(viewType, view);
            view.Open(args);
            view.Controller?.OnViewOpened(view);
            return view;
        }

        // 关闭指定 UI，但保留实例以便后续复用
        public void Close(ViewType viewType, params object[] args)
        {
            if (!m_openViews.TryGetValue(viewType, out UIBase view))
                return;

            m_openViews.Remove(viewType);
            view.Close(args);
            view.Controller?.OnViewClosed(view);
        }

        // 关闭当前已打开的全部 UI
        public void CloseAll(params object[] args)
        {
            List<ViewType> viewTypes = new(m_openViews.Keys);
            for (int i = viewTypes.Count - 1; i >= 0; i--)
                Close(viewTypes[i], args);
        }

        // 销毁指定 UI 实例并移除缓存
        public void DestroyView(ViewType viewType)
        {
            m_openViews.Remove(viewType);
            if (!m_viewCache.TryGetValue(viewType, out UIBase view))
                return;

            m_viewCache.Remove(viewType);
            Object.Destroy(view.gameObject);
        }

        // 注销 UI 注册数据与运行时实例
        public void Unregister(ViewType viewType)
        {
            DestroyView(viewType);
            m_viewDatas.Remove(viewType);
        }

        // 注销指定控制器注册的全部 UI
        public void UnregisterByController(BaseController controller)
        {
            List<ViewType> viewTypes = new();
            foreach (KeyValuePair<ViewType, UIData> pair in m_viewDatas)
            {
                if (pair.Value.Controller == controller)
                    viewTypes.Add(pair.Key);
            }

            foreach (ViewType viewType in viewTypes)
                Unregister(viewType);
        }

        // 销毁当前管理的全部 UI 实例
        public void Destroy()
        {
            foreach (UIBase view in m_viewCache.Values)
                Object.Destroy(view.gameObject);

            m_openViews.Clear();
            m_viewCache.Clear();
            m_viewDatas.Clear();
        }

        // 创建 UI 实例并在首次加载时完成 View 上下文注入
        private UIBase GetOrCreateView(ViewType viewType, UIData viewData)
        {
            if (m_viewCache.TryGetValue(viewType, out UIBase cachedView))
                return cachedView;

            GameObject viewObject = Object.Instantiate(viewData.Prefab, viewData.Parent);
            UIBase view = viewObject.GetComponent(viewData.ViewType) as UIBase;
            if (view == null)
                view = viewObject.AddComponent(viewData.ViewType) as UIBase;

            view.ViewType = viewType;
            view.Controller = viewData.Controller;
            view.Init();
            m_viewCache.Add(viewType, view);
            view.Controller?.OnViewLoaded(view);
            return view;
        }
    }
}
