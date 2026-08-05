/*
 * ┌──────────────────────────────────┐
 * │  描    述: 控制器基类，统一控制器生命周期
 * │  类    名: BaseController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Framework.QTower.Event;

namespace Framework.QTower.Controller
{
    public abstract class BaseController
    {
        private readonly Dictionary<string, Action> m_events = new();

        public void Init()
        {
            OnInit();
            RegisterModuleEvent();
        }

        public void Destroy()
        {
            RemoveModuleEvent();
            OnDestroy();
        }

        #region 生命周期
        // 初始化派生控制器
        protected virtual void OnInit()
        {
        }
        
        public virtual void Tick(float deltaTime) { }

        // 销毁派生控制器持有的运行资源
        protected virtual void OnDestroy()
        {
        }
        #endregion

        // 注册当前控制器监听的模块事件
        public void RegisterEvent<TEvent>(string eventName, Action<TEvent> callback)
        {
            EventManager.RegisterEvent(eventName, callback);
            Action removeEvent = () => EventManager.UnregisterEvent(eventName, callback);

            if (m_events.TryGetValue(eventName, out Action registered))
            {
                m_events[eventName] = registered + removeEvent;
                return;
            }

            m_events.Add(eventName, removeEvent);
        }

        // 移除当前控制器监听的模块事件
        public void UnregisterEvent(string eventName)
        {
            if (!m_events.TryGetValue(eventName, out Action removeEvent))
                return;

            removeEvent.Invoke();
            m_events.Remove(eventName);
        }

        // 初始化模块事件
        protected virtual void RegisterModuleEvent()
        {
        }

        // 移除模块事件
        private void RemoveModuleEvent()
        {
            foreach (Action removeEvent in m_events.Values)
                removeEvent.Invoke();

            m_events.Clear();
        }
    }
}
