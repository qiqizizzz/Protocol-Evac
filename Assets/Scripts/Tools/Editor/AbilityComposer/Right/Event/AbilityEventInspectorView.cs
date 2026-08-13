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
        private const string SELECT_RECEIVER_CHOICE = "选择接收类";
        private const string SELECT_FUNCTION_CHOICE = "选择 Function";

        private static readonly List<string> S_CategoryChoices = new List<string>
        {
            nameof(AbilityEventCategory.Default),
            nameof(AbilityEventCategory.Gameplay),
            nameof(AbilityEventCategory.Vfx),
            nameof(AbilityEventCategory.Audio)
        };

        private readonly VisualElement m_rootVisualElement;
        private Label m_titleLabel;
        private DropdownField m_categoryField;
        private DropdownField m_receiverChoicesField;
        private DropdownField m_functionChoicesField;
        private TextField m_functionField;
        private Button m_saveEventButton;
        private readonly Dictionary<string, List<string>> m_functionGroups = new Dictionary<string, List<string>>();
        private readonly List<string> m_receiverChoices = new List<string>();
        private readonly List<string> m_functionChoices = new List<string>();

        public event Action<AbilityEventCategory> OnCategoryChanged;
        public event Action<string> OnReceiverTypeNameChanged;
        public event Action<string> OnFunctionNameChanged;
        public event Action OnSaveEventRequested;

        // 注入 Event Inspector 页面容器
        public AbilityEventInspectorView(VisualElement rootVisualElement)
        {
            m_rootVisualElement = rootVisualElement;
        }

        // 创建 Event Inspector 控件
        protected override void OnEditorInit()
        {
            m_titleLabel = new Label("事件检查器");
            m_titleLabel.AddToClassList("ac-section-title");
            m_categoryField = new DropdownField("Category", S_CategoryChoices, 0);
            m_categoryField.AddToClassList("ac-inspector-field");
            m_receiverChoices.Add(SELECT_RECEIVER_CHOICE);
            m_receiverChoicesField = new DropdownField("接收类", m_receiverChoices, 0);
            m_receiverChoicesField.AddToClassList("ac-inspector-field");
            m_functionChoices.Add(SELECT_FUNCTION_CHOICE);
            m_functionChoicesField = new DropdownField("Function", m_functionChoices, 0);
            m_functionChoicesField.AddToClassList("ac-inspector-field");
            m_functionField = new TextField("Custom Function");
            m_functionField.isDelayed = true;
            m_functionField.AddToClassList("ac-inspector-field");
            VisualElement saveRow = new VisualElement();
            saveRow.AddToClassList("ac-inspector-save-row");
            m_saveEventButton = new Button();
            m_saveEventButton.AddToClassList("ac-button");
            m_saveEventButton.AddToClassList("ac-inspector-save-button");
            Label saveButtonLabel = new Label("保存事件");
            saveButtonLabel.AddToClassList("ac-button-label");
            saveButtonLabel.AddToClassList("ac-muted-button-label");
            m_saveEventButton.Add(saveButtonLabel);
            saveRow.Add(m_saveEventButton);
            m_rootVisualElement.Add(m_titleLabel);
            m_rootVisualElement.Add(m_categoryField);
            m_rootVisualElement.Add(m_receiverChoicesField);
            m_rootVisualElement.Add(m_functionChoicesField);
            m_rootVisualElement.Add(m_functionField);
            m_rootVisualElement.Add(saveRow);
            SetInspectorVisible(false);
        }

        // 更新预览对象可接收的 Animation Event Function 候选
        public void SetFunctionChoices(IReadOnlyDictionary<string, List<string>> functionGroups)
        {
            string previousReceiver = m_receiverChoicesField == null ? SELECT_RECEIVER_CHOICE : m_receiverChoicesField.value;
            string previousFunction = m_functionChoicesField == null ? SELECT_FUNCTION_CHOICE : m_functionChoicesField.value;
            m_functionGroups.Clear();
            foreach (KeyValuePair<string, List<string>> functionGroup in functionGroups)
                m_functionGroups.Add(functionGroup.Key, new List<string>(functionGroup.Value));

            m_receiverChoices.Clear();
            m_receiverChoices.Add(SELECT_RECEIVER_CHOICE);
            List<string> receiverNames = new List<string>(m_functionGroups.Keys);
            receiverNames.Sort(StringComparer.Ordinal);
            foreach (string receiverName in receiverNames)
                m_receiverChoices.Add(receiverName);

            m_receiverChoicesField.choices = m_receiverChoices;
            string restoredReceiver = m_functionGroups.ContainsKey(previousReceiver)
                ? previousReceiver
                : SELECT_RECEIVER_CHOICE;
            m_receiverChoicesField.SetValueWithoutNotify(restoredReceiver);
            RefreshFunctionChoices(restoredReceiver);
            if (m_functionChoices.Contains(previousFunction))
                m_functionChoicesField.SetValueWithoutNotify(previousFunction);
        }

        // 更新当前接收类对应的 Animation Event Function 候选
        private void RefreshFunctionChoices(string receiverName)
        {
            m_functionChoices.Clear();
            m_functionChoices.Add(SELECT_FUNCTION_CHOICE);
            if (m_functionGroups.TryGetValue(receiverName, out List<string> functionChoices))
                m_functionChoices.AddRange(functionChoices);

            m_functionChoicesField.choices = m_functionChoices;
            m_functionChoicesField.SetValueWithoutNotify(SELECT_FUNCTION_CHOICE);
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
            string selectedReceiver = !string.IsNullOrEmpty(selectedEvent.ReceiverTypeName)
                && m_functionGroups.ContainsKey(selectedEvent.ReceiverTypeName)
                && m_functionGroups[selectedEvent.ReceiverTypeName].Contains(selectedEvent.FunctionName)
                ? selectedEvent.ReceiverTypeName
                : FindReceiverForFunction(selectedEvent.FunctionName);
            m_receiverChoicesField.SetValueWithoutNotify(selectedReceiver);
            RefreshFunctionChoices(selectedReceiver);
            string selectedFunction = m_functionChoices.Contains(selectedEvent.FunctionName)
                ? selectedEvent.FunctionName
                : SELECT_FUNCTION_CHOICE;
            m_functionChoicesField.SetValueWithoutNotify(selectedFunction);
            m_functionField.SetValueWithoutNotify(selectedEvent.FunctionName);
        }

        protected override void SubscribeViewEvents()
        {
            m_categoryField.RegisterValueChangedCallback(HandleCategoryChanged);
            m_receiverChoicesField.RegisterValueChangedCallback(HandleReceiverChoiceChanged);
            m_functionChoicesField.RegisterValueChangedCallback(HandleFunctionChoiceChanged);
            m_functionField.RegisterValueChangedCallback(HandleFunctionNameChanged);
            m_saveEventButton.clicked += RequestSaveEvent;
        }

        protected override void UnsubscribeViewEvents()
        {
            m_categoryField.UnregisterValueChangedCallback(HandleCategoryChanged);
            m_receiverChoicesField.UnregisterValueChangedCallback(HandleReceiverChoiceChanged);
            m_functionChoicesField.UnregisterValueChangedCallback(HandleFunctionChoiceChanged);
            m_functionField.UnregisterValueChangedCallback(HandleFunctionNameChanged);
            m_saveEventButton.clicked -= RequestSaveEvent;
        }

        // 切换事件编辑字段显示
        private void SetInspectorVisible(bool hasSelectedEvent)
        {
            m_titleLabel.style.display = hasSelectedEvent ? DisplayStyle.Flex : DisplayStyle.None;
            m_categoryField.style.display = hasSelectedEvent ? DisplayStyle.Flex : DisplayStyle.None;
            m_receiverChoicesField.style.display = hasSelectedEvent ? DisplayStyle.Flex : DisplayStyle.None;
            m_functionChoicesField.style.display = hasSelectedEvent ? DisplayStyle.Flex : DisplayStyle.None;
            m_functionField.style.display = hasSelectedEvent ? DisplayStyle.Flex : DisplayStyle.None;
            m_saveEventButton.parent.style.display = hasSelectedEvent ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // 根据 Function 名称查找当前事件所属的接收类
        private string FindReceiverForFunction(string functionName)
        {
            foreach (KeyValuePair<string, List<string>> functionGroup in m_functionGroups)
            {
                if (functionGroup.Value.Contains(functionName))
                    return functionGroup.Key;
            }

            return SELECT_RECEIVER_CHOICE;
        }

        // 转换分类下拉框的选择结果
        private void HandleCategoryChanged(ChangeEvent<string> changeEvent)
        {
            if (Enum.TryParse(changeEvent.newValue, out AbilityEventCategory category))
                OnCategoryChanged?.Invoke(category);
        }

        // 切换接收类并刷新对应的 Function 候选
        private void HandleReceiverChoiceChanged(ChangeEvent<string> changeEvent)
        {
            RefreshFunctionChoices(changeEvent.newValue);
            OnReceiverTypeNameChanged?.Invoke(changeEvent.newValue == SELECT_RECEIVER_CHOICE ? string.Empty : changeEvent.newValue);
        }

        // 将 Function 下拉框选择写回当前事件草稿
        private void HandleFunctionChoiceChanged(ChangeEvent<string> changeEvent)
        {
            if (changeEvent.newValue != SELECT_FUNCTION_CHOICE)
                OnFunctionNameChanged?.Invoke(changeEvent.newValue);
        }

        // 转发 Function 文本编辑结果
        private void HandleFunctionNameChanged(ChangeEvent<string> changeEvent)
        {
            OnFunctionNameChanged?.Invoke(changeEvent.newValue);
        }

        // 请求将当前事件草稿写入动画资源
        private void RequestSaveEvent()
        {
            OnSaveEventRequested?.Invoke();
        }
    }
}
