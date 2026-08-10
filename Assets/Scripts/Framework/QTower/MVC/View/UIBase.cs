/*
 * ┌──────────────────────────────────┐
 * │  描    述: 运行时 UI 视图基类，统一 UI 生命周期与事件订阅
 * │  类    名: UIBase.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;
using Framework.QTower.Controller;

namespace Framework.QTower.View
{
    public abstract class UIBase : MonoBehaviour
    {
        public BaseController Controller { get; internal set; }
        public ViewType ViewType { get; internal set; }

        private bool m_isInitialized;

        public void Init()
        {
            if (m_isInitialized)
                return;

            OnInit();
            SubscribeViewEvents();
            m_isInitialized = true;
        }

        public void Open(params object[] args)
        {
            SetVisible(true);
            OnOpen(args);
        }

        public void Close(params object[] args)
        {
            OnClose(args);
            SetVisible(false);
        }

        // 设置面板显示状态
        public void SetVisible(bool isVisible)
        {
            if (gameObject.activeSelf == isVisible)
                return;

            gameObject.SetActive(isVisible);
        }

        private void OnDestroy()
        {
            if (!m_isInitialized)
                return;

            UnsubscribeViewEvents();
            OnDispose();
            m_isInitialized = false;
        }

        protected virtual void OnInit()
        {
        }

        protected virtual void OnOpen(params object[] args)
        {
        }

        protected virtual void OnClose(params object[] args)
        {
        }

        protected virtual void SubscribeViewEvents()
        {
        }

        protected virtual void UnsubscribeViewEvents()
        {
        }

        protected virtual void OnDispose()
        {
        }
    }
}
