/*
 * ┌────────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 主视图，管理资源输入、播放控件与状态栏
 * │  类    名: AbilityComposerView.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Framework.QTower.Editor.View;
using Module.Ability.Data.Window;
using Module.Ability.Data.Window.Hit;
using Module.Ability.Data.Window.Vfx;
using Tools.AbilityComposer.Editor.View.Center;
using Tools.AbilityComposer.Editor.View.Center.Event;
using Tools.AbilityComposer.Editor.View.Center.Timeline;
using Tools.AbilityComposer.Editor.View.Left;
using Tools.AbilityComposer.Editor.View.Right;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Utils.log;
using UiFontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace Tools.AbilityComposer.Editor.View
{
    public sealed class AbilityComposerView : UIBaseEditor
    {
        private const string WINDOW_UXML_PATH = "Assets/Scripts/Tools/AbilityComposer/Editor/UI/Uxml/AbilityComposerWindow.uxml";
        private const string WINDOW_USS_PATH = "Assets/Scripts/Tools/AbilityComposer/Editor/UI/Uss/AbilityComposerWindow.uss";
        private const string MI_SANS_FONT_ASSET_PATH = "Assets/Fonts/miSans/MiSans-Regular-UI Toolkit.asset";
        private const float COMPACT_LAYOUT_WIDTH = 1200f;
        private const float NARROW_LAYOUT_WIDTH = 900f;
        private const string NORMAL_LAYOUT_CLASS = "ac-layout-normal";
        private const string COMPACT_LAYOUT_CLASS = "ac-layout-compact";
        private const string NARROW_LAYOUT_CLASS = "ac-layout-narrow";

        private ObjectField m_previewSourceField;
        private DropdownField m_prefabAnimationClipField;
        private ObjectField m_animationClipField;
        private Toggle m_showGlobalAnimationToggle;
        private Button m_returnPreviousSceneButton;
        private Button m_createPreviewButton;
        private Button m_focusPreviewButton;
        private Button m_saveAllButton;
        private Button m_undoButton;
        private VisualElement m_rootVisualElement;
        private AbilityComposerData m_composerData;
        private AbilityLeftView m_leftView;
        private AbilityCenterView m_centerView;
        private AbilityRightView m_rightView;
        private readonly List<AnimationClip> m_prefabAnimationClips = new List<AnimationClip>();
        private bool m_isControlsReady;

        public event Action<GameObject> OnPreviewSourceChanged;
        public event Action<AnimationClip> OnAnimationClipChanged;
        public event Action<bool> OnShowGlobalAnimationsChanged;
        public event Action<AbilityWindowConfigSO> OnWindowConfigChanged;
        public event Action OnCreatePreviewRequested;
        public event Action OnFocusPreviewRequested;
        public event Action OnReturnPreviousSceneRequested;
        public event Action OnJumpFirstFrameRequested;
        public event Action OnPreviousFrameRequested;
        public event Action OnPlaybackToggled;
        public event Action OnNextFrameRequested;
        public event Action OnJumpLastFrameRequested;
        public event Action<int> OnCurrentFrameChanged;
        public event Action OnAddEventRequested;
        public event Action OnDeleteSelectedEventRequested;
        public event Action OnAddWindowRequested;
        public event Action OnDeleteSelectedWindowRequested;
        public event Action<bool> OnHitWindowTrackToggled;
        public event Action<bool> OnStepAdvanceWindowTrackToggled;
        public event Action<bool> OnMovementLockWindowTrackToggled;
        public event Action<bool> OnVfxWindowTrackToggled;
        public event Action<AbilityEventCategory> OnEventCategoryChanged;
        public event Action<string> OnEventReceiverTypeNameChanged;
        public event Action<string> OnEventFunctionNameChanged;
        public event Action<AbilityWindowDraftType> OnWindowTypeChanged;
        public event Action<int, int> OnWindowFramesChanged;
        public event Action<float> OnWindowDamageChanged;
        public event Action<AbilityVfxTriggerType> OnWindowVfxTriggerTypeChanged;
        public event Action<AbilityVfxTargetType> OnWindowVfxTargetTypeChanged;
        public event Action<GameObject> OnWindowVfxPrefabChanged;
        public event Action<string> OnWindowVfxSocketIdChanged;
        public event Action<AbilityVfxLifeMode> OnWindowVfxLifeModeChanged;
        public event Action<Vector3> OnWindowVfxPositionOffsetChanged;
        public event Action<Vector3> OnWindowVfxEulerOffsetChanged;
        public event Action<bool> OnWindowVfxFollowTargetChanged;
        public event Action OnSaveWindowRequested;
        public event Action OnSaveEventRequested;
        public event Action OnCloseEventInspectorRequested;
        public event Action OnCloseWindowInspectorRequested;
        public event Action OnSaveAllRequested;
        public event Action OnUndoRequested;

        // 注入主视图需要的根节点与工作上下文
        public AbilityComposerView(VisualElement rootVisualElement, AbilityComposerData composerData)
        {
            m_rootVisualElement = rootVisualElement;
            m_composerData = composerData;
        }

        // 创建窗口布局并绑定主视图控件
        protected override void OnEditorInit()
        {
            m_isControlsReady = false;
            VisualTreeAsset windowVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WINDOW_UXML_PATH);
            if (windowVisualTree == null)
            {
                QLog.Error($"未找到窗口布局资源：{WINDOW_UXML_PATH}");
                CreateMissingAssetView(m_rootVisualElement, "未找到 Ability Composer 的 UXML 布局资源");
                return;
            }

            windowVisualTree.CloneTree(m_rootVisualElement);
            StyleSheet windowStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(WINDOW_USS_PATH);
            if (windowStyleSheet == null)
            {
                QLog.Error($"未找到窗口样式资源：{WINDOW_USS_PATH}");
                CreateMissingAssetView(m_rootVisualElement, "未找到 Ability Composer 的 USS 样式资源");
                return;
            }

            m_rootVisualElement.styleSheets.Add(windowStyleSheet);
            if (!FindControls(m_rootVisualElement))
                return;

            m_leftView = new AbilityLeftView(m_rootVisualElement.Q<VisualElement>("preview-control-panel"));
            m_centerView = new AbilityCenterView(m_rootVisualElement.Q<VisualElement>("timeline-panel"));
            m_rightView = new AbilityRightView(m_rootVisualElement.Q<VisualElement>("event-inspector-panel"));
            m_leftView.Init();
            m_centerView.Init();
            m_rightView.Init();
            ConfigureResourceFields(m_composerData);
            ApplyMiSansFont(m_rootVisualElement);
            m_rootVisualElement.RegisterCallback<GeometryChangedEvent>(HandleRootGeometryChanged);
            m_isControlsReady = true;
            ApplyLayoutMode(m_rootVisualElement.resolvedStyle.width);
        }

        // 返回时间轴内容容器，供独立时间轴视图装配
        public bool TryGetTimelineElements(out ScrollView timelineScrollView, out VisualElement timelineContent)
        {
            return m_centerView.TryGetTimelineElements(out timelineScrollView, out timelineContent);
        }

        // 使用当前数据刷新播放控件与状态文字
        public void Refresh(AbilityTimelineData timelineData, bool hasPreview, bool canEditWindows, bool canUndo,
            IReadOnlyList<string> vfxSocketIdChoices)
        {
            m_returnPreviousSceneButton.SetEnabled(hasPreview);
            m_saveAllButton.SetEnabled(timelineData.HasClip);
            m_undoButton.SetEnabled(canUndo);
            m_leftView.Refresh(timelineData, hasPreview, canEditWindows);
            m_rightView.Refresh(timelineData, vfxSocketIdChoices);
        }

        // 更新右侧 Event Inspector 的 Function 下拉候选
        public void SetEventFunctionChoices(IReadOnlyDictionary<string, List<string>> functionChoices)
        {
            m_rightView.SetEventFunctionChoices(functionChoices);
        }

        // 更新当前预制体可选的动画片段
        public void SetPrefabAnimationClips(IReadOnlyList<AnimationClip> animationClips, AnimationClip selectedAnimationClip)
        {
            m_prefabAnimationClips.Clear();
            List<string> clipNames = new List<string>();
            for (int clipIndex = 0; clipIndex < animationClips.Count; clipIndex++)
            {
                AnimationClip animationClip = animationClips[clipIndex];
                m_prefabAnimationClips.Add(animationClip);
                clipNames.Add(animationClip.name);
            }

            if (clipNames.Count == 0)
            {
                clipNames.Add("当前预制体未引用动画");
                m_prefabAnimationClipField.choices = clipNames;
                m_prefabAnimationClipField.SetValueWithoutNotify(clipNames[0]);
                m_prefabAnimationClipField.SetEnabled(false);
                return;
            }

            m_prefabAnimationClipField.choices = clipNames;
            m_prefabAnimationClipField.SetEnabled(true);
            SetSelectedAnimationClip(selectedAnimationClip);
        }

        // 同步全局与预制体动画选择控件的当前值
        public void SetSelectedAnimationClip(AnimationClip animationClip)
        {
            m_animationClipField.SetValueWithoutNotify(animationClip);
            int clipIndex = m_prefabAnimationClips.IndexOf(animationClip);
            if (clipIndex >= 0)
                m_prefabAnimationClipField.SetValueWithoutNotify(m_prefabAnimationClipField.choices[clipIndex]);
        }

        // 切换全局动画与预制体动画选择控件
        public void SetShowGlobalAnimations(bool showGlobalAnimations)
        {
            m_showGlobalAnimationToggle.SetValueWithoutNotify(showGlobalAnimations);
            m_animationClipField.style.display = showGlobalAnimations ? DisplayStyle.Flex : DisplayStyle.None;
            m_prefabAnimationClipField.style.display = showGlobalAnimations ? DisplayStyle.None : DisplayStyle.Flex;
        }

        protected override void SubscribeViewEvents()
        {
            if (!m_isControlsReady)
                return;

            m_previewSourceField.RegisterValueChangedCallback(HandlePreviewSourceChanged);
            m_prefabAnimationClipField.RegisterValueChangedCallback(HandlePrefabAnimationClipChanged);
            m_animationClipField.RegisterValueChangedCallback(HandleAnimationClipChanged);
            m_showGlobalAnimationToggle.RegisterValueChangedCallback(HandleShowGlobalAnimationChanged);
            m_returnPreviousSceneButton.clicked += RequestReturnPreviousScene;
            m_createPreviewButton.clicked += RequestCreatePreview;
            m_focusPreviewButton.clicked += RequestFocusPreview;
            m_saveAllButton.clicked += RequestSaveAll;
            m_undoButton.clicked += RequestUndo;
            m_leftView.OnJumpFirstFrameRequested += RequestJumpFirstFrame;
            m_leftView.OnPreviousFrameRequested += RequestPreviousFrame;
            m_leftView.OnPlaybackToggled += RequestPlaybackToggle;
            m_leftView.OnNextFrameRequested += RequestNextFrame;
            m_leftView.OnJumpLastFrameRequested += RequestJumpLastFrame;
            m_leftView.OnCurrentFrameChanged += RequestCurrentFrameChanged;
            m_leftView.OnAddEventRequested += RequestAddEvent;
            m_leftView.OnDeleteSelectedEventRequested += RequestDeleteSelectedEvent;
            m_leftView.OnAddWindowRequested += RequestAddWindow;
            m_leftView.OnDeleteSelectedWindowRequested += RequestDeleteSelectedWindow;
            m_leftView.OnHitWindowTrackToggled += RequestHitWindowTrackToggled;
            m_leftView.OnStepAdvanceWindowTrackToggled += RequestStepAdvanceWindowTrackToggled;
            m_leftView.OnMovementLockWindowTrackToggled += RequestMovementLockWindowTrackToggled;
            m_leftView.OnVfxWindowTrackToggled += RequestVfxWindowTrackToggled;
            m_rightView.OnEventCategoryChanged += RequestEventCategoryChanged;
            m_rightView.OnEventReceiverTypeNameChanged += RequestEventReceiverTypeNameChanged;
            m_rightView.OnEventFunctionNameChanged += RequestEventFunctionNameChanged;
            m_rightView.OnSaveEventRequested += RequestSaveEvent;
            m_rightView.OnWindowTypeChanged += RequestWindowTypeChanged;
            m_rightView.OnWindowConfigChanged += RequestWindowConfigChanged;
            m_rightView.OnWindowFramesChanged += RequestWindowFramesChanged;
            m_rightView.OnWindowDamageChanged += RequestWindowDamageChanged;
            m_rightView.OnWindowVfxTriggerTypeChanged += RequestWindowVfxTriggerTypeChanged;
            m_rightView.OnWindowVfxTargetTypeChanged += RequestWindowVfxTargetTypeChanged;
            m_rightView.OnWindowVfxPrefabChanged += RequestWindowVfxPrefabChanged;
            m_rightView.OnWindowVfxSocketIdChanged += RequestWindowVfxSocketIdChanged;
            m_rightView.OnWindowVfxLifeModeChanged += RequestWindowVfxLifeModeChanged;
            m_rightView.OnWindowVfxPositionOffsetChanged += RequestWindowVfxPositionOffsetChanged;
            m_rightView.OnWindowVfxEulerOffsetChanged += RequestWindowVfxEulerOffsetChanged;
            m_rightView.OnWindowVfxFollowTargetChanged += RequestWindowVfxFollowTargetChanged;
            m_rightView.OnSaveWindowRequested += RequestSaveWindow;
            m_rightView.OnCloseEventInspectorRequested += RequestCloseEventInspector;
            m_rightView.OnCloseWindowInspectorRequested += RequestCloseWindowInspector;
        }

        protected override void UnsubscribeViewEvents()
        {
            if (!m_isControlsReady)
                return;

            m_previewSourceField.UnregisterValueChangedCallback(HandlePreviewSourceChanged);
            m_prefabAnimationClipField.UnregisterValueChangedCallback(HandlePrefabAnimationClipChanged);
            m_animationClipField.UnregisterValueChangedCallback(HandleAnimationClipChanged);
            m_showGlobalAnimationToggle.UnregisterValueChangedCallback(HandleShowGlobalAnimationChanged);
            m_returnPreviousSceneButton.clicked -= RequestReturnPreviousScene;
            m_createPreviewButton.clicked -= RequestCreatePreview;
            m_focusPreviewButton.clicked -= RequestFocusPreview;
            m_saveAllButton.clicked -= RequestSaveAll;
            m_undoButton.clicked -= RequestUndo;
            m_leftView.OnJumpFirstFrameRequested -= RequestJumpFirstFrame;
            m_leftView.OnPreviousFrameRequested -= RequestPreviousFrame;
            m_leftView.OnPlaybackToggled -= RequestPlaybackToggle;
            m_leftView.OnNextFrameRequested -= RequestNextFrame;
            m_leftView.OnJumpLastFrameRequested -= RequestJumpLastFrame;
            m_leftView.OnCurrentFrameChanged -= RequestCurrentFrameChanged;
            m_leftView.OnAddEventRequested -= RequestAddEvent;
            m_leftView.OnDeleteSelectedEventRequested -= RequestDeleteSelectedEvent;
            m_leftView.OnAddWindowRequested -= RequestAddWindow;
            m_leftView.OnDeleteSelectedWindowRequested -= RequestDeleteSelectedWindow;
            m_leftView.OnHitWindowTrackToggled -= RequestHitWindowTrackToggled;
            m_leftView.OnStepAdvanceWindowTrackToggled -= RequestStepAdvanceWindowTrackToggled;
            m_leftView.OnMovementLockWindowTrackToggled -= RequestMovementLockWindowTrackToggled;
            m_leftView.OnVfxWindowTrackToggled -= RequestVfxWindowTrackToggled;
            m_rightView.OnEventCategoryChanged -= RequestEventCategoryChanged;
            m_rightView.OnEventReceiverTypeNameChanged -= RequestEventReceiverTypeNameChanged;
            m_rightView.OnEventFunctionNameChanged -= RequestEventFunctionNameChanged;
            m_rightView.OnSaveEventRequested -= RequestSaveEvent;
            m_rightView.OnWindowTypeChanged -= RequestWindowTypeChanged;
            m_rightView.OnWindowConfigChanged -= RequestWindowConfigChanged;
            m_rightView.OnWindowFramesChanged -= RequestWindowFramesChanged;
            m_rightView.OnWindowDamageChanged -= RequestWindowDamageChanged;
            m_rightView.OnWindowVfxTriggerTypeChanged -= RequestWindowVfxTriggerTypeChanged;
            m_rightView.OnWindowVfxTargetTypeChanged -= RequestWindowVfxTargetTypeChanged;
            m_rightView.OnWindowVfxPrefabChanged -= RequestWindowVfxPrefabChanged;
            m_rightView.OnWindowVfxSocketIdChanged -= RequestWindowVfxSocketIdChanged;
            m_rightView.OnWindowVfxLifeModeChanged -= RequestWindowVfxLifeModeChanged;
            m_rightView.OnWindowVfxPositionOffsetChanged -= RequestWindowVfxPositionOffsetChanged;
            m_rightView.OnWindowVfxEulerOffsetChanged -= RequestWindowVfxEulerOffsetChanged;
            m_rightView.OnWindowVfxFollowTargetChanged -= RequestWindowVfxFollowTargetChanged;
            m_rightView.OnSaveWindowRequested -= RequestSaveWindow;
            m_rightView.OnCloseEventInspectorRequested -= RequestCloseEventInspector;
            m_rightView.OnCloseWindowInspectorRequested -= RequestCloseWindowInspector;
            m_isControlsReady = false;
        }

        protected override void OnEditorDispose()
        {
            m_rootVisualElement?.UnregisterCallback<GeometryChangedEvent>(HandleRootGeometryChanged);
            m_leftView?.Destroy();
            m_centerView?.Destroy();
            m_rightView?.Destroy();
        }

        // 根据窗口宽度切换 Ability Composer 的响应式布局档位
        private void HandleRootGeometryChanged(GeometryChangedEvent geometryChangedEvent)
        {
            ApplyLayoutMode(geometryChangedEvent.newRect.width);
        }

        // 将当前宽度映射为正常、紧凑或窄屏布局类
        private void ApplyLayoutMode(float width)
        {
            if (width <= 0f)
                return;

            m_rootVisualElement.RemoveFromClassList(NORMAL_LAYOUT_CLASS);
            m_rootVisualElement.RemoveFromClassList(COMPACT_LAYOUT_CLASS);
            m_rootVisualElement.RemoveFromClassList(NARROW_LAYOUT_CLASS);
            if (width < NARROW_LAYOUT_WIDTH)
                m_rootVisualElement.AddToClassList(NARROW_LAYOUT_CLASS);
            else if (width < COMPACT_LAYOUT_WIDTH)
                m_rootVisualElement.AddToClassList(COMPACT_LAYOUT_CLASS);
            else
                m_rootVisualElement.AddToClassList(NORMAL_LAYOUT_CLASS);
        }

        // 查找 UXML 中的主视图控件
        private bool FindControls(VisualElement rootVisualElement)
        {
            m_previewSourceField = rootVisualElement.Q<ObjectField>("preview-source-field");
            m_prefabAnimationClipField = rootVisualElement.Q<DropdownField>("prefab-animation-clip-field");
            m_animationClipField = rootVisualElement.Q<ObjectField>("animation-clip-field");
            m_showGlobalAnimationToggle = rootVisualElement.Q<Toggle>("show-global-animation-toggle");
            m_returnPreviousSceneButton = rootVisualElement.Q<Button>("return-previous-scene-button");
            m_createPreviewButton = rootVisualElement.Q<Button>("create-preview-button");
            m_focusPreviewButton = rootVisualElement.Q<Button>("focus-preview-button");
            m_saveAllButton = rootVisualElement.Q<Button>("save-all-button");
            m_undoButton = rootVisualElement.Q<Button>("undo-button");

            if (m_previewSourceField == null || m_prefabAnimationClipField == null || m_animationClipField == null
                || m_showGlobalAnimationToggle == null || m_returnPreviousSceneButton == null
                || m_createPreviewButton == null || m_focusPreviewButton == null || m_saveAllButton == null
                || m_undoButton == null)
            {
                QLog.Error("配置 Ability Composer 主视图失败：缺少必要的 UXML 控件");
                return false;
            }

            return true;
        }

        // 设置资源输入控件的类型、提示与初始值
        private void ConfigureResourceFields(AbilityComposerData composerData)
        {
            m_previewSourceField.objectType = typeof(GameObject);
            m_previewSourceField.allowSceneObjects = false;
            m_previewSourceField.tooltip = "仅支持 Project 中的 Prefab 资源";
            m_previewSourceField.SetValueWithoutNotify(composerData.PreviewSource);
            m_animationClipField.objectType = typeof(AnimationClip);
            m_animationClipField.allowSceneObjects = false;
            m_animationClipField.SetValueWithoutNotify(composerData.SelectedAnimationClip);
            m_showGlobalAnimationToggle.tooltip = "开启后可从项目 Assets 中选择任意 AnimationClip";
            SetShowGlobalAnimations(composerData.IsShowingGlobalAnimations);
        }

        // 将预览资源变更转换为用户意图
        private void HandlePreviewSourceChanged(ChangeEvent<UnityEngine.Object> changeEvent)
        {
            GameObject previewPrefab = changeEvent.newValue as GameObject;
            if (changeEvent.newValue != null && !PrefabUtility.IsPartOfPrefabAsset(changeEvent.newValue))
            {
                QLog.Error("预览对象只支持 Project 中的 Prefab 资源");
                m_previewSourceField.SetValueWithoutNotify(null);
                previewPrefab = null;
            }

            OnPreviewSourceChanged?.Invoke(previewPrefab);
        }

        // 将动画资源变更转换为用户意图
        private void HandleAnimationClipChanged(ChangeEvent<UnityEngine.Object> changeEvent)
        {
            OnAnimationClipChanged?.Invoke(changeEvent.newValue as AnimationClip);
        }

        // 将预制体动画下拉选择转换为用户意图
        private void HandlePrefabAnimationClipChanged(ChangeEvent<string> changeEvent)
        {
            int clipIndex = m_prefabAnimationClipField.choices.IndexOf(changeEvent.newValue);
            if (clipIndex < 0 || clipIndex >= m_prefabAnimationClips.Count)
                return;

            OnAnimationClipChanged?.Invoke(m_prefabAnimationClips[clipIndex]);
        }

        // 将全局动画开关变更转换为用户意图
        private void HandleShowGlobalAnimationChanged(ChangeEvent<bool> changeEvent)
        {
            OnShowGlobalAnimationsChanged?.Invoke(changeEvent.newValue);
        }

        // 请求返回进入预览前的场景
        private void RequestReturnPreviousScene()
        {
            OnReturnPreviousSceneRequested?.Invoke();
        }

        // 请求创建临时预览对象
        private void RequestCreatePreview()
        {
            OnCreatePreviewRequested?.Invoke();
        }

        // 请求聚焦临时预览对象
        private void RequestFocusPreview()
        {
            OnFocusPreviewRequested?.Invoke();
        }

        // 请求一键保存全部草稿
        private void RequestSaveAll() => OnSaveAllRequested?.Invoke();

        // 请求撤销 Composer 的上一步操作
        private void RequestUndo() => OnUndoRequested?.Invoke();

        // 请求跳转到第一帧
        private void RequestJumpFirstFrame()
        {
            OnJumpFirstFrameRequested?.Invoke();
        }

        // 请求显示前一帧
        private void RequestPreviousFrame()
        {
            OnPreviousFrameRequested?.Invoke();
        }

        // 请求切换播放状态
        private void RequestPlaybackToggle()
        {
            OnPlaybackToggled?.Invoke();
        }

        // 请求显示后一帧
        private void RequestNextFrame()
        {
            OnNextFrameRequested?.Invoke();
        }

        // 请求跳转到最后一帧
        private void RequestJumpLastFrame()
        {
            OnJumpLastFrameRequested?.Invoke();
        }

        // 转发左侧帧输入请求
        private void RequestCurrentFrameChanged(int frame)
        {
            OnCurrentFrameChanged?.Invoke(frame);
        }

        // 转发添加事件请求
        private void RequestAddEvent() => OnAddEventRequested?.Invoke();

        // 转发删除选中事件请求
        private void RequestDeleteSelectedEvent() => OnDeleteSelectedEventRequested?.Invoke();

        // 转发创建通用窗口请求
        private void RequestAddWindow() => OnAddWindowRequested?.Invoke();

        // 转发删除选中窗口请求
        private void RequestDeleteSelectedWindow() => OnDeleteSelectedWindowRequested?.Invoke();

        // 转发命中窗口轨道开关请求
        private void RequestHitWindowTrackToggled(bool isEnabled) => OnHitWindowTrackToggled?.Invoke(isEnabled);

        // 转发技能推进窗口轨道开关请求
        private void RequestStepAdvanceWindowTrackToggled(bool isEnabled) => OnStepAdvanceWindowTrackToggled?.Invoke(isEnabled);

        // 转发移动锁定窗口轨道开关请求
        private void RequestMovementLockWindowTrackToggled(bool isEnabled) => OnMovementLockWindowTrackToggled?.Invoke(isEnabled);

        // 转发特效窗口轨道开关请求
        private void RequestVfxWindowTrackToggled(bool isEnabled) => OnVfxWindowTrackToggled?.Invoke(isEnabled);

        // 转发事件分类编辑请求
        private void RequestEventCategoryChanged(AbilityEventCategory category) => OnEventCategoryChanged?.Invoke(category);

        // 转发事件接收类编辑请求
        private void RequestEventReceiverTypeNameChanged(string receiverTypeName) => OnEventReceiverTypeNameChanged?.Invoke(receiverTypeName);

        // 转发事件 Function 编辑请求
        private void RequestEventFunctionNameChanged(string functionName) => OnEventFunctionNameChanged?.Invoke(functionName);

        // 转发窗口类型编辑请求
        private void RequestWindowTypeChanged(AbilityWindowDraftType type) => OnWindowTypeChanged?.Invoke(type);

        // 转发窗口主体配置切换请求
        private void RequestWindowConfigChanged(AbilityWindowConfigSO windowConfig) => OnWindowConfigChanged?.Invoke(windowConfig);

        // 转发窗口帧范围编辑请求
        private void RequestWindowFramesChanged(int startFrame, int endFrame) => OnWindowFramesChanged?.Invoke(startFrame, endFrame);

        // 转发窗口伤害编辑请求
        private void RequestWindowDamageChanged(float damage) => OnWindowDamageChanged?.Invoke(damage);

        // 转发特效窗口触发方式编辑请求
        private void RequestWindowVfxTriggerTypeChanged(AbilityVfxTriggerType triggerType) => OnWindowVfxTriggerTypeChanged?.Invoke(triggerType);

        // 转发特效窗口目标编辑请求
        private void RequestWindowVfxTargetTypeChanged(AbilityVfxTargetType targetType) => OnWindowVfxTargetTypeChanged?.Invoke(targetType);

        // 转发特效窗口预制体编辑请求
        private void RequestWindowVfxPrefabChanged(GameObject vfxPrefab) => OnWindowVfxPrefabChanged?.Invoke(vfxPrefab);

        // 转发特效窗口挂点 Id 编辑请求
        private void RequestWindowVfxSocketIdChanged(string socketId) => OnWindowVfxSocketIdChanged?.Invoke(socketId);

        // 转发特效窗口生命周期编辑请求
        private void RequestWindowVfxLifeModeChanged(AbilityVfxLifeMode lifeMode) => OnWindowVfxLifeModeChanged?.Invoke(lifeMode);

        // 转发特效窗口位置偏移编辑请求
        private void RequestWindowVfxPositionOffsetChanged(Vector3 positionOffset) => OnWindowVfxPositionOffsetChanged?.Invoke(positionOffset);

        // 转发特效窗口旋转偏移编辑请求
        private void RequestWindowVfxEulerOffsetChanged(Vector3 eulerOffset) => OnWindowVfxEulerOffsetChanged?.Invoke(eulerOffset);

        // 转发特效窗口跟随目标编辑请求
        private void RequestWindowVfxFollowTargetChanged(bool followTarget) => OnWindowVfxFollowTargetChanged?.Invoke(followTarget);

        // 转发保存窗口轨道请求
        private void RequestSaveWindow() => OnSaveWindowRequested?.Invoke();

        // 转发保存事件请求
        private void RequestSaveEvent() => OnSaveEventRequested?.Invoke();

        // 转发关闭事件检查器页签请求
        private void RequestCloseEventInspector() => OnCloseEventInspectorRequested?.Invoke();

        // 转发关闭窗口检查器页签请求
        private void RequestCloseWindowInspector() => OnCloseWindowInspectorRequested?.Invoke();

        // 为窗口文字应用 MiSans，避免中文回退到系统粗体字体
        private void ApplyMiSansFont(VisualElement rootVisualElement)
        {
            UiFontAsset miSansFontAsset = AssetDatabase.LoadAssetAtPath<UiFontAsset>(MI_SANS_FONT_ASSET_PATH);
            if (miSansFontAsset == null)
            {
                QLog.Error($"未找到 Ability Composer 的 MiSans 字体资源：{MI_SANS_FONT_ASSET_PATH}");
                return;
            }

            StyleFontDefinition fontDefinition = new StyleFontDefinition(miSansFontAsset);
            List<TextElement> textElements = rootVisualElement.Query<TextElement>().ToList();
            foreach (TextElement textElement in textElements)
                textElement.style.unityFontDefinition = fontDefinition;
        }

        // 在窗口中显示缺失 UXML 或 USS 时的明确错误信息
        private void CreateMissingAssetView(VisualElement rootVisualElement, string message)
        {
            Label errorLabel = new Label(message);
            errorLabel.style.paddingLeft = 12f;
            errorLabel.style.paddingRight = 12f;
            errorLabel.style.paddingTop = 12f;
            errorLabel.style.paddingBottom = 12f;
            errorLabel.style.color = new Color(1f, 0.45f, 0.45f, 1f);
            rootVisualElement.Add(errorLabel);
        }
    }
}
