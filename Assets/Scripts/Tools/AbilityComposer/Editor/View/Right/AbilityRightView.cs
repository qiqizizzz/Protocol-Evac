/*
 * ┌──────────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 右侧视图，承载 Animation Event Inspector
 * │  类    名: AbilityRightView.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Framework.QTower.Editor.View;
using Module.Ability.Data.Window;
using Tools.AbilityComposer.Editor.View.Center.Event;
using Tools.AbilityComposer.Editor.View.Center.Timeline;
using Tools.AbilityComposer.Editor.View.Right.Event;
using Tools.AbilityComposer.Editor.View.Right.Window;
using UnityEngine.UIElements;
using Utils.log;

namespace Tools.AbilityComposer.Editor.View.Right
{
    public sealed class AbilityRightView : UIBaseEditor
    {
        private enum InspectorTabType
        {
            Event,
            Window
        }

        private readonly VisualElement m_rootVisualElement;
        private VisualElement m_tabBar;
        private VisualElement m_eventWorkspace;
        private VisualElement m_windowWorkspace;
        private AbilityEventInspectorView m_eventInspectorView;
        private AbilityWindowInspectorView m_windowInspectorView;
        private Button m_eventTabButton;
        private Button m_windowTabButton;
        private InspectorTabType? m_activeTabType;
        private bool m_isEventTabOpen;
        private bool m_isWindowTabOpen;
        private bool m_isControlsReady;

        public event Action<AbilityEventCategory> OnEventCategoryChanged;
        public event Action<string> OnEventReceiverTypeNameChanged;
        public event Action<string> OnEventFunctionNameChanged;
        public event Action OnSaveEventRequested;
        public event Action<AbilityWindowDraftType> OnWindowTypeChanged;
        public event Action<AbilityWindowConfigSO> OnWindowConfigChanged;
        public event Action<int, int> OnWindowFramesChanged;
        public event Action<float> OnWindowDamageChanged;
        public event Action OnSaveWindowRequested;
        public event Action OnCloseEventInspectorRequested;
        public event Action OnCloseWindowInspectorRequested;

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

            workspace.Clear();
            m_tabBar = new VisualElement();
            m_tabBar.AddToClassList("ac-inspector-tab-bar");
            m_eventWorkspace = new VisualElement();
            m_eventWorkspace.AddToClassList("ac-inspector-tab-content");
            m_windowWorkspace = new VisualElement();
            m_windowWorkspace.AddToClassList("ac-inspector-tab-content");
            workspace.Add(m_tabBar);
            workspace.Add(m_eventWorkspace);
            workspace.Add(m_windowWorkspace);
            m_eventInspectorView = new AbilityEventInspectorView(m_eventWorkspace);
            m_windowInspectorView = new AbilityWindowInspectorView(m_windowWorkspace);
            m_eventInspectorView.Init();
            m_windowInspectorView.Init();
            m_isControlsReady = true;
        }

        // 刷新当前 Event Inspector 页面
        public void Refresh(AbilityTimelineData timelineData)
        {
            if (!m_isControlsReady)
                return;

            if (timelineData.IsWindowInspectorActive && timelineData.SelectedWindow != null)
                OpenTab(InspectorTabType.Window);
            else if (timelineData.SelectedEvent != null)
                OpenTab(InspectorTabType.Event);

            m_eventInspectorView.Refresh(timelineData);
            m_windowInspectorView.Refresh(timelineData);
            RefreshTabPresentation();
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
            m_eventInspectorView.OnSaveEventRequested += RequestSaveEvent;
            m_windowInspectorView.OnTypeChanged += RequestWindowTypeChanged;
            m_windowInspectorView.OnWindowConfigChanged += RequestWindowConfigChanged;
            m_windowInspectorView.OnFramesChanged += RequestWindowFramesChanged;
            m_windowInspectorView.OnDamageChanged += RequestWindowDamageChanged;
            m_windowInspectorView.OnSaveWindowRequested += RequestSaveWindow;
        }

        protected override void UnsubscribeViewEvents()
        {
            if (!m_isControlsReady)
                return;

            m_eventInspectorView.OnCategoryChanged -= RequestEventCategoryChanged;
            m_eventInspectorView.OnReceiverTypeNameChanged -= RequestEventReceiverTypeNameChanged;
            m_eventInspectorView.OnFunctionNameChanged -= RequestEventFunctionNameChanged;
            m_eventInspectorView.OnSaveEventRequested -= RequestSaveEvent;
            m_windowInspectorView.OnTypeChanged -= RequestWindowTypeChanged;
            m_windowInspectorView.OnWindowConfigChanged -= RequestWindowConfigChanged;
            m_windowInspectorView.OnFramesChanged -= RequestWindowFramesChanged;
            m_windowInspectorView.OnDamageChanged -= RequestWindowDamageChanged;
            m_windowInspectorView.OnSaveWindowRequested -= RequestSaveWindow;
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

        // 转发保存事件请求
        private void RequestSaveEvent() => OnSaveEventRequested?.Invoke();

        // 转发窗口类型编辑请求
        private void RequestWindowTypeChanged(AbilityWindowDraftType type) => OnWindowTypeChanged?.Invoke(type);

        // 转发窗口主体配置切换请求
        private void RequestWindowConfigChanged(AbilityWindowConfigSO windowConfig) => OnWindowConfigChanged?.Invoke(windowConfig);

        // 转发窗口帧范围编辑请求
        private void RequestWindowFramesChanged(int startFrame, int endFrame) => OnWindowFramesChanged?.Invoke(startFrame, endFrame);

        // 转发窗口伤害编辑请求
        private void RequestWindowDamageChanged(float damage) => OnWindowDamageChanged?.Invoke(damage);

        // 转发保存窗口轨道请求
        private void RequestSaveWindow() => OnSaveWindowRequested?.Invoke();

        // 打开指定检查器页签并将其置为当前页
        private void OpenTab(InspectorTabType tabType)
        {
            if (tabType == InspectorTabType.Event)
                m_isEventTabOpen = true;
            else
                m_isWindowTabOpen = true;

            m_activeTabType = tabType;
        }

        // 重新生成页签栏并显示当前激活页内容
        private void RefreshTabPresentation()
        {
            m_tabBar.Clear();
            if (m_isEventTabOpen)
                m_eventTabButton = CreateTabButton(InspectorTabType.Event, "●", "事件");

            if (m_isWindowTabOpen)
                m_windowTabButton = CreateTabButton(InspectorTabType.Window, "■", "窗口");

            if (!m_activeTabType.HasValue)
            {
                m_eventWorkspace.style.display = DisplayStyle.None;
                m_windowWorkspace.style.display = DisplayStyle.None;
                return;
            }

            m_eventWorkspace.style.display = m_activeTabType == InspectorTabType.Event ? DisplayStyle.Flex : DisplayStyle.None;
            m_windowWorkspace.style.display = m_activeTabType == InspectorTabType.Window ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // 创建包含小色标、标题和关闭按钮的检查器页签
        private Button CreateTabButton(InspectorTabType tabType, string marker, string title)
        {
            Button tabButton = new Button();
            tabButton.AddToClassList("ac-inspector-tab-button");
            if (m_activeTabType == tabType)
                tabButton.AddToClassList("ac-inspector-tab-button-selected");

            Label markerLabel = new Label(marker);
            markerLabel.AddToClassList(tabType == InspectorTabType.Event ? "ac-inspector-tab-marker-event" : "ac-inspector-tab-marker-window");
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("ac-inspector-tab-label");
            Button closeButton = new Button();
            closeButton.text = "×";
            closeButton.tooltip = $"关闭{title}页签";
            closeButton.AddToClassList("ac-inspector-tab-close-button");
            closeButton.RegisterCallback<ClickEvent>(clickEvent =>
            {
                clickEvent.StopPropagation();
                CloseTab(tabType);
            });
            tabButton.Add(markerLabel);
            tabButton.Add(titleLabel);
            tabButton.Add(closeButton);
            tabButton.clicked += () => SelectTab(tabType);
            m_tabBar.Add(tabButton);
            return tabButton;
        }

        // 切换当前显示的检查器页签
        private void SelectTab(InspectorTabType tabType)
        {
            m_activeTabType = tabType;
            RefreshTabPresentation();
        }

        // 关闭指定检查器页签并让 Controller 清理对应选择
        private void CloseTab(InspectorTabType tabType)
        {
            if (tabType == InspectorTabType.Event)
            {
                m_isEventTabOpen = false;
                OnCloseEventInspectorRequested?.Invoke();
            }
            else
            {
                m_isWindowTabOpen = false;
                OnCloseWindowInspectorRequested?.Invoke();
            }

            if (m_activeTabType == tabType)
                m_activeTabType = m_isEventTabOpen ? InspectorTabType.Event : m_isWindowTabOpen ? InspectorTabType.Window : null;

            RefreshTabPresentation();
        }
    }
}
