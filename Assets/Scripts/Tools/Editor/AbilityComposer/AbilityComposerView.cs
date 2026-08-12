/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 主视图，管理资源输入、播放控件与状态栏
 * │  类    名: AbilityComposerView.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Framework.QTower.Editor.View;
using Tools.Editor.AbilityComposer.Center;
using Tools.Editor.AbilityComposer.Center.Event;
using Tools.Editor.AbilityComposer.Center.Timeline;
using Tools.Editor.AbilityComposer.Left;
using Tools.Editor.AbilityComposer.Right;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Utils.log;
using UiFontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace Tools.Editor.AbilityComposer
{
    public sealed class AbilityComposerView : UIBaseEditor
    {
        private const string WINDOW_UXML_PATH = "Assets/Scripts/Tools/Editor/AbilityComposer/UI/Uxml/AbilityComposerWindow.uxml";
        private const string WINDOW_USS_PATH = "Assets/Scripts/Tools/Editor/AbilityComposer/UI/Uss/AbilityComposerWindow.uss";
        private const string MI_SANS_FONT_ASSET_PATH = "Assets/Fonts/miSans/MiSans-Regular-UI Toolkit.asset";

        private ObjectField m_previewSourceField;
        private ObjectField m_animationClipField;
        private Button m_returnPreviousSceneButton;
        private Button m_createPreviewButton;
        private Button m_focusPreviewButton;
        private VisualElement m_rootVisualElement;
        private AbilityComposerData m_composerData;
        private AbilityLeftView m_leftView;
        private AbilityCenterView m_centerView;
        private AbilityRightView m_rightView;
        private bool m_isControlsReady;

        public event Action<GameObject> OnPreviewSourceChanged;
        public event Action<AnimationClip> OnAnimationClipChanged;
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
        public event Action<AbilityEventCategory> OnEventCategoryChanged;
        public event Action<string> OnEventFunctionNameChanged;

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
            m_isControlsReady = true;
        }

        // 返回时间轴内容容器，供独立时间轴视图装配
        public bool TryGetTimelineElements(out ScrollView timelineScrollView, out VisualElement timelineContent)
        {
            return m_centerView.TryGetTimelineElements(out timelineScrollView, out timelineContent);
        }

        // 使用当前数据刷新播放控件与状态文字
        public void Refresh(AbilityTimelineData timelineData, bool hasPreview)
        {
            m_returnPreviousSceneButton.SetEnabled(hasPreview);
            m_leftView.Refresh(timelineData, hasPreview);
            m_rightView.Refresh(timelineData);
        }

        // 更新右侧 Event Inspector 的 Function 下拉候选
        public void SetEventFunctionChoices(IReadOnlyList<string> functionChoices)
        {
            m_rightView.SetEventFunctionChoices(functionChoices);
        }

        protected override void SubscribeViewEvents()
        {
            if (!m_isControlsReady)
                return;

            m_previewSourceField.RegisterValueChangedCallback(HandlePreviewSourceChanged);
            m_animationClipField.RegisterValueChangedCallback(HandleAnimationClipChanged);
            m_returnPreviousSceneButton.clicked += RequestReturnPreviousScene;
            m_createPreviewButton.clicked += RequestCreatePreview;
            m_focusPreviewButton.clicked += RequestFocusPreview;
            m_leftView.OnJumpFirstFrameRequested += RequestJumpFirstFrame;
            m_leftView.OnPreviousFrameRequested += RequestPreviousFrame;
            m_leftView.OnPlaybackToggled += RequestPlaybackToggle;
            m_leftView.OnNextFrameRequested += RequestNextFrame;
            m_leftView.OnJumpLastFrameRequested += RequestJumpLastFrame;
            m_leftView.OnCurrentFrameChanged += RequestCurrentFrameChanged;
            m_leftView.OnAddEventRequested += RequestAddEvent;
            m_leftView.OnDeleteSelectedEventRequested += RequestDeleteSelectedEvent;
            m_rightView.OnEventCategoryChanged += RequestEventCategoryChanged;
            m_rightView.OnEventFunctionNameChanged += RequestEventFunctionNameChanged;
        }

        protected override void UnsubscribeViewEvents()
        {
            if (!m_isControlsReady)
                return;

            m_previewSourceField.UnregisterValueChangedCallback(HandlePreviewSourceChanged);
            m_animationClipField.UnregisterValueChangedCallback(HandleAnimationClipChanged);
            m_returnPreviousSceneButton.clicked -= RequestReturnPreviousScene;
            m_createPreviewButton.clicked -= RequestCreatePreview;
            m_focusPreviewButton.clicked -= RequestFocusPreview;
            m_leftView.OnJumpFirstFrameRequested -= RequestJumpFirstFrame;
            m_leftView.OnPreviousFrameRequested -= RequestPreviousFrame;
            m_leftView.OnPlaybackToggled -= RequestPlaybackToggle;
            m_leftView.OnNextFrameRequested -= RequestNextFrame;
            m_leftView.OnJumpLastFrameRequested -= RequestJumpLastFrame;
            m_leftView.OnCurrentFrameChanged -= RequestCurrentFrameChanged;
            m_leftView.OnAddEventRequested -= RequestAddEvent;
            m_leftView.OnDeleteSelectedEventRequested -= RequestDeleteSelectedEvent;
            m_rightView.OnEventCategoryChanged -= RequestEventCategoryChanged;
            m_rightView.OnEventFunctionNameChanged -= RequestEventFunctionNameChanged;
            m_isControlsReady = false;
        }

        protected override void OnEditorDispose()
        {
            m_leftView?.Destroy();
            m_centerView?.Destroy();
            m_rightView?.Destroy();
        }

        // 查找 UXML 中的主视图控件
        private bool FindControls(VisualElement rootVisualElement)
        {
            m_previewSourceField = rootVisualElement.Q<ObjectField>("preview-source-field");
            m_animationClipField = rootVisualElement.Q<ObjectField>("animation-clip-field");
            m_returnPreviousSceneButton = rootVisualElement.Q<Button>("return-previous-scene-button");
            m_createPreviewButton = rootVisualElement.Q<Button>("create-preview-button");
            m_focusPreviewButton = rootVisualElement.Q<Button>("focus-preview-button");

            if (m_previewSourceField == null || m_animationClipField == null || m_returnPreviousSceneButton == null
                || m_createPreviewButton == null || m_focusPreviewButton == null)
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

        // 转发事件分类编辑请求
        private void RequestEventCategoryChanged(AbilityEventCategory category) => OnEventCategoryChanged?.Invoke(category);

        // 转发事件 Function 编辑请求
        private void RequestEventFunctionNameChanged(string functionName) => OnEventFunctionNameChanged?.Invoke(functionName);

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
