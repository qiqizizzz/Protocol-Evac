/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 窗口检查器页面，编辑选中窗口的类型、帧范围与类型参数
 * │  类    名: AbilityWindowInspectorView.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Framework.QTower.Editor.View;
using Module.Player.Window;
using Tools.Editor.AbilityComposer.Center.Timeline;
using UnityEngine.UIElements;

namespace Tools.Editor.AbilityComposer.Right.Window
{
    public sealed class AbilityWindowInspectorView : UIBaseEditor
    {
        private static readonly List<string> S_TypeChoices = new List<string>
        {
            "命中窗口",
            "无敌帧窗口"
        };

        private readonly VisualElement m_rootVisualElement;
        private Label m_titleLabel;
        private DropdownField m_typeField;
        private IntegerField m_startFrameField;
        private IntegerField m_endFrameField;
        private FloatField m_damageField;

        public event Action<AbilityWindowType> OnTypeChanged;
        public event Action<int, int> OnFramesChanged;
        public event Action<float> OnDamageChanged;

        // 注入窗口检查器页面容器
        public AbilityWindowInspectorView(VisualElement rootVisualElement)
        {
            m_rootVisualElement = rootVisualElement;
        }

        // 创建窗口检查器控件
        protected override void OnEditorInit()
        {
            m_titleLabel = new Label("窗口检查器");
            m_titleLabel.AddToClassList("ac-section-title");
            m_typeField = new DropdownField("类型", S_TypeChoices, 0);
            m_typeField.AddToClassList("ac-inspector-field");
            m_startFrameField = new IntegerField("开始帧");
            m_startFrameField.isDelayed = true;
            m_startFrameField.AddToClassList("ac-inspector-field");
            m_endFrameField = new IntegerField("结束帧");
            m_endFrameField.isDelayed = true;
            m_endFrameField.AddToClassList("ac-inspector-field");
            m_damageField = new FloatField("伤害");
            m_damageField.isDelayed = true;
            m_damageField.AddToClassList("ac-inspector-field");
            m_rootVisualElement.Add(m_titleLabel);
            m_rootVisualElement.Add(m_typeField);
            m_rootVisualElement.Add(m_startFrameField);
            m_rootVisualElement.Add(m_endFrameField);
            m_rootVisualElement.Add(m_damageField);
            SetInspectorVisible(false);
        }

        // 刷新选中窗口的检查器字段
        public void Refresh(AbilityTimelineData timelineData)
        {
            AbilityWindowDraft selectedWindow = timelineData.SelectedWindow;
            bool isVisible = selectedWindow != null;
            SetInspectorVisible(isVisible);
            if (!isVisible)
                return;

            m_typeField.SetValueWithoutNotify(GetTypeChoice(selectedWindow.Type));
            m_startFrameField.SetValueWithoutNotify(selectedWindow.StartFrame);
            m_endFrameField.SetValueWithoutNotify(selectedWindow.EndFrame);
            m_damageField.SetValueWithoutNotify(selectedWindow.Damage);
            m_damageField.style.display = selectedWindow.Type == AbilityWindowType.Hit ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected override void SubscribeViewEvents()
        {
            m_typeField.RegisterValueChangedCallback(HandleTypeChanged);
            m_startFrameField.RegisterValueChangedCallback(HandleFramesChanged);
            m_endFrameField.RegisterValueChangedCallback(HandleFramesChanged);
            m_damageField.RegisterValueChangedCallback(HandleDamageChanged);
        }

        protected override void UnsubscribeViewEvents()
        {
            m_typeField.UnregisterValueChangedCallback(HandleTypeChanged);
            m_startFrameField.UnregisterValueChangedCallback(HandleFramesChanged);
            m_endFrameField.UnregisterValueChangedCallback(HandleFramesChanged);
            m_damageField.UnregisterValueChangedCallback(HandleDamageChanged);
        }

        // 切换窗口检查器显示
        private void SetInspectorVisible(bool isVisible)
        {
            DisplayStyle displayStyle = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            m_titleLabel.style.display = displayStyle;
            m_typeField.style.display = displayStyle;
            m_startFrameField.style.display = displayStyle;
            m_endFrameField.style.display = displayStyle;
            m_damageField.style.display = displayStyle;
        }

        // 转换窗口类型下拉框的选择结果
        private void HandleTypeChanged(ChangeEvent<string> changeEvent)
        {
            OnTypeChanged?.Invoke(changeEvent.newValue == "无敌帧窗口" ? AbilityWindowType.Invincible : AbilityWindowType.Hit);
        }

        // 提交窗口帧范围编辑
        private void HandleFramesChanged(ChangeEvent<int> changeEvent)
        {
            OnFramesChanged?.Invoke(m_startFrameField.value, m_endFrameField.value);
        }

        // 提交命中窗口伤害编辑
        private void HandleDamageChanged(ChangeEvent<float> changeEvent)
        {
            OnDamageChanged?.Invoke(changeEvent.newValue);
        }

        // 将枚举类型转换为下拉选项文字
        private string GetTypeChoice(AbilityWindowType type)
        {
            return type == AbilityWindowType.Invincible ? "无敌帧窗口" : "命中窗口";
        }
    }
}
