/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 右侧视图，承载 Animation Event Inspector
 * │  类    名: AbilityRightView.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Framework.QTower.Editor.View;
using Module.Player.Window;
using Tools.Editor.AbilityComposer.Center.Event;
using Tools.Editor.AbilityComposer.Center.Timeline;
using Tools.Editor.AbilityComposer.Right.Event;
using Tools.Editor.AbilityComposer.Right.Window;
using UnityEngine.UIElements;
using Utils.log;

namespace Tools.Editor.AbilityComposer.Right
{
    public sealed class AbilityRightView : UIBaseEditor
    {
        private readonly VisualElement m_rootVisualElement;
        private AbilityEventInspectorView m_eventInspectorView;
        private AbilityWindowInspectorView m_windowInspectorView;
        private bool m_isControlsReady;

        public event Action<AbilityEventCategory> OnEventCategoryChanged;
        public event Action<string> OnEventReceiverTypeNameChanged;
        public event Action<string> OnEventFunctionNameChanged;
        public event Action<AbilityWindowType> OnWindowTypeChanged;
        public event Action<int, int> OnWindowFramesChanged;
        public event Action<float> OnWindowDamageChanged;

        // 注入右侧区域根节点
        public AbilityRightView(VisualElement rootVisualElement)
        {
            m_rootVisualElement = rootVisualElement;
        }

        // 创建右侧事件和窗口检查器页面
        protected override void OnEditorInit()
        {
            VisualElement workspace = m_rootVisualElement.Q<VisualElement>("event-inspector-workspace");
            if (workspace == null)
            {
                QLog.Error("配置 Ability Composer 右侧视图失败：缺少 Event Inspector 容器");
                return;
            }

            m_eventInspectorView = new AbilityEventInspectorView(workspace);
            m_windowInspectorView = new AbilityWindowInspectorView(workspace);
            m_eventInspectorView.Init();
            m_windowInspectorView.Init();
            m_isControlsReady = true;
        }

        // 刷新当前 Event Inspector 页面
        public void Refresh(AbilityTimelineData timelineData)
        {
            if (!m_isControlsReady)
                return;

            m_eventInspectorView.Refresh(timelineData);
            m_windowInspectorView.Refresh(timelineData);
        }

        // 更新右侧 Event Inspector 的 Function 下拉候选
        public void SetEventFunctionChoices(IReadOnlyDictionary<string, List<string>> functionChoices)
        {
            if (!m_isControlsReady)
                return;

            m_eventInspectorView.SetFunctionChoices(functionChoices);
        }

        protected override void SubscribeViewEvents()
        {
            if (!m_isControlsReady)
                return;

            m_eventInspectorView.OnCategoryChanged += RequestEventCategoryChanged;
            m_eventInspectorView.OnReceiverTypeNameChanged += RequestEventReceiverTypeNameChanged;
            m_eventInspectorView.OnFunctionNameChanged += RequestEventFunctionNameChanged;
            m_windowInspectorView.OnTypeChanged += RequestWindowTypeChanged;
            m_windowInspectorView.OnFramesChanged += RequestWindowFramesChanged;
            m_windowInspectorView.OnDamageChanged += RequestWindowDamageChanged;
        }

        protected override void UnsubscribeViewEvents()
        {
            if (!m_isControlsReady)
                return;

            m_eventInspectorView.OnCategoryChanged -= RequestEventCategoryChanged;
            m_eventInspectorView.OnReceiverTypeNameChanged -= RequestEventReceiverTypeNameChanged;
            m_eventInspectorView.OnFunctionNameChanged -= RequestEventFunctionNameChanged;
            m_windowInspectorView.OnTypeChanged -= RequestWindowTypeChanged;
            m_windowInspectorView.OnFramesChanged -= RequestWindowFramesChanged;
            m_windowInspectorView.OnDamageChanged -= RequestWindowDamageChanged;
            m_isControlsReady = false;
        }

        protected override void OnEditorDispose()
        {
            if (m_eventInspectorView != null)
                m_eventInspectorView.Destroy();

            if (m_windowInspectorView != null)
                m_windowInspectorView.Destroy();
        }

        // 转发事件分类编辑请求
        private void RequestEventCategoryChanged(AbilityEventCategory category) => OnEventCategoryChanged?.Invoke(category);

        // 转发事件接收类编辑请求
        private void RequestEventReceiverTypeNameChanged(string receiverTypeName) => OnEventReceiverTypeNameChanged?.Invoke(receiverTypeName);

        // 转发事件 Function 编辑请求
        private void RequestEventFunctionNameChanged(string functionName) => OnEventFunctionNameChanged?.Invoke(functionName);

        // 转发窗口类型编辑请求
        private void RequestWindowTypeChanged(AbilityWindowType type) => OnWindowTypeChanged?.Invoke(type);

        // 转发窗口帧范围编辑请求
        private void RequestWindowFramesChanged(int startFrame, int endFrame) => OnWindowFramesChanged?.Invoke(startFrame, endFrame);

        // 转发窗口伤害编辑请求
        private void RequestWindowDamageChanged(float damage) => OnWindowDamageChanged?.Invoke(damage);
    }
}
