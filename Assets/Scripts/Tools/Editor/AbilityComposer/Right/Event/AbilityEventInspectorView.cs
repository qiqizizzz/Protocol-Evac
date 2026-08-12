/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 动画事件检查器页面，编辑选中事件的分类与 Function
 * │  类    名: AbilityEventInspectorView.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Framework.QTower.Editor.View;
using Tools.Editor.AbilityComposer.Center.Event;
using Tools.Editor.AbilityComposer.Center.Timeline;
using UnityEngine.UIElements;

namespace Tools.Editor.AbilityComposer.Right.Event
{
    public sealed class AbilityEventInspectorView : UIBaseEditor
    {
        private static readonly List<string> S_CategoryChoices = new List<string>
        {
            nameof(AbilityEventCategory.Default),
            nameof(AbilityEventCategory.Gameplay),
            nameof(AbilityEventCategory.Vfx),
            nameof(AbilityEventCategory.Audio)
        };

        private readonly VisualElement m_rootVisualElement;
        private DropdownField m_categoryField;
        private Label m_frameValueLabel;
        private Label m_timeValueLabel;
        private TextField m_functionField;
        private Label m_emptyStateLabel;

        public event Action<AbilityEventCategory> OnCategoryChanged;
        public event Action<string> OnFunctionNameChanged;

        // 注入 Event Inspector 页面容器
        public AbilityEventInspectorView(VisualElement rootVisualElement)
        {
            m_rootVisualElement = rootVisualElement;
        }

        // 创建 Event Inspector 控件
        protected override void OnEditorInit()
        {
            m_rootVisualElement.Clear();
            m_emptyStateLabel = new Label("选择时间轴事件后，在此处编辑事件");
            m_emptyStateLabel.AddToClassList("ac-inspector-empty-state");
            m_categoryField = new DropdownField("Category", S_CategoryChoices, 0);
            m_categoryField.AddToClassList("ac-inspector-field");
            m_frameValueLabel = CreateReadOnlyValue("Frame");
            m_timeValueLabel = CreateReadOnlyValue("Time");
            m_functionField = new TextField("Function");
            m_functionField.isDelayed = true;
            m_functionField.AddToClassList("ac-inspector-field");
            m_rootVisualElement.Add(m_emptyStateLabel);
            m_rootVisualElement.Add(m_categoryField);
            m_rootVisualElement.Add(m_frameValueLabel);
            m_rootVisualElement.Add(m_timeValueLabel);
            m_rootVisualElement.Add(m_functionField);
            SetInspectorVisible(false);
        }

        // 刷新选中事件的 Inspector 字段
        public void Refresh(AbilityTimelineData timelineData)
        {
            AbilityEventDraft selectedEvent = timelineData.SelectedEvent;
            bool hasSelectedEvent = selectedEvent != null;
            SetInspectorVisible(hasSelectedEvent);
            if (!hasSelectedEvent)
                return;

            m_categoryField.SetValueWithoutNotify(selectedEvent.Category.ToString());
            m_frameValueLabel.text = $"Frame  {selectedEvent.Frame}";
            m_timeValueLabel.text = $"Time  {selectedEvent.Frame / timelineData.FrameRate:0.###}";
            m_functionField.SetValueWithoutNotify(selectedEvent.FunctionName);
        }

        protected override void SubscribeViewEvents()
        {
            m_categoryField.RegisterValueChangedCallback(HandleCategoryChanged);
            m_functionField.RegisterValueChangedCallback(HandleFunctionNameChanged);
        }

        protected override void UnsubscribeViewEvents()
        {
            m_categoryField.UnregisterValueChangedCallback(HandleCategoryChanged);
            m_functionField.UnregisterValueChangedCallback(HandleFunctionNameChanged);
        }

        // 创建只读显示字段
        private Label CreateReadOnlyValue(string label)
        {
            Label valueLabel = new Label(label);
            valueLabel.AddToClassList("ac-inspector-readonly-value");
            return valueLabel;
        }

        // 切换空状态和编辑字段显示
        private void SetInspectorVisible(bool hasSelectedEvent)
        {
            m_emptyStateLabel.style.display = hasSelectedEvent ? DisplayStyle.None : DisplayStyle.Flex;
            m_categoryField.style.display = hasSelectedEvent ? DisplayStyle.Flex : DisplayStyle.None;
            m_frameValueLabel.style.display = hasSelectedEvent ? DisplayStyle.Flex : DisplayStyle.None;
            m_timeValueLabel.style.display = hasSelectedEvent ? DisplayStyle.Flex : DisplayStyle.None;
            m_functionField.style.display = hasSelectedEvent ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // 转换分类下拉框的选择结果
        private void HandleCategoryChanged(ChangeEvent<string> changeEvent)
        {
            if (Enum.TryParse(changeEvent.newValue, out AbilityEventCategory category))
                OnCategoryChanged?.Invoke(category);
        }

        // 转发 Function 文本编辑结果
        private void HandleFunctionNameChanged(ChangeEvent<string> changeEvent)
        {
            OnFunctionNameChanged?.Invoke(changeEvent.newValue);
        }
    }
}
