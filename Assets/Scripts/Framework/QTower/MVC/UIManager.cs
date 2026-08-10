/*
 * ┌──────────────────────────────────┐
 * │  描    述: UI 管理器，负责 UI 视图注册、加载、缓存、打开、关闭与销毁
 * │  类    名: UIManager.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Framework.QTower.Controller;
using Framework.QTower.View;
using UnityEngine;
using Utils.log;
using ResManager = global::Common.ResManager;

namespace Framework.QTower
{
    public sealed class UIManager
    {
        private readonly Dictionary<ViewType, UIData> m_viewDatas = new();
        private readonly Dictionary<ViewType, UIBase> m_viewCache = new();
        private readonly Dictionary<ViewType, UIBase> m_openViews = new();
        private readonly Dictionary<ViewType, List<Action<UIBase>>> m_openCallbacks = new();
        private readonly HashSet<ViewType> m_loadingViews = new();

        private Transform m_uiRoot;
        private bool m_isDestroyed;

        // 设置 UI 统一挂载根节点
        public void SetRoot(Transform uiRoot)
        {
            if (uiRoot == null)
            {
                QLog.Error("设置 UI 根节点失败：UIRoot 为空");
                return;
            }

            m_uiRoot = uiRoot;
        }

        // 注册 UI 视图类型与所属控制器
        public bool Register<TView>(ViewType viewType, BaseController controller)
            where TView : UIBase
        {
            if (viewType == ViewType.None)
            {
                QLog.Error("注册 UI 失败：ViewType 不能为 None");
                return false;
            }

            if (m_viewDatas.ContainsKey(viewType))
            {
                QLog.Error($"注册 UI 失败：{viewType} 已注册");
                return false;
            }

            m_viewDatas.Add(viewType, new UIData(typeof(TView), controller));
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

        // 异步加载并打开指定 UI
        public void OpenAsync<TView>(ViewType viewType, Action<TView> onCompleted, params object[] args)
            where TView : UIBase
        {
            OpenAsync(viewType, view => onCompleted?.Invoke(view as TView), args);
        }

        // 异步加载并打开指定 UI
        public void OpenAsync(ViewType viewType, Action<UIBase> onCompleted, params object[] args)
        {
            if (m_isDestroyed)
            {
                QLog.Error($"打开 UI 失败：UIManager 已销毁，目标 {viewType}");
                onCompleted?.Invoke(null);
                return;
            }

            if (m_openViews.TryGetValue(viewType, out UIBase openView))
            {
                onCompleted?.Invoke(openView);
                return;
            }

            if (m_viewCache.TryGetValue(viewType, out UIBase cachedView))
            {
                OpenView(viewType, cachedView, args);
                onCompleted?.Invoke(cachedView);
                return;
            }

            if (!m_viewDatas.TryGetValue(viewType, out UIData viewData))
            {
                QLog.Error($"打开 UI 失败：{viewType} 未注册");
                onCompleted?.Invoke(null);
                return;
            }

            AddOpenCallback(viewType, onCompleted);
            if (m_loadingViews.Contains(viewType))
                return;

            if (m_uiRoot == null)
            {
                QLog.Error($"打开 UI 失败：{viewType} 未设置 UIRoot");
                CompleteOpen(viewType, null);
                return;
            }

            m_loadingViews.Add(viewType);
            ResManager.InstantiateAsync(
                viewType.ToString(),
                viewObject => HandleViewLoaded(viewType, viewData, viewObject, args),
                m_uiRoot);
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
            ResManager.UnLoadInstance(view.gameObject);
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
            m_isDestroyed = true;
            foreach (UIBase view in m_viewCache.Values)
                ResManager.UnLoadInstance(view.gameObject);

            m_openViews.Clear();
            m_viewCache.Clear();
            m_viewDatas.Clear();
            m_openCallbacks.Clear();
            m_loadingViews.Clear();
        }

        // 记录异步打开完成回调
        private void AddOpenCallback(ViewType viewType, Action<UIBase> onCompleted)
        {
            if (onCompleted == null)
                return;

            if (!m_openCallbacks.TryGetValue(viewType, out List<Action<UIBase>> callbacks))
            {
                callbacks = new List<Action<UIBase>>();
                m_openCallbacks.Add(viewType, callbacks);
            }

            callbacks.Add(onCompleted);
        }

        // 完成 UI 实例加载并注入 View 上下文
        private void HandleViewLoaded(ViewType viewType, UIData viewData, GameObject viewObject, object[] args)
        {
            m_loadingViews.Remove(viewType);
            if (m_isDestroyed)
            {
                ResManager.UnLoadInstance(viewObject);
                return;
            }

            if (viewObject == null)
            {
                CompleteOpen(viewType, null);
                return;
            }

            UIBase view = viewObject.GetComponent(viewData.ViewClass) as UIBase;
            if (view == null)
                view = viewObject.AddComponent(viewData.ViewClass) as UIBase;

            if (view == null)
            {
                QLog.Error($"加载 UI 失败：{viewType} 未能挂载 {viewData.ViewClass.Name}");
                ResManager.UnLoadInstance(viewObject);
                CompleteOpen(viewType, null);
                return;
            }

            view.ViewType = viewType;
            view.Controller = viewData.Controller;
            view.Init();
            m_viewCache.Add(viewType, view);
            view.Controller?.OnViewLoaded(view);
            OpenView(viewType, view, args);
            CompleteOpen(viewType, view);
        }

        // 打开已创建的 UI 实例
        private void OpenView(ViewType viewType, UIBase view, object[] args)
        {
            m_openViews.Add(viewType, view);
            view.Open(args);
            view.Controller?.OnViewOpened(view);
        }

        // 调用指定 UI 的全部打开完成回调
        private void CompleteOpen(ViewType viewType, UIBase view)
        {
            if (!m_openCallbacks.TryGetValue(viewType, out List<Action<UIBase>> callbacks))
                return;

            m_openCallbacks.Remove(viewType);
            foreach (Action<UIBase> callback in callbacks)
                callback.Invoke(view);
        }
    }
}
