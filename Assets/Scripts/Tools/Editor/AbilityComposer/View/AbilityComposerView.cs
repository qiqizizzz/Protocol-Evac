/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 主视图，管理资源输入、播放控件与状态栏
 * │  类    名: AbilityComposerView.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.QTower.Editor.View;
using Tools.Editor.AbilityComposer.Timeline;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Utils.log;
using UiFontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace Tools.Editor.AbilityComposer.View
{
    public sealed class AbilityComposerView : UIBaseEditor
    {
        private const string WINDOW_UXML_PATH = "Assets/Scripts/Tools/Editor/AbilityComposer/UI/Uxml/AbilityComposerWindow.uxml";
        private const string WINDOW_USS_PATH = "Assets/Scripts/Tools/Editor/AbilityComposer/UI/Uss/AbilityComposerWindow.uss";
        private const string MI_SANS_FONT_ASSET_PATH = "Assets/Fonts/miSans/MiSans-Regular-UI Toolkit.asset";

        private ObjectField m_previewSourceField;
        private ObjectField m_animationClipField;
        private ScrollView m_timelineScrollView;
        private VisualElement m_timelineContent;
        private Button m_returnPreviousSceneButton;
        private Button m_createPreviewButton;
        private Button m_focusPreviewButton;
        private Button m_jumpFirstFrameButton;
        private Button m_previousFrameButton;
        private Button m_playToggleButton;
        private Button m_nextFrameButton;
        private Button m_jumpLastFrameButton;
        private Label m_playToggleLabel;
        private Label m_frameCounterLabel;
        private Label m_frameRateLabel;
        private Label m_currentTimeLabel;
        private Label m_currentFrameStatusLabel;
        private VisualElement m_rootVisualElement;
        private AbilityComposerData m_composerData;
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

            ConfigureResourceFields(m_composerData);
            ApplyMiSansFont(m_rootVisualElement);
            m_isControlsReady = true;
        }

        // 返回时间轴内容容器，供独立时间轴视图装配
        public bool TryGetTimelineElements(out ScrollView timelineScrollView, out VisualElement timelineContent)
        {
            timelineScrollView = m_timelineScrollView;
            timelineContent = m_timelineContent;
            if (timelineScrollView != null && timelineContent != null)
                return true;

            QLog.Error("配置 Ability Composer 时间轴失败：缺少必要的 UXML 控件");
            return false;
        }

        // 使用当前数据刷新播放控件与状态文字
        public void Refresh(AbilityTimelineData timelineData, bool hasPreview)
        {
            bool hasAnimationClip = timelineData.HasClip;
            m_returnPreviousSceneButton.SetEnabled(hasPreview);
            m_playToggleButton.SetEnabled(hasAnimationClip);
            m_jumpFirstFrameButton.SetEnabled(hasAnimationClip);
            m_previousFrameButton.SetEnabled(hasAnimationClip);
            m_nextFrameButton.SetEnabled(hasAnimationClip);
            m_jumpLastFrameButton.SetEnabled(hasAnimationClip);

            if (!hasAnimationClip)
            {
                m_frameCounterLabel.text = "-- / --";
                m_frameRateLabel.text = "--";
                m_currentTimeLabel.text = "--:--";
                m_currentFrameStatusLabel.text = "当前帧 --";
                m_playToggleLabel.text = "▶";
                return;
            }

            m_frameCounterLabel.text = $"{timelineData.CurrentFrame} / {timelineData.LastFrame}";
            m_frameRateLabel.text = $"{timelineData.FrameRate:0.##}";
            m_currentTimeLabel.text = $"{timelineData.CurrentTime:0.000}s";
            m_currentFrameStatusLabel.text = $"当前帧 {timelineData.CurrentFrame}";
            m_playToggleLabel.text = timelineData.IsPlaying ? "Ⅱ" : "▶";
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
            m_jumpFirstFrameButton.clicked += RequestJumpFirstFrame;
            m_previousFrameButton.clicked += RequestPreviousFrame;
            m_playToggleButton.clicked += RequestPlaybackToggle;
            m_nextFrameButton.clicked += RequestNextFrame;
            m_jumpLastFrameButton.clicked += RequestJumpLastFrame;
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
            m_jumpFirstFrameButton.clicked -= RequestJumpFirstFrame;
            m_previousFrameButton.clicked -= RequestPreviousFrame;
            m_playToggleButton.clicked -= RequestPlaybackToggle;
            m_nextFrameButton.clicked -= RequestNextFrame;
            m_jumpLastFrameButton.clicked -= RequestJumpLastFrame;
            m_isControlsReady = false;
        }

        // 查找 UXML 中的主视图控件
        private bool FindControls(VisualElement rootVisualElement)
        {
            m_previewSourceField = rootVisualElement.Q<ObjectField>("preview-source-field");
            m_animationClipField = rootVisualElement.Q<ObjectField>("animation-clip-field");
            m_timelineScrollView = rootVisualElement.Q<ScrollView>("timeline-scroll-view");
            m_timelineContent = rootVisualElement.Q<VisualElement>("timeline-content");
            m_returnPreviousSceneButton = rootVisualElement.Q<Button>("return-previous-scene-button");
            m_createPreviewButton = rootVisualElement.Q<Button>("create-preview-button");
            m_focusPreviewButton = rootVisualElement.Q<Button>("focus-preview-button");
            m_jumpFirstFrameButton = rootVisualElement.Q<Button>("jump-first-frame-button");
            m_previousFrameButton = rootVisualElement.Q<Button>("previous-frame-button");
            m_playToggleButton = rootVisualElement.Q<Button>("play-toggle-button");
            m_nextFrameButton = rootVisualElement.Q<Button>("next-frame-button");
            m_jumpLastFrameButton = rootVisualElement.Q<Button>("jump-last-frame-button");
            m_playToggleLabel = rootVisualElement.Q<Label>("play-toggle-label");
            m_frameCounterLabel = rootVisualElement.Q<Label>("preview-frame-counter");
            m_frameRateLabel = rootVisualElement.Q<Label>("preview-frame-rate");
            m_currentTimeLabel = rootVisualElement.Q<Label>("preview-current-time");
            m_currentFrameStatusLabel = rootVisualElement.Q<Label>("current-frame-status");

            if (m_previewSourceField == null || m_animationClipField == null || m_returnPreviousSceneButton == null
                || m_createPreviewButton == null || m_focusPreviewButton == null || m_jumpFirstFrameButton == null
                || m_previousFrameButton == null || m_playToggleButton == null || m_nextFrameButton == null
                || m_jumpLastFrameButton == null || m_playToggleLabel == null || m_frameCounterLabel == null
                || m_frameRateLabel == null || m_currentTimeLabel == null || m_currentFrameStatusLabel == null)
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
