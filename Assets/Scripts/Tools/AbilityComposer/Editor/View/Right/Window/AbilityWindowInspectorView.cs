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
using Module.Ability.Data.Window.Audio;
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
            "特效窗口",
            "音效窗口"
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

        private static readonly List<string> S_AudioTriggerTypeChoices = new List<string>
        {
            nameof(AbilityAudioTriggerType.WindowEnter),
            nameof(AbilityAudioTriggerType.WindowDuration),
            nameof(AbilityAudioTriggerType.OnHit)
        };

        private static readonly List<string> S_AudioPlaybackTypeChoices = new List<string>
        {
            nameof(AbilityAudioPlaybackType.OneShot),
            nameof(AbilityAudioPlaybackType.RandomOneShot),
            nameof(AbilityAudioPlaybackType.SequenceOneShot),
            nameof(AbilityAudioPlaybackType.Loop)
        };

        private static readonly List<string> S_AudioTargetTypeChoices = new List<string>
        {
            nameof(AbilityAudioTargetType.SourceRoot),
            nameof(AbilityAudioTargetType.SourceSocket),
            nameof(AbilityAudioTargetType.HitPoint),
            nameof(AbilityAudioTargetType.HitTargetRoot),
            nameof(AbilityAudioTargetType.HitTargetSocket)
        };

        private const string NO_VFX_SOCKET_SELECTED_CHOICE = "未选择挂点";
        private const string NO_VFX_SOCKET_FOUND_CHOICE = "未找到挂点";
        private const string CUSTOM_VFX_SOCKET_CHOICE = "手动输入";

        private readonly VisualElement m_rootVisualElement;
        private readonly List<string> m_vfxSocketIdChoices = new List<string>();
        private readonly List<string> m_vfxSocketChoiceValues = new List<string>();
        private Label m_titleLabel;
        private ObjectField m_windowConfigField;
        private DropdownField m_typeField;
        private IntegerField m_startFrameField;
        private IntegerField m_endFrameField;
        private FloatField m_damageField;
        private DropdownField m_vfxTriggerTypeField;
        private DropdownField m_vfxTargetTypeField;
        private ObjectField m_vfxPrefabField;
        private DropdownField m_vfxSocketChoiceField;
        private TextField m_vfxSocketIdField;
        private DropdownField m_vfxLifeModeField;
        private Vector3Field m_vfxPositionOffsetField;
        private Vector3Field m_vfxEulerOffsetField;
        private Toggle m_vfxFollowTargetToggle;
        private DropdownField m_audioTriggerTypeField;
        private DropdownField m_audioPlaybackTypeField;
        private Foldout m_audioClipFoldout;
        private VisualElement m_audioClipListRoot;
        private Button m_audioClipAddButton;
        private Slider m_audioVolumeField;
        private FloatField m_audioPitchField;
        private Slider m_audioRandomPitchRangeField;
        private Toggle m_audioSpatialToggle;
        private Toggle m_audioStopOnWindowEndToggle;
        private DropdownField m_audioTargetTypeField;
        private DropdownField m_audioSocketChoiceField;
        private TextField m_audioSocketIdField;
        private Vector3Field m_audioPositionOffsetField;
        private Button m_saveWindowButton;
        private Button m_deleteWindowButton;

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
        public event Action<AbilityAudioTriggerType> OnAudioTriggerTypeChanged;
        public event Action<AbilityAudioPlaybackType> OnAudioPlaybackTypeChanged;
        public event Action<int, AudioClip> OnAudioClipChanged;
        public event Action OnAudioClipAddRequested;
        public event Action<int> OnAudioClipRemoveRequested;
        public event Action<float> OnAudioVolumeChanged;
        public event Action<float> OnAudioPitchChanged;
        public event Action<float> OnAudioRandomPitchRangeChanged;
        public event Action<bool> OnAudioSpatialChanged;
        public event Action<bool> OnAudioStopOnWindowEndChanged;
        public event Action<AbilityAudioTargetType> OnAudioTargetTypeChanged;
        public event Action<string> OnAudioSocketIdChanged;
        public event Action<Vector3> OnAudioPositionOffsetChanged;
        public event Action OnSaveWindowRequested;
        public event Action OnDeleteWindowRequested;

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
            m_vfxSocketChoiceField = new DropdownField("挂点选择");
            m_vfxSocketChoiceField.AddToClassList("ac-inspector-field");
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
            m_audioTriggerTypeField = new DropdownField("音效触发", S_AudioTriggerTypeChoices, 0);
            m_audioTriggerTypeField.AddToClassList("ac-inspector-field");
            m_audioPlaybackTypeField = new DropdownField("播放类型", S_AudioPlaybackTypeChoices, 0);
            m_audioPlaybackTypeField.AddToClassList("ac-inspector-field");
            m_audioClipFoldout = new Foldout();
            m_audioClipFoldout.text = "音效数组";
            m_audioClipFoldout.value = false;
            m_audioClipFoldout.AddToClassList("ac-inspector-field");
            m_audioClipFoldout.style.flexDirection = FlexDirection.Column;
            m_audioClipFoldout.style.width = Length.Percent(100);
            m_audioClipListRoot = new VisualElement();
            m_audioClipListRoot.style.flexDirection = FlexDirection.Column;
            m_audioClipListRoot.style.width = Length.Percent(100);
            m_audioClipAddButton = new Button();
            m_audioClipAddButton.text = "添加音效";
            m_audioClipAddButton.AddToClassList("ac-button");
            m_audioClipAddButton.style.marginTop = 4f;
            m_audioClipAddButton.style.width = Length.Percent(100);
            m_audioClipFoldout.Add(m_audioClipListRoot);
            m_audioClipFoldout.Add(m_audioClipAddButton);
            m_audioVolumeField = new Slider("音量", 0f, 1f);
            m_audioVolumeField.AddToClassList("ac-inspector-field");
            m_audioPitchField = new FloatField("音高");
            m_audioPitchField.isDelayed = true;
            m_audioPitchField.AddToClassList("ac-inspector-field");
            m_audioRandomPitchRangeField = new Slider("随机音高", 0f, 1f);
            m_audioRandomPitchRangeField.AddToClassList("ac-inspector-field");
            m_audioSpatialToggle = new Toggle("3D 音效");
            m_audioSpatialToggle.AddToClassList("ac-inspector-field");
            m_audioStopOnWindowEndToggle = new Toggle("窗口结束时停止");
            m_audioStopOnWindowEndToggle.AddToClassList("ac-inspector-field");
            m_audioTargetTypeField = new DropdownField("播放目标", S_AudioTargetTypeChoices, 0);
            m_audioTargetTypeField.AddToClassList("ac-inspector-field");
            m_audioSocketChoiceField = new DropdownField("音效挂点选择");
            m_audioSocketChoiceField.AddToClassList("ac-inspector-field");
            m_audioSocketIdField = new TextField("音效挂点 Id");
            m_audioSocketIdField.isDelayed = true;
            m_audioSocketIdField.AddToClassList("ac-inspector-field");
            m_audioPositionOffsetField = new Vector3Field("音效位置偏移");
            m_audioPositionOffsetField.AddToClassList("ac-inspector-field");
            m_saveWindowButton = new Button();
            m_saveWindowButton.AddToClassList("ac-button");
            m_saveWindowButton.AddToClassList("ac-inspector-save-button");
            Label saveButtonLabel = new Label("保存窗口");
            saveButtonLabel.name = "save-window-button-label";
            saveButtonLabel.AddToClassList("ac-button-label");
            saveButtonLabel.AddToClassList("ac-muted-button-label");
            m_saveWindowButton.Add(saveButtonLabel);
            m_deleteWindowButton = new Button();
            m_deleteWindowButton.AddToClassList("ac-button");
            m_deleteWindowButton.AddToClassList("ac-inspector-save-button");
            Label deleteButtonLabel = new Label("删除窗口");
            deleteButtonLabel.name = "delete-window-button-label";
            deleteButtonLabel.AddToClassList("ac-button-label");
            deleteButtonLabel.AddToClassList("ac-muted-button-label");
            m_deleteWindowButton.Add(deleteButtonLabel);
            m_rootVisualElement.Add(m_titleLabel);
            m_rootVisualElement.Add(m_windowConfigField);
            m_rootVisualElement.Add(m_typeField);
            m_rootVisualElement.Add(m_startFrameField);
            m_rootVisualElement.Add(m_endFrameField);
            m_rootVisualElement.Add(m_damageField);
            m_rootVisualElement.Add(m_vfxTriggerTypeField);
            m_rootVisualElement.Add(m_vfxTargetTypeField);
            m_rootVisualElement.Add(m_vfxPrefabField);
            m_rootVisualElement.Add(m_vfxSocketChoiceField);
            m_rootVisualElement.Add(m_vfxSocketIdField);
            m_rootVisualElement.Add(m_vfxLifeModeField);
            m_rootVisualElement.Add(m_vfxPositionOffsetField);
            m_rootVisualElement.Add(m_vfxEulerOffsetField);
            m_rootVisualElement.Add(m_vfxFollowTargetToggle);
            m_rootVisualElement.Add(m_audioTriggerTypeField);
            m_rootVisualElement.Add(m_audioPlaybackTypeField);
            m_rootVisualElement.Add(m_audioClipFoldout);
            m_rootVisualElement.Add(m_audioVolumeField);
            m_rootVisualElement.Add(m_audioPitchField);
            m_rootVisualElement.Add(m_audioRandomPitchRangeField);
            m_rootVisualElement.Add(m_audioSpatialToggle);
            m_rootVisualElement.Add(m_audioStopOnWindowEndToggle);
            m_rootVisualElement.Add(m_audioTargetTypeField);
            m_rootVisualElement.Add(m_audioSocketChoiceField);
            m_rootVisualElement.Add(m_audioSocketIdField);
            m_rootVisualElement.Add(m_audioPositionOffsetField);
            VisualElement saveRow = new VisualElement();
            saveRow.AddToClassList("ac-inspector-save-row");
            saveRow.Add(m_deleteWindowButton);
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
            m_deleteWindowButton.SetEnabled(true);

            m_typeField.SetValueWithoutNotify(GetTypeChoice(selectedWindow.Type));
            m_startFrameField.SetValueWithoutNotify(selectedWindow.StartFrame);
            m_endFrameField.SetValueWithoutNotify(selectedWindow.EndFrame);
            m_damageField.SetValueWithoutNotify(selectedWindow.Damage);
            m_vfxTriggerTypeField.SetValueWithoutNotify(selectedWindow.VfxTriggerType.ToString());
            m_vfxTargetTypeField.SetValueWithoutNotify(selectedWindow.VfxTargetType.ToString());
            m_vfxPrefabField.SetValueWithoutNotify(selectedWindow.VfxPrefab);
            m_vfxSocketIdField.SetValueWithoutNotify(selectedWindow.VfxSocketId);
            RefreshVfxSocketChoiceField(selectedWindow.VfxSocketId);
            m_vfxLifeModeField.SetValueWithoutNotify(selectedWindow.VfxLifeMode.ToString());
            m_vfxPositionOffsetField.SetValueWithoutNotify(selectedWindow.VfxLocalPositionOffset);
            m_vfxEulerOffsetField.SetValueWithoutNotify(selectedWindow.VfxLocalEulerOffset);
            m_vfxFollowTargetToggle.SetValueWithoutNotify(selectedWindow.VfxFollowTarget);
            m_audioTriggerTypeField.SetValueWithoutNotify(selectedWindow.AudioTriggerType.ToString());
            m_audioPlaybackTypeField.SetValueWithoutNotify(selectedWindow.AudioPlaybackType.ToString());
            RefreshAudioClipList(selectedWindow);
            m_audioVolumeField.SetValueWithoutNotify(selectedWindow.AudioVolume);
            m_audioPitchField.SetValueWithoutNotify(selectedWindow.AudioPitch);
            m_audioRandomPitchRangeField.SetValueWithoutNotify(selectedWindow.AudioRandomPitchRange);
            m_audioSpatialToggle.SetValueWithoutNotify(selectedWindow.AudioSpatial);
            m_audioStopOnWindowEndToggle.SetValueWithoutNotify(selectedWindow.AudioStopOnWindowEnd);
            m_audioTargetTypeField.SetValueWithoutNotify(selectedWindow.AudioTargetType.ToString());
            m_audioSocketIdField.SetValueWithoutNotify(selectedWindow.AudioSocketId);
            RefreshAudioSocketChoiceField(selectedWindow.AudioSocketId);
            m_audioPositionOffsetField.SetValueWithoutNotify(selectedWindow.AudioLocalPositionOffset);
            RefreshTypeSpecificFields(selectedWindow.Type, selectedWindow.VfxTargetType, selectedWindow.AudioTargetType);
        }

        // 更新当前预览对象可选的特效挂点 Id
        public void SetVfxSocketIdChoices(IReadOnlyList<string> socketIdChoices)
        {
            m_vfxSocketIdChoices.Clear();
            for (int socketIndex = 0; socketIndex < socketIdChoices.Count; socketIndex++)
            {
                string socketId = socketIdChoices[socketIndex];
                if (string.IsNullOrEmpty(socketId) || m_vfxSocketIdChoices.Contains(socketId))
                    continue;

                m_vfxSocketIdChoices.Add(socketId);
            }

            RefreshVfxSocketChoiceField(m_vfxSocketIdField.value);
            RefreshAudioSocketChoiceField(m_audioSocketIdField.value);
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
            m_vfxSocketChoiceField.RegisterValueChangedCallback(HandleVfxSocketChoiceChanged);
            m_vfxSocketIdField.RegisterValueChangedCallback(HandleVfxSocketIdChanged);
            m_vfxLifeModeField.RegisterValueChangedCallback(HandleVfxLifeModeChanged);
            m_vfxPositionOffsetField.RegisterValueChangedCallback(HandleVfxPositionOffsetChanged);
            m_vfxEulerOffsetField.RegisterValueChangedCallback(HandleVfxEulerOffsetChanged);
            m_vfxFollowTargetToggle.RegisterValueChangedCallback(HandleVfxFollowTargetChanged);
            m_audioTriggerTypeField.RegisterValueChangedCallback(HandleAudioTriggerTypeChanged);
            m_audioPlaybackTypeField.RegisterValueChangedCallback(HandleAudioPlaybackTypeChanged);
            m_audioClipAddButton.clicked += RequestAudioClipAdd;
            m_audioVolumeField.RegisterValueChangedCallback(HandleAudioVolumeChanged);
            m_audioPitchField.RegisterValueChangedCallback(HandleAudioPitchChanged);
            m_audioRandomPitchRangeField.RegisterValueChangedCallback(HandleAudioRandomPitchRangeChanged);
            m_audioSpatialToggle.RegisterValueChangedCallback(HandleAudioSpatialChanged);
            m_audioStopOnWindowEndToggle.RegisterValueChangedCallback(HandleAudioStopOnWindowEndChanged);
            m_audioTargetTypeField.RegisterValueChangedCallback(HandleAudioTargetTypeChanged);
            m_audioSocketChoiceField.RegisterValueChangedCallback(HandleAudioSocketChoiceChanged);
            m_audioSocketIdField.RegisterValueChangedCallback(HandleAudioSocketIdChanged);
            m_audioPositionOffsetField.RegisterValueChangedCallback(HandleAudioPositionOffsetChanged);
            m_saveWindowButton.clicked += RequestSaveWindow;
            m_deleteWindowButton.clicked += RequestDeleteWindow;
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
            m_vfxSocketChoiceField.UnregisterValueChangedCallback(HandleVfxSocketChoiceChanged);
            m_vfxSocketIdField.UnregisterValueChangedCallback(HandleVfxSocketIdChanged);
            m_vfxLifeModeField.UnregisterValueChangedCallback(HandleVfxLifeModeChanged);
            m_vfxPositionOffsetField.UnregisterValueChangedCallback(HandleVfxPositionOffsetChanged);
            m_vfxEulerOffsetField.UnregisterValueChangedCallback(HandleVfxEulerOffsetChanged);
            m_vfxFollowTargetToggle.UnregisterValueChangedCallback(HandleVfxFollowTargetChanged);
            m_audioTriggerTypeField.UnregisterValueChangedCallback(HandleAudioTriggerTypeChanged);
            m_audioPlaybackTypeField.UnregisterValueChangedCallback(HandleAudioPlaybackTypeChanged);
            m_audioClipAddButton.clicked -= RequestAudioClipAdd;
            m_audioVolumeField.UnregisterValueChangedCallback(HandleAudioVolumeChanged);
            m_audioPitchField.UnregisterValueChangedCallback(HandleAudioPitchChanged);
            m_audioRandomPitchRangeField.UnregisterValueChangedCallback(HandleAudioRandomPitchRangeChanged);
            m_audioSpatialToggle.UnregisterValueChangedCallback(HandleAudioSpatialChanged);
            m_audioStopOnWindowEndToggle.UnregisterValueChangedCallback(HandleAudioStopOnWindowEndChanged);
            m_audioTargetTypeField.UnregisterValueChangedCallback(HandleAudioTargetTypeChanged);
            m_audioSocketChoiceField.UnregisterValueChangedCallback(HandleAudioSocketChoiceChanged);
            m_audioSocketIdField.UnregisterValueChangedCallback(HandleAudioSocketIdChanged);
            m_audioPositionOffsetField.UnregisterValueChangedCallback(HandleAudioPositionOffsetChanged);
            m_saveWindowButton.clicked -= RequestSaveWindow;
            m_deleteWindowButton.clicked -= RequestDeleteWindow;
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
            m_vfxSocketChoiceField.style.display = displayStyle;
            m_vfxSocketIdField.style.display = displayStyle;
            m_vfxLifeModeField.style.display = displayStyle;
            m_vfxPositionOffsetField.style.display = displayStyle;
            m_vfxEulerOffsetField.style.display = displayStyle;
            m_vfxFollowTargetToggle.style.display = displayStyle;
            m_audioTriggerTypeField.style.display = displayStyle;
            m_audioPlaybackTypeField.style.display = displayStyle;
            m_audioClipFoldout.style.display = displayStyle;
            m_audioVolumeField.style.display = displayStyle;
            m_audioPitchField.style.display = displayStyle;
            m_audioRandomPitchRangeField.style.display = displayStyle;
            m_audioSpatialToggle.style.display = displayStyle;
            m_audioStopOnWindowEndToggle.style.display = displayStyle;
            m_audioTargetTypeField.style.display = displayStyle;
            m_audioSocketChoiceField.style.display = displayStyle;
            m_audioSocketIdField.style.display = displayStyle;
            m_audioPositionOffsetField.style.display = displayStyle;
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
            m_vfxSocketChoiceField.SetEnabled(isEnabled && m_vfxSocketIdChoices.Count > 0);
            m_vfxSocketIdField.SetEnabled(isEnabled);
            m_vfxLifeModeField.SetEnabled(isEnabled);
            m_vfxPositionOffsetField.SetEnabled(isEnabled);
            m_vfxEulerOffsetField.SetEnabled(isEnabled);
            m_vfxFollowTargetToggle.SetEnabled(isEnabled);
            m_audioTriggerTypeField.SetEnabled(isEnabled);
            m_audioPlaybackTypeField.SetEnabled(isEnabled);
            m_audioClipFoldout.SetEnabled(isEnabled);
            m_audioVolumeField.SetEnabled(isEnabled);
            m_audioPitchField.SetEnabled(isEnabled);
            m_audioRandomPitchRangeField.SetEnabled(isEnabled);
            m_audioSpatialToggle.SetEnabled(isEnabled);
            m_audioStopOnWindowEndToggle.SetEnabled(isEnabled);
            m_audioTargetTypeField.SetEnabled(isEnabled);
            m_audioSocketChoiceField.SetEnabled(isEnabled && m_vfxSocketIdChoices.Count > 0);
            m_audioSocketIdField.SetEnabled(isEnabled);
            m_audioPositionOffsetField.SetEnabled(isEnabled);
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
                case "音效窗口":
                    type = AbilityWindowDraftType.Audio;
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
            {
                RefreshTypeSpecificFields(AbilityWindowDraftType.Vfx, targetType, AbilityAudioTargetType.SourceRoot);
                OnVfxTargetTypeChanged?.Invoke(targetType);
            }
        }

        // 提交特效窗口预制体编辑
        private void HandleVfxPrefabChanged(ChangeEvent<UnityEngine.Object> changeEvent)
        {
            OnVfxPrefabChanged?.Invoke(changeEvent.newValue as GameObject);
        }

        // 将挂点候选同步到挂点 Id
        private void HandleVfxSocketChoiceChanged(ChangeEvent<string> changeEvent)
        {
            if (changeEvent.newValue == NO_VFX_SOCKET_FOUND_CHOICE
                || changeEvent.newValue == CUSTOM_VFX_SOCKET_CHOICE)
                return;

            string socketId = changeEvent.newValue == NO_VFX_SOCKET_SELECTED_CHOICE
                ? string.Empty
                : changeEvent.newValue;
            ApplyVfxSocketId(socketId);
        }

        // 提交特效窗口挂点 Id 编辑
        private void HandleVfxSocketIdChanged(ChangeEvent<string> changeEvent)
        {
            RefreshVfxSocketChoiceField(changeEvent.newValue);
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

        // 提交音效窗口触发方式编辑
        private void HandleAudioTriggerTypeChanged(ChangeEvent<string> changeEvent)
        {
            if (Enum.TryParse(changeEvent.newValue, out AbilityAudioTriggerType triggerType))
                OnAudioTriggerTypeChanged?.Invoke(triggerType);
        }

        // 提交音效窗口播放类型编辑
        private void HandleAudioPlaybackTypeChanged(ChangeEvent<string> changeEvent)
        {
            if (Enum.TryParse(changeEvent.newValue, out AbilityAudioPlaybackType playbackType))
                OnAudioPlaybackTypeChanged?.Invoke(playbackType);
        }

        // 刷新音效资源列表
        private void RefreshAudioClipList(AbilityWindowDraft selectedWindow)
        {
            m_audioClipListRoot.Clear();
            IReadOnlyList<AudioClip> audioClips = selectedWindow.AudioClips;
            m_audioClipFoldout.text = $"音效数组  Size {audioClips.Count}";
            for (int clipIndex = 0; clipIndex < audioClips.Count; clipIndex++)
                CreateAudioClipRow(clipIndex, audioClips[clipIndex]);
        }

        // 创建单行音效资源编辑控件
        private void CreateAudioClipRow(int clipIndex, AudioClip audioClip)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.width = Length.Percent(100);
            row.style.marginBottom = 4f;

            Label clipLabel = new Label($"音效 {clipIndex + 1}");
            clipLabel.style.width = 52f;
            clipLabel.style.minWidth = 52f;
            clipLabel.style.flexShrink = 0f;

            ObjectField audioClipField = new ObjectField();
            audioClipField.objectType = typeof(AudioClip);
            audioClipField.allowSceneObjects = false;
            audioClipField.style.flexGrow = 1f;
            audioClipField.style.flexShrink = 1f;
            audioClipField.style.minWidth = 0f;
            audioClipField.SetValueWithoutNotify(audioClip);
            int currentClipIndex = clipIndex;
            audioClipField.RegisterValueChangedCallback(changeEvent =>
                OnAudioClipChanged?.Invoke(currentClipIndex, changeEvent.newValue as AudioClip));

            Button removeButton = new Button();
            removeButton.text = "删除";
            removeButton.style.width = 42f;
            removeButton.style.minWidth = 42f;
            removeButton.style.flexShrink = 0f;
            removeButton.style.marginLeft = 4f;
            removeButton.clicked += () => OnAudioClipRemoveRequested?.Invoke(currentClipIndex);

            row.Add(clipLabel);
            row.Add(audioClipField);
            row.Add(removeButton);
            m_audioClipListRoot.Add(row);
        }

        // 请求新增音效资源槽
        private void RequestAudioClipAdd()
        {
            OnAudioClipAddRequested?.Invoke();
        }

        // 提交音效窗口音量编辑
        private void HandleAudioVolumeChanged(ChangeEvent<float> changeEvent)
        {
            OnAudioVolumeChanged?.Invoke(changeEvent.newValue);
        }

        // 提交音效窗口音高编辑
        private void HandleAudioPitchChanged(ChangeEvent<float> changeEvent)
        {
            OnAudioPitchChanged?.Invoke(changeEvent.newValue);
        }

        // 提交音效窗口随机音高范围编辑
        private void HandleAudioRandomPitchRangeChanged(ChangeEvent<float> changeEvent)
        {
            OnAudioRandomPitchRangeChanged?.Invoke(changeEvent.newValue);
        }

        // 提交音效窗口空间化编辑
        private void HandleAudioSpatialChanged(ChangeEvent<bool> changeEvent)
        {
            OnAudioSpatialChanged?.Invoke(changeEvent.newValue);
        }

        // 提交音效窗口结束截断编辑
        private void HandleAudioStopOnWindowEndChanged(ChangeEvent<bool> changeEvent)
        {
            OnAudioStopOnWindowEndChanged?.Invoke(changeEvent.newValue);
        }

        // 提交音效窗口播放目标编辑
        private void HandleAudioTargetTypeChanged(ChangeEvent<string> changeEvent)
        {
            if (Enum.TryParse(changeEvent.newValue, out AbilityAudioTargetType targetType))
            {
                RefreshTypeSpecificFields(AbilityWindowDraftType.Audio, AbilityVfxTargetType.SourceSocket, targetType);
                OnAudioTargetTypeChanged?.Invoke(targetType);
            }
        }

        // 将音效挂点候选同步到音效挂点 Id
        private void HandleAudioSocketChoiceChanged(ChangeEvent<string> changeEvent)
        {
            if (changeEvent.newValue == NO_VFX_SOCKET_FOUND_CHOICE
                || changeEvent.newValue == CUSTOM_VFX_SOCKET_CHOICE)
                return;

            string socketId = changeEvent.newValue == NO_VFX_SOCKET_SELECTED_CHOICE
                ? string.Empty
                : changeEvent.newValue;
            ApplyAudioSocketId(socketId);
        }

        // 提交音效窗口挂点 Id 编辑
        private void HandleAudioSocketIdChanged(ChangeEvent<string> changeEvent)
        {
            RefreshAudioSocketChoiceField(changeEvent.newValue);
            OnAudioSocketIdChanged?.Invoke(changeEvent.newValue);
        }

        // 提交音效窗口位置偏移编辑
        private void HandleAudioPositionOffsetChanged(ChangeEvent<Vector3> changeEvent)
        {
            OnAudioPositionOffsetChanged?.Invoke(changeEvent.newValue);
        }

        // 请求将当前窗口草稿写入窗口轨道资产
        private void RequestSaveWindow()
        {
            OnSaveWindowRequested?.Invoke();
        }

        // 请求删除当前选中的窗口并写回窗口轨道资产
        private void RequestDeleteWindow()
        {
            OnDeleteWindowRequested?.Invoke();
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
                case AbilityWindowDraftType.Audio:
                    return "音效窗口";
                default:
                    return "命中窗口";
            }
        }

        // 根据窗口类型显示对应参数
        private void RefreshTypeSpecificFields(AbilityWindowDraftType type, AbilityVfxTargetType vfxTargetType,
            AbilityAudioTargetType audioTargetType)
        {
            bool isHit = type == AbilityWindowDraftType.Hit;
            bool isVfx = type == AbilityWindowDraftType.Vfx;
            bool isAudio = type == AbilityWindowDraftType.Audio;
            bool usesVfxSocket = isVfx && UsesVfxSocket(vfxTargetType);
            bool usesAudioSocket = isAudio && UsesAudioSocket(audioTargetType);
            m_damageField.style.display = isHit ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxTriggerTypeField.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxTargetTypeField.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxPrefabField.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxSocketChoiceField.style.display = usesVfxSocket ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxSocketIdField.style.display = usesVfxSocket ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxLifeModeField.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxPositionOffsetField.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxEulerOffsetField.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
            m_vfxFollowTargetToggle.style.display = isVfx ? DisplayStyle.Flex : DisplayStyle.None;
            m_audioTriggerTypeField.style.display = isAudio ? DisplayStyle.Flex : DisplayStyle.None;
            m_audioPlaybackTypeField.style.display = isAudio ? DisplayStyle.Flex : DisplayStyle.None;
            m_audioClipFoldout.style.display = isAudio ? DisplayStyle.Flex : DisplayStyle.None;
            m_audioVolumeField.style.display = isAudio ? DisplayStyle.Flex : DisplayStyle.None;
            m_audioPitchField.style.display = isAudio ? DisplayStyle.Flex : DisplayStyle.None;
            m_audioRandomPitchRangeField.style.display = isAudio ? DisplayStyle.Flex : DisplayStyle.None;
            m_audioSpatialToggle.style.display = isAudio ? DisplayStyle.Flex : DisplayStyle.None;
            m_audioStopOnWindowEndToggle.style.display = isAudio ? DisplayStyle.Flex : DisplayStyle.None;
            m_audioTargetTypeField.style.display = isAudio ? DisplayStyle.Flex : DisplayStyle.None;
            m_audioSocketChoiceField.style.display = usesAudioSocket ? DisplayStyle.Flex : DisplayStyle.None;
            m_audioSocketIdField.style.display = usesAudioSocket ? DisplayStyle.Flex : DisplayStyle.None;
            m_audioPositionOffsetField.style.display = isAudio ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // 判断当前生成目标是否需要挂点 Id
        private bool UsesVfxSocket(AbilityVfxTargetType targetType)
        {
            return targetType == AbilityVfxTargetType.SourceSocket
                   || targetType == AbilityVfxTargetType.HitTargetSocket;
        }

        // 判断当前音效播放目标是否需要挂点 Id
        private bool UsesAudioSocket(AbilityAudioTargetType targetType)
        {
            return targetType == AbilityAudioTargetType.SourceSocket
                   || targetType == AbilityAudioTargetType.HitTargetSocket;
        }

        // 应用候选选择得到的挂点 Id
        private void ApplyVfxSocketId(string socketId)
        {
            m_vfxSocketIdField.SetValueWithoutNotify(socketId);
            RefreshVfxSocketChoiceField(socketId);
            OnVfxSocketIdChanged?.Invoke(socketId);
        }

        // 应用候选选择得到的音效挂点 Id
        private void ApplyAudioSocketId(string socketId)
        {
            m_audioSocketIdField.SetValueWithoutNotify(socketId);
            RefreshAudioSocketChoiceField(socketId);
            OnAudioSocketIdChanged?.Invoke(socketId);
        }

        // 刷新挂点下拉候选和当前显示项
        private void RefreshVfxSocketChoiceField(string currentSocketId)
        {
            if (m_vfxSocketChoiceField == null)
                return;

            m_vfxSocketChoiceValues.Clear();
            if (m_vfxSocketIdChoices.Count == 0)
            {
                m_vfxSocketChoiceValues.Add(NO_VFX_SOCKET_FOUND_CHOICE);
                m_vfxSocketChoiceField.choices = m_vfxSocketChoiceValues;
                m_vfxSocketChoiceField.SetValueWithoutNotify(NO_VFX_SOCKET_FOUND_CHOICE);
                m_vfxSocketChoiceField.SetEnabled(false);
                return;
            }

            m_vfxSocketChoiceValues.Add(NO_VFX_SOCKET_SELECTED_CHOICE);
            m_vfxSocketChoiceValues.AddRange(m_vfxSocketIdChoices);
            if (!string.IsNullOrEmpty(currentSocketId) && !m_vfxSocketIdChoices.Contains(currentSocketId))
                m_vfxSocketChoiceValues.Add(CUSTOM_VFX_SOCKET_CHOICE);

            m_vfxSocketChoiceField.choices = m_vfxSocketChoiceValues;
            m_vfxSocketChoiceField.SetValueWithoutNotify(GetVfxSocketChoice(currentSocketId));
            m_vfxSocketChoiceField.SetEnabled(true);
        }

        // 将当前挂点 Id 映射到下拉显示项
        private string GetVfxSocketChoice(string socketId)
        {
            if (string.IsNullOrEmpty(socketId))
                return NO_VFX_SOCKET_SELECTED_CHOICE;

            if (m_vfxSocketIdChoices.Contains(socketId))
                return socketId;

            return CUSTOM_VFX_SOCKET_CHOICE;
        }

        // 刷新音效挂点下拉候选和当前显示项
        private void RefreshAudioSocketChoiceField(string currentSocketId)
        {
            if (m_audioSocketChoiceField == null)
                return;

            m_vfxSocketChoiceValues.Clear();
            if (m_vfxSocketIdChoices.Count == 0)
            {
                m_vfxSocketChoiceValues.Add(NO_VFX_SOCKET_FOUND_CHOICE);
                m_audioSocketChoiceField.choices = m_vfxSocketChoiceValues;
                m_audioSocketChoiceField.SetValueWithoutNotify(NO_VFX_SOCKET_FOUND_CHOICE);
                m_audioSocketChoiceField.SetEnabled(false);
                return;
            }

            m_vfxSocketChoiceValues.Add(NO_VFX_SOCKET_SELECTED_CHOICE);
            m_vfxSocketChoiceValues.AddRange(m_vfxSocketIdChoices);
            if (!string.IsNullOrEmpty(currentSocketId) && !m_vfxSocketIdChoices.Contains(currentSocketId))
                m_vfxSocketChoiceValues.Add(CUSTOM_VFX_SOCKET_CHOICE);

            m_audioSocketChoiceField.choices = m_vfxSocketChoiceValues;
            m_audioSocketChoiceField.SetValueWithoutNotify(GetVfxSocketChoice(currentSocketId));
            m_audioSocketChoiceField.SetEnabled(true);
        }

    }
}
