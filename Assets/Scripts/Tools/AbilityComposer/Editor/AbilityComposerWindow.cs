/*
 * ┌────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 主窗口，负责窗口生命周期与模块装配
 * │  类    名: AbilityComposerWindow.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────────────────────────┘
 */

using Module.Ability.Data;
using Module.Ability.Data.Window;
using Tools.AbilityComposer.Editor.Preview;
using Tools.AbilityComposer.Editor.View;
using Tools.AbilityComposer.Editor.View.Center.Timeline;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tools.AbilityComposer.Editor
{
    public sealed class AbilityComposerWindow : EditorWindow
    {
        private const string MENU_PATH = "工具/Ability/Ability Composer";
        private const string WINDOW_TITLE = "Ability Composer";

        [SerializeField] private AbilityComposerData ComposerData = new AbilityComposerData();

        private AbilityComposerController m_composerController;
        private AbilityComposerView m_composerView;
        private AbilityPreviewController m_previewController;
        private AbilityTimelineView m_timelineView;

        // 打开 Ability Composer 主窗口
        [MenuItem(MENU_PATH)]
        private static void OpenWindow()
        {
            AbilityComposerWindow window = GetWindow<AbilityComposerWindow>();
            AbilityWindowConfigSO requestedWindowConfig = AbilityComposerOpenRequest.ConsumeWindowConfig();
            AnimationClip requestedAnimationClip = AbilityComposerOpenRequest.ConsumeAnimationClip();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(1024f, 640f);
            window.Show();

            if (requestedWindowConfig != null)
            {
                window.ComposerData.SetWindowConfig(requestedWindowConfig);
                window.ComposerData.SetAnimationClip(requestedWindowConfig.AnimationClip);
                window.RebuildComposer();
            }
            else if (requestedAnimationClip != null)
            {
                window.ComposerData.SetAnimationClip(requestedAnimationClip);
                window.RebuildComposer();
            }
        }

        private void CreateGUI()
        {
            AbilityWindowConfigSO requestedWindowConfig = AbilityComposerOpenRequest.ConsumeWindowConfig();
            AnimationClip requestedAnimationClip = AbilityComposerOpenRequest.ConsumeAnimationClip();
            if (requestedWindowConfig != null)
            {
                ComposerData.SetWindowConfig(requestedWindowConfig);
                ComposerData.SetAnimationClip(requestedWindowConfig.AnimationClip);
            }
            else if (requestedAnimationClip != null)
                ComposerData.SetAnimationClip(requestedAnimationClip);

            DisposeModules(false);
            rootVisualElement.Clear();
            m_composerView = new AbilityComposerView(rootVisualElement, ComposerData);
            m_composerView.Init();

            if (!m_composerView.TryGetTimelineElements(out ScrollView timelineScrollView, out VisualElement timelineContent))
                return;

            m_previewController ??= new AbilityPreviewController();
            m_timelineView = new AbilityTimelineView(timelineScrollView, timelineContent);
            m_timelineView.Init();

            m_composerController = new AbilityComposerController(ComposerData, m_composerView, m_timelineView, m_previewController);
            m_composerController.Init();
        }

        private void OnDisable()
        {
            DisposeModules(true);
        }

        // 使用新的动画片段重建 Composer 内容
        private void RebuildComposer()
        {
            CreateGUI();
        }

        // 释放已装配模块，并按需要退出临时预览场景
        private void DisposeModules(bool returnPreviousScene)
        {
            if (returnPreviousScene)
                m_composerController?.ReturnToPreviousScene();

            m_composerController?.Destroy();
            m_timelineView?.Destroy();
            m_composerView?.Destroy();
            m_composerController = null;
            m_composerView = null;
            m_timelineView = null;
        }
    }
}
