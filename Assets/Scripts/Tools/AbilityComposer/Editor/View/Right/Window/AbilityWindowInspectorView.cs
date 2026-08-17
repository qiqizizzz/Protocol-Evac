/*
 * ┌─────────────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 窗口检查器页面，编辑选中窗口的类型、帧范围与类型参数
 * │  类    名: AbilityWindowInspectorView.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Framework.QTower.Editor.View;
using Module.Ability.Data.Window;
using Module.Ability.Data.Window.Vfx;
using Tools.AbilityComposer.Editor.View.Center.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tools.AbilityComposer.Editor.View.Right.Window
{
    public sealed class AbilityWindowInspectorView : UIBaseEditor
    {
        private static readonly List<string> S_TypeChoices = new List<string>
        {
            "命中窗口",
            "技能推进窗口",
            "移动锁定窗口",
            "特效窗口"
        };

        private static readonly List<string> S_VfxTriggerTypeChoices = new List<string>
        {
            nameof(AbilityVfxTriggerType.WindowEnter),
            nameof(AbilityVfxTriggerType.WindowDuration),
            nameof(AbilityVfxTriggerType.OnHit)
        };

        private static readonly List<string> S_VfxTargetTypeChoices = new List<string>
        {
            nameof(AbilityVfxTargetType.SourceSocket),
            nameof(AbilityVfxTargetType.SourceRoot),
            nameof(AbilityVfxTargetType.HitPoint),
            nameof(AbilityVfxTargetType.HitTargetRoot),
            nameof(AbilityVfxTargetType.HitTargetSocket)
        };

        private static readonly List<string> S_VfxLifeModeChoices = new List<string>
        {
            nameof(AbilityVfxLifeMode.AutoDestroy),
            nameof(AbilityVfxLifeMode.StopOnWindowEnd),
            nameof(AbilityVfxLifeMode.DestroyOnWindowEnd)
        };

        private readonly VisualElement m_rootVisualElement;
        private Label m_titleLabel;
        private ObjectField m_windowConfigField;
        private DropdownField m_typeField;
        private IntegerField m_startFrameField;
        private IntegerField m_endFrameField;
        private FloatField m_damageField;
        private DropdownField m_vfxTriggerTypeField;
        private DropdownField m_vfxTargetTypeField;
        private ObjectField m_vfxPrefabField;
        private TextField m_vfxSocketIdField;
        private DropdownField m_vfxLifeModeField;
        private Vector3Field m_vfxPositionOffsetField;
        private Vector3Field m_vfxEulerOffsetField;
        private Toggle m_vfxFollowTargetToggle;
        private Button m_saveWindowButton;

        public event Action<AbilityWindowConfigSO> OnWindowConfigChanged;
        public event Action<AbilityWindowDraftType> OnTypeChanged;
        public event Action<int, int> OnFramesChanged;
        public event Action<float> OnDamageChanged;
        public event Action<AbilityVfxTriggerType> OnVfxTriggerTypeChanged;
        public event Action<AbilityVfxTargetType> OnVfxTargetTypeChanged;
        public event Action<GameObject> OnVfxPrefabChanged;
        public event Action<string> OnVfxSocketIdChanged;
        public event Action<AbilityVfxLifeMode> OnVfxLifeModeChanged;
        public event Action<Vector3> OnVfxPositionOffsetChanged;
        public event Action<Vector3> OnVfxEulerOffsetChanged;
        public event Action<bool> OnVfxFollowTargetChanged;
        public event Action OnSaveWindowRequested;

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
            m_windowConfigField = new ObjectField("窗口主体配置");
            m_windowConfigField.objectType = typeof(AbilityWindowConfigSO);
            m_windowConfigField.allowSceneObjects = false;
            m_windowConfigField.AddToClassList("ac-inspector-field");
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
            m_vfxTriggerTypeField = new DropdownField("触发方式", S_VfxTriggerTypeChoices, 1);
            m_vfxTriggerTypeField.AddToClassList("ac-inspector-field");
            m_vfxTargetTypeField = new DropdownField("生成目标", S_VfxTargetTypeChoices, 0);
            m_vfxTargetTypeField.AddToClassList("ac-inspector-field");
            m_vfxPrefabField = new ObjectField("特效预制体");
            m_vfxPrefabField.objectType = typeof(GameObject);
            m_vfxPrefabField.allowSceneObjects = false;
            m_vfxPrefabField.AddToClassList("ac-inspector-field");
            m_vfxSocketIdField = new TextField("挂点 Id");
            m_vfxSocketIdField.isDelayed = true;
            m_vfxSocketIdField.AddToClassList("ac-inspector-field");
            m_vfxLifeModeField = new DropdownField("生命周期", S_VfxLifeModeChoices, 2);
            m_vfxLifeModeField.AddToClassList("ac-inspector-field");
            m_vfxPositionOffsetField = new Vector3Field("位置偏移");
            m_vfxPositionOffsetField.AddToClassList("ac-inspector-field");
            m_vfxEulerOffsetField = new Vector3Field("旋转偏移");
            m_vfxEulerOffsetField.AddToClassList("ac-inspector-field");
            m_vfxFollowTargetToggle = new Toggle("跟随目标");
            m_vfxFollowTargetToggle.AddToClassList("ac-inspector-field");
            m_saveWindowButton = new Button();
            m_saveWindowButton.AddToClassList("ac-button");
            m_saveWindowButton.AddToClassList("ac-inspector-save-button");
            Label saveButtonLabel = new Label("保存窗口");
            saveButtonLabel.name = "save-window-button-label";
            saveButtonLabel.AddToClassList("ac-button-label");
            saveButtonLabel.AddToClassList("ac-muted-button-label");
            m_saveWindowButton.Add(saveButtonLabel);
            m_rootVisualElement.Add(m_titleLabel);
            m_rootVisualElement.Add(m_windowConfigField);
            m_rootVisualElement.Add(m_typeField);
            m_rootVisualElement.Add(m_startFrameField);
            m_rootVisualElement.Add(m_endFrameField);
            m_rootVisualElement.Add(m_damageField);
            m_rootVisualElement.Add(m_vfxTriggerTypeField);
            m_rootVisualElement.Add(m_vfxTargetTypeField);
            m_rootVisualElement.Add(m_vfxPrefabField);
            m_rootVisualElement.Add(m_vfxSocketIdField);
            m_rootVisualElement.Add(m_vfxLifeModeField);
            m_rootVisualElement.Add(m_vfxPositionOffsetField);
            m_rootVisualElement.Add(m_vfxEulerOffsetField);
            m_rootVisualElement.Add(m_vfxFollowTargetToggle);
            VisualElement saveRow = new VisualElement();
            saveRow.AddToClassList("ac-inspector-save-row");
            saveRow.Add(m_saveWindowButton);
            m_rootVisualElement.Add(saveRow);
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

            m_windowConfigField.SetValueWithoutNotify(timelineData.WindowConfig);

            SetWindowFieldsEnabled(true);
            m_saveWindowButton.SetEnabled(true);

            m_typeField.SetValueWithoutNotify(GetTypeChoice(selectedWindow.Type));
            m_startFrameField.SetValueWithoutNotify(selectedWindow.StartFrame);
            m_endFrameField.SetValueWithoutNotify(selectedWindow.EndFrame);
            m_damageField.SetValueWithoutNotify(selectedWindow.Damage);
            m_vfxTriggerTypeField.SetValueWithoutNotify(selectedWindow.VfxTriggerType.ToString());
            m_vfxTargetTypeField.SetValueWithoutNotify(selectedWindow.VfxTargetType.ToString());
            m_vfxPrefabField.SetValueWithoutNotify(selectedWindow.VfxPrefab);
            m_vfxSocketIdField.SetValueWithoutNotify(selectedWindow.VfxSocketId);
            m_vfxLifeModeField.SetValueWithoutNotify(selectedWindow.VfxLifeMode.ToString());
            m_vfxPositionOffsetField.SetValueWithoutNotify(selectedWindow.VfxLocalPositionOffset);
            m_vfxEulerOffsetField.SetValueWithoutNotify(selectedWindow.VfxLocalEulerOffset);
            m_vfxFollowTargetToggle.SetValueWithoutNotify(selectedWindow.VfxFollowTarget);
            RefreshTypeSpecificFields(selectedWindow.Type);
        }

        protected override void SubscribeViewEvents()
        {
            m_windowConfigField.RegisterValueChangedCallback(HandleWindowConfigChanged);
            m_typeField.RegisterValueChangedCallback(HandleTypeChanged);
            m_startFrameField.RegisterValueChangedCallback(HandleFramesChanged);
            m_endFrameField.RegisterValueChangedCallback(HandleFramesChanged);
            m_damageField.RegisterValueChangedCallback(HandleDamageChanged);
            m_vfxTriggerTypeField.RegisterValueChangedCallback(HandleVfxTriggerTypeChanged);
            m_vfxTargetTypeField.RegisterValueChangedCallback(HandleVfxTargetTypeChanged);
            m_vfxPrefabField.RegisterValueChangedCallback(HandleVfxPrefabChanged);
            m_vfxSocketIdField.RegisterValueChangedCallback(HandleVfxSocketIdChanged);
            m_vfxLifeModeField.RegisterValueChangedCallback(HandleVfxLifeModeChanged);
            m_vfxPositionOffsetField.RegisterValueChangedCallback(HandleVfxPositionOffsetChanged);
            m_vfxEulerOffsetField.RegisterValueChangedCallback(HandleVfxEulerOffsetChanged);
            m_vfxFollowTargetToggle.RegisterValueChangedCallback(HandleVfxFollowTargetChanged);
            m_saveWindowButton.clicked += RequestSaveWindow;
        }

        protected override void UnsubscribeViewEvents()
        {
            m_windowConfigField.UnregisterValueChangedCallback(HandleWindowConfigChanged);
            m_typeField.UnregisterValueChangedCallback(HandleTypeChanged);
            m_startFrameField.UnregisterValueChangedCallback(HandleFramesChanged);
            m_endFrameField.UnregisterValueChangedCallback(HandleFramesChanged);
            m_damageField.UnregisterValueChangedCallback(HandleDamageChanged);
            m_vfxTriggerTypeField.UnregisterValueChangedCallback(HandleVfxTriggerTypeChanged);
            m_vfxTargetTypeField.UnregisterValueChangedCallback(HandleVfxTargetTypeChanged);
            m_vfxPrefabField.UnregisterValueChangedCallback(HandleVfxPrefabChanged);
            m_vfxSocketIdField.UnregisterValueChangedCallback(HandleVfxSocketIdChanged);
            m_vfxLifeModeField.UnregisterValueChangedCallback(HandleVfxLifeModeChanged);
            m_vfxPositionOffsetField.UnregisterValueChangedCallback(HandleVfxPositionOffsetChanged);
            m_vfxEulerOffsetField.UnregisterValueChangedCallback(HandleVfxEulerOffsetChanged);
            m_vfxFollowTargetToggle.UnregisterValueChangedCallback(HandleVfxFollowTargetChanged);
            m_saveWindowButton.clicked -= RequestSaveWindow;
        }

        // 切换窗口检查器显示
        private void SetInspectorVisible(bool isVisible)
        {
            DisplayStyle displayStyle = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            m_titleLabel.style.display = displayStyle;
            m_windowConfigField.style.display = displayStyle;
            m_typeField.style.display = displayStyle;
            m_startFrameField.style.display = displayStyle;
            m_endFrameField.style.display = displayStyle;
            m_damageField.style.display = displayStyle;
            m_vfxTriggerTypeField.style.display = displayStyle;
            m_vfxTargetTypeField.style.display = displayStyle;
            m_vfxPrefabField.style.display = displayStyle;
            m_vfxSocketIdField.style.display = displayStyle;
            m_vfxLifeModeField.style.display = displayStyle;
            m_vfxPositionOffsetField.style.display = displayStyle;
            m_vfxEulerOffsetField.style.display = displayStyle;
            m_vfxFollowTargetToggle.style.display = displayStyle;
            m_saveWindowButton.parent.style.display = displayStyle;
        }

        // 切换窗口字段的可编辑状态
        private void SetWindowFieldsEnabled(bool isEnabled)
        {
            m_typeField.SetEnabled(isEnabled);
            m_startFrameField.SetEnabled(isEnabled);
            m_endFrameField.SetEnabled(isEnabled);
            m_damageField.SetEnabled(isEnabled);
            m_vfxTriggerTypeField.SetEnabled(isEnabled);
            m_vfxTargetTypeField.SetEnabled(isEnabled);
            m_vfxPrefabField.SetEnabled(isEnabled);
            m_vfxSocketIdField.SetEnabled(isEnabled);
            m_vfxLifeModeField.SetEnabled(isEnabled);
            m_vfxPositionOffsetField.SetEnabled(isEnabled);
            m_vfxEulerOffsetField.SetEnabled(isEnabled);
            m_vfxFollowTargetToggle.SetEnabled(isEnabled);
        }

        // 切换当前编辑的窗口主体配置
        private void HandleWindowConfigChanged(ChangeEvent<UnityEngine.Object> changeEvent)
        {
            OnWindowConfigChanged?.Invoke(changeEvent.newValue as AbilityWindowConfigSO);
        }

        // 转换窗口类型下拉框的选择结果
        private void HandleTypeChanged(ChangeEvent<string> changeEvent)
        {
            AbilityWindowDraftType type;
            switch (changeEvent.newValue)
            {
                case "技能推进窗口":
                    type = AbilityWindowDraftType.StepAdvance;
                    break;
                case "移动锁定窗口":
                    type = AbilityWindowDraftType.MovementLock;
                    break;
                case "特效窗口":
                    type = AbilityWindowDraftType.Vfx;
                    break;
                default:
                    type = AbilityWindowDraftType.Hit;
                    break;
            }

            OnTypeChanged?.Invoke(type);
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

        // 提交特效窗口触发方式编辑
        private void HandleVfxTriggerTypeChanged(ChangeEvent<string> changeEvent)
        {
            if (Enum.TryParse(changeEvent.newValue, out AbilityVfxTriggerType triggerType))
                OnVfxTriggerTypeChanged?.Invoke(triggerType);
        }

        // 提交特效窗口生成目标编辑
        private void HandleVfxTargetTypeChanged(ChangeEvent<string> changeEvent)
        {
            if (Enum.TryParse(changeEvent.newValue, out AbilityVfxTargetType targetType))
                OnVfxTargetTypeChanged?.Invoke(targetType);
        }

        // 提交特效窗口预制体编辑
        private void HandleVfxPrefabChanged(ChangeEvent<UnityEngine.Object> changeEvent)
        {
            OnVfxPrefabChanged?.Invoke(changeEvent.newValue as GameObject);
        }

        // 提交特效窗口挂点 Id 编辑
        private void HandleVfxSocketIdChanged(ChangeEvent<string> changeEvent)
        {
            OnVfxSocketIdChanged?.Invoke(changeEvent.newValue);
        }

        // 提交特效窗口生命周期编辑
        private void HandleVfxLifeModeChanged(ChangeEvent<string> changeEvent)
        {
            if (Enum.TryParse(changeEvent.newValue, out AbilityVfxLifeMode lifeMode))
                OnVfxLifeModeChanged?.Invoke(lifeMode);
        }

        // 提交特效窗口位置偏移编辑
        private void HandleVfxPositionOffsetChanged(ChangeEvent<Vector3> changeEvent)
        {
            OnVfxPositionOffsetChanged?.Invoke(changeEvent.newValue);
        }

        // 提交特效窗口旋转偏移编辑
        private void HandleVfxEulerOffsetChanged(ChangeEvent<Vector3> changeEvent)
        {
            OnVfxEulerOffsetChanged?.Invoke(changeEvent.newValue);
        }

        // 提交特效窗口跟随目标编辑
        private void HandleVfxFollowTargetChanged(ChangeEvent<bool> changeEvent)
        {
            OnVfxFollowTargetChanged?.Invoke(changeEvent.newValue);
        }

        // 请求将当前窗口草稿写入窗口轨道资产
        private void RequestSaveWindow()
        {
            OnSaveWindowRequested?.Invoke();
        }

        // 将枚举类型转换为下拉选项文字
        private string GetTypeChoice(AbilityWindowDraftType type)
        {
            switch (type)
            {
                case AbilityWindowDraftType.StepAdvance:
                    return "技能推进窗口";
                case AbilityWindowDraftType.MovementLock:
                    return "移动锁定窗口";
                case AbilityWindowDraftType.Vfx:
                    return "特效窗口";
                default:
                    return "命中窗口";
            }
        }

        // 根据窗口类型显示对应参数
        private void RefreshTypeSpecificFields(AbilityWindowDraftType type)
        {
            bool isHit = type == AbilityWindowDraftType.Hit;
            bool isVfx = type == AbilityWindowDraftType.Vfx;
            m_damageField.style.display = isHit ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxTriggerTypeField.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxTargetTypeField.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxPrefabField.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxSocketIdField.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxLifeModeField.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxPositionOffsetField.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxEulerOffsetField.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxFollowTargetToggle.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
        }

    }
}
