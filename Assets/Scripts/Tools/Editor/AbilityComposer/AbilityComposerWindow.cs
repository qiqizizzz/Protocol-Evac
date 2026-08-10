/*
 * ┌──────────────────────────────────┐
 * │  描    述: Ability Composer 主窗口，负责加载工具布局与配置静态编辑控件
 * │  类    名: AbilityComposerWindow.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using System.Linq;
using Tools.Editor.AbilityComposer.Preview;
using Tools.Editor.AbilityComposer.Timeline;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Utils.log;
using UiFontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace Tools.Editor.AbilityComposer
{
    public sealed class AbilityComposerWindow : EditorWindow
    {
        private const string MENU_PATH = "工具/Ability/Ability Composer";
        private const string WINDOW_TITLE = "Ability Composer";
        private const string WINDOW_UXML_PATH = "Assets/Scripts/Tools/Editor/AbilityComposer/UI/Uxml/AbilityComposerWindow.uxml";
        private const string WINDOW_USS_PATH = "Assets/Scripts/Tools/Editor/AbilityComposer/UI/Uss/AbilityComposerWindow.uss";
        private const string MI_SANS_FONT_ASSET_PATH = "Assets/Fonts/miSans/MiSans-Regular-UI Toolkit.asset";

        [SerializeField] private GameObject PreviewPrefab;
        [SerializeField] private AnimationClip AnimationClip;
        private AbilityPreviewController m_previewController;
        private AbilityPreviewData m_previewData = new AbilityPreviewData();
        private Button m_returnPreviousSceneButton;
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
        private AbilityTimelineView m_timelineView;
        private double m_playbackStartTime;
        private int m_playbackStartFrame;

        // 打开 Ability Composer 主窗口
        [MenuItem(MENU_PATH)]
        private static void OpenWindow()
        {
            AbilityComposerWindow window = GetWindow<AbilityComposerWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(1024f, 640f);
            window.Show();
        }

        private void CreateGUI()
        {
            m_timelineView?.Dispose();
            rootVisualElement.Clear();
            if (m_previewController == null)
                m_previewController = new AbilityPreviewController();
            if (m_previewData == null)
                m_previewData = new AbilityPreviewData();
            m_timelineView = new AbilityTimelineView();

            VisualTreeAsset windowVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WINDOW_UXML_PATH);
            if (windowVisualTree == null)
            {
                QLog.Error($"未找到窗口布局资源：{WINDOW_UXML_PATH}");
                CreateMissingAssetView("未找到 Ability Composer 的 UXML 布局资源");
                return;
            }

            windowVisualTree.CloneTree(rootVisualElement);

            StyleSheet windowStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(WINDOW_USS_PATH);
            if (windowStyleSheet == null)
            {
                QLog.Error($"未找到窗口样式资源：{WINDOW_USS_PATH}");
                CreateMissingAssetView("未找到 Ability Composer 的 USS 样式资源");
                return;
            }

            rootVisualElement.styleSheets.Add(windowStyleSheet);
            ConfigureStaticControls();
            ConfigureTimelineControls();
            ConfigurePreviewControls();
            RestorePreviewResources();
            ApplyMiSansFont();
            m_timelineView.SetPreviewData(m_previewData);
            RefreshPreviewFrameUI();
            EditorApplication.update -= UpdatePreviewPlayback;
            EditorApplication.update += UpdatePreviewPlayback;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdatePreviewPlayback;
            m_previewData?.StopPlayback();
            m_previewController?.ReturnToPreviousScene();
            m_timelineView?.Dispose();
        }

        // 配置 P0 阶段的预览 Prefab 与动画资源输入控件
        private void ConfigureStaticControls()
        {
            ObjectField previewSourceField = rootVisualElement.Q<ObjectField>("preview-source-field");
            if (previewSourceField == null)
            {
                QLog.Error("未找到预览对象输入控件：preview-source-field");
                return;
            }

            previewSourceField.objectType = typeof(GameObject);
            previewSourceField.allowSceneObjects = false;
            previewSourceField.tooltip = "仅支持 Project 中的 Prefab 资源";
            previewSourceField.SetValueWithoutNotify(PreviewPrefab);

            ObjectField animationClipField = rootVisualElement.Q<ObjectField>("animation-clip-field");
            if (animationClipField == null)
            {
                QLog.Error("未找到动画片段输入控件：animation-clip-field");
                return;
            }

            animationClipField.objectType = typeof(AnimationClip);
            animationClipField.allowSceneObjects = false;
            animationClipField.SetValueWithoutNotify(AnimationClip);
        }

        // 配置 P1 阶段的预览相关控件
        private void ConfigurePreviewControls()
        {
            ObjectField previewSourceField = rootVisualElement.Q<ObjectField>("preview-source-field");
            ObjectField animationClipField = rootVisualElement.Q<ObjectField>("animation-clip-field");
            m_returnPreviousSceneButton = rootVisualElement.Q<Button>("return-previous-scene-button");
            Button createPreviewButton = rootVisualElement.Q<Button>("create-preview-button");
            Button focusPreviewButton = rootVisualElement.Q<Button>("focus-preview-button");

            if (previewSourceField == null || animationClipField == null
                || m_returnPreviousSceneButton == null || createPreviewButton == null || focusPreviewButton == null)
            {
                QLog.Error("配置 Ability Composer 预览控件失败：缺少必要的 UXML 控件");
                return;
            }

            previewSourceField.RegisterValueChangedCallback(OnPreviewSourceChanged);
            animationClipField.RegisterValueChangedCallback(OnAnimationClipChanged);
            m_returnPreviousSceneButton.clicked += ReturnToPreviousScene;
            createPreviewButton.clicked += CreatePreview;
            focusPreviewButton.clicked += FocusPreview;
            RefreshPreviewControls();
        }

        // 配置 P2 阶段的播放控制与时间轴控件
        private void ConfigureTimelineControls()
        {
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
            ScrollView timelineScrollView = rootVisualElement.Q<ScrollView>("timeline-scroll-view");
            VisualElement timelineContent = rootVisualElement.Q<VisualElement>("timeline-content");

            if (m_jumpFirstFrameButton == null || m_previousFrameButton == null || m_playToggleButton == null
                || m_nextFrameButton == null || m_jumpLastFrameButton == null || m_playToggleLabel == null
                || m_frameCounterLabel == null || m_frameRateLabel == null || m_currentTimeLabel == null
                || m_currentFrameStatusLabel == null || timelineScrollView == null || timelineContent == null)
            {
                QLog.Error("配置 Ability Composer 时间轴失败：缺少必要的 UXML 控件");
                return;
            }

            m_jumpFirstFrameButton.clicked += JumpToFirstFrame;
            m_previousFrameButton.clicked += ShowPreviousFrame;
            m_playToggleButton.clicked += TogglePlayback;
            m_nextFrameButton.clicked += ShowNextFrame;
            m_jumpLastFrameButton.clicked += JumpToLastFrame;
            m_timelineView.Initialize(timelineScrollView, timelineContent);
            m_timelineView.OnFrameRequested += HandleTimelineFrameRequested;
        }

        // 预览来源改变时清理旧预览
        private void OnPreviewSourceChanged(ChangeEvent<Object> changeEvent)
        {
            GameObject previewPrefab = changeEvent.newValue as GameObject;
            if (changeEvent.newValue != null && !PrefabUtility.IsPartOfPrefabAsset(changeEvent.newValue))
            {
                QLog.Error("预览对象只支持 Project 中的 Prefab 资源");
                ((ObjectField)changeEvent.target).SetValueWithoutNotify(null);
                previewPrefab = null;
            }

            PreviewPrefab = previewPrefab;
            m_previewController.ClearPreview();
            m_previewData.StopPlayback();
            RefreshPreviewControls();
        }

        // 动画片段改变时清理旧预览
        private void OnAnimationClipChanged(ChangeEvent<Object> changeEvent)
        {
            m_previewController.ClearPreview();
            AnimationClip = changeEvent.newValue as AnimationClip;
            m_previewData.SetAnimationClip(AnimationClip);
            m_timelineView.SetPreviewData(m_previewData);
            RefreshPreviewFrameUI();
            RefreshPreviewControls();
        }

        // 根据当前输入创建临时预览
        private void CreatePreview()
        {
            ObjectField previewSourceField = rootVisualElement.Q<ObjectField>("preview-source-field");
            ObjectField animationClipField = rootVisualElement.Q<ObjectField>("animation-clip-field");

            GameObject previewSource = previewSourceField.value as GameObject;
            if (previewSource == null)
            {
                QLog.Error("创建预览失败：请选择 Project 中的 Prefab 资源");
                return;
            }

            if (!PrefabUtility.IsPartOfPrefabAsset(previewSource))
            {
                QLog.Error("创建预览失败：预览对象必须是 Project 中的 Prefab 资源");
                return;
            }

            AnimationClip animationClip = animationClipField.value as AnimationClip;
            if (animationClip == null)
            {
                QLog.Error("创建预览失败：请选择 Animation Clip");
                return;
            }

            m_previewController.CreatePreview(previewSource, animationClip);
            SampleCurrentFrame(false);
            RefreshPreviewControls();
        }

        // 创建当前预览后聚焦 Scene View
        private void FocusPreview()
        {
            if (!m_previewController.HasPreview)
                CreatePreview();

            if (!m_previewController.HasPreview)
                return;

            m_previewController.FocusPreview();
        }

        // 返回创建预览前的工作场景
        private void ReturnToPreviousScene()
        {
            m_previewData.StopPlayback();
            m_previewController.ReturnToPreviousScene();
            RefreshPreviewControls();
        }

        // 刷新预览相关按钮的可用状态
        private void RefreshPreviewControls()
        {
            m_returnPreviousSceneButton.SetEnabled(m_previewController.HasPreview);
            bool hasAnimationClip = m_previewData.HasClip;
            m_playToggleButton.SetEnabled(hasAnimationClip);
            m_jumpFirstFrameButton.SetEnabled(hasAnimationClip);
            m_previousFrameButton.SetEnabled(hasAnimationClip);
            m_nextFrameButton.SetEnabled(hasAnimationClip);
            m_jumpLastFrameButton.SetEnabled(hasAnimationClip);
        }

        // 在窗口重建后恢复已选择的资源与动画帧数据
        private void RestorePreviewResources()
        {
            if (AnimationClip != null)
                m_previewData.SetAnimationClip(AnimationClip);
        }

        // 跳转到动画的第一帧
        private void JumpToFirstFrame()
        {
            SetCurrentFrame(0, true);
        }

        // 显示当前帧的前一帧
        private void ShowPreviousFrame()
        {
            SetCurrentFrame(m_previewData.CurrentFrame - 1, true);
        }

        // 响应时间轴拖动并采样所请求的帧
        private void HandleTimelineFrameRequested(int frame)
        {
            SetCurrentFrame(frame, false);
        }

        // 切换预览动画的播放与暂停状态
        private void TogglePlayback()
        {
            if (!m_previewData.HasClip)
                return;

            if (!m_previewController.HasPreview)
                CreatePreview();

            if (!m_previewController.HasPreview)
                return;

            if (m_previewData.IsPlaying)
            {
                m_previewData.StopPlayback();
                RefreshPreviewFrameUI();
                return;
            }

            if (m_previewData.CurrentFrame == m_previewData.LastFrame)
            {
                m_previewData.SetCurrentFrame(0);
                SampleCurrentFrame(true);
            }

            m_playbackStartFrame = m_previewData.CurrentFrame;
            m_playbackStartTime = EditorApplication.timeSinceStartup;
            m_previewData.StartPlayback();
            RefreshPreviewFrameUI();
        }

        // 显示当前帧的后一帧
        private void ShowNextFrame()
        {
            SetCurrentFrame(m_previewData.CurrentFrame + 1, true);
        }

        // 跳转到动画的最后一帧
        private void JumpToLastFrame()
        {
            SetCurrentFrame(m_previewData.LastFrame, true);
        }

        // 在播放状态下依据 Editor 时间推进播放头
        private void UpdatePreviewPlayback()
        {
            if (m_previewData == null || !m_previewData.IsPlaying)
                return;

            double elapsedSeconds = EditorApplication.timeSinceStartup - m_playbackStartTime;
            int targetFrame = m_playbackStartFrame + Mathf.FloorToInt((float)elapsedSeconds * m_previewData.FrameRate);
            if (targetFrame >= m_previewData.LastFrame)
            {
                SetCurrentFrame(m_previewData.LastFrame, false);
                m_previewData.StopPlayback();
                RefreshPreviewFrameUI();
                return;
            }

            if (targetFrame != m_previewData.CurrentFrame)
                SetCurrentFrame(targetFrame, false, false);
        }

        // 更新当前帧并同步采样预览对象
        private void SetCurrentFrame(int frame, bool scrollIntoView, bool stopPlayback = true)
        {
            if (!m_previewData.HasClip)
                return;

            if (stopPlayback)
                m_previewData.StopPlayback();
            m_previewData.SetCurrentFrame(frame);
            SampleCurrentFrame(scrollIntoView);
            RefreshPreviewFrameUI();
        }

        // 采样当前帧并按需将播放头滚动到可视范围
        private void SampleCurrentFrame(bool scrollIntoView)
        {
            if (m_previewController.HasPreview)
                m_previewController.SampleAnimation(m_previewData.Clip, m_previewData.CurrentTime);

            if (scrollIntoView)
                m_timelineView.ScrollFrameIntoView(m_previewData.CurrentFrame);
        }

        // 刷新预览数据文字、播放按钮和播放头位置
        private void RefreshPreviewFrameUI()
        {
            if (m_frameCounterLabel == null)
                return;

            if (!m_previewData.HasClip)
            {
                m_frameCounterLabel.text = "-- / --";
                m_frameRateLabel.text = "--";
                m_currentTimeLabel.text = "--:--";
                m_currentFrameStatusLabel.text = "当前帧 --";
                m_playToggleLabel.text = "▶";
                RefreshPreviewControls();
                return;
            }

            m_frameCounterLabel.text = $"{m_previewData.CurrentFrame} / {m_previewData.LastFrame}";
            m_frameRateLabel.text = $"{m_previewData.FrameRate:0.##}";
            m_currentTimeLabel.text = $"{m_previewData.CurrentTime:0.000}s";
            m_currentFrameStatusLabel.text = $"当前帧 {m_previewData.CurrentFrame}";
            m_playToggleLabel.text = m_previewData.IsPlaying ? "Ⅱ" : "▶";
            m_timelineView.RefreshCurrentFrame();
            RefreshPreviewControls();
        }

        // 为窗口文字应用 MiSans，避免中文回退到系统粗体字体
        private void ApplyMiSansFont()
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
        private void CreateMissingAssetView(string message)
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
