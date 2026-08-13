/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 业务控制器，协调时间轴数据与场景预览
 * │  类    名: AbilityComposerController.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.Editor.Controller;
using Tools.Editor.AbilityComposer.Center.Event;
using Tools.Editor.AbilityComposer.Center.Timeline;
using Tools.Editor.AbilityComposer.Preview;
using Tools.Editor.AbilityComposer.Right.Event;
using UnityEditor;
using UnityEngine;
using Utils.log;

namespace Tools.Editor.AbilityComposer
{
    public sealed class AbilityComposerController : BaseEditorController
    {
        private AbilityComposerData m_composerData;
        private AbilityTimelineData m_timelineData;
        private AbilityComposerView m_composerView;
        private AbilityTimelineView m_timelineView;
        private AbilityPreviewController m_previewController;
        private float m_playbackElapsedTime;
        private int m_playbackStartFrame;

        // 注入当前窗口的时间轴、预览与视图依赖
        public AbilityComposerController(AbilityComposerData composerData, AbilityComposerView composerView,
            AbilityTimelineView timelineView, AbilityPreviewController previewController)
        {
            m_composerData = composerData;
            m_composerView = composerView;
            m_timelineView = timelineView;
            m_previewController = previewController;
        }

        // 初始化 Composer 的数据、视图事件与时间轴
        protected override void OnEditorInit()
        {
            m_timelineData = new AbilityTimelineData();
            m_timelineData.SetAnimationClip(m_composerData.SelectedAnimationClip);
            LoadAnimationEvents();
            RefreshEventFunctionChoices();
            m_timelineView.SetTimelineData(m_timelineData);
            RefreshView();
        }

        // 释放 Composer 持有的编辑器播放状态
        protected override void OnEditorDispose()
        {
            m_timelineData.StopPlayback();
        }

        // 在程序集重载前退出预览并停止编辑器播放
        protected override void OnBeforeReload()
        {
            m_timelineData.StopPlayback();
            m_previewController.ReturnToPreviousScene();
        }

        // 使用编辑器 Tick 推进播放预览
        public override void Tick(float deltaTime)
        {
            UpdatePreviewPlayback(deltaTime);
        }

        // 注册各个 View 发送的用户操作意图
        protected override void RegisterModuleEvent()
        {
            RegisterEvent<GameObject>(
                callback => m_composerView.OnPreviewSourceChanged += callback,
                callback => m_composerView.OnPreviewSourceChanged -= callback,
                HandlePreviewSourceChanged);
            RegisterEvent<AnimationClip>(
                callback => m_composerView.OnAnimationClipChanged += callback,
                callback => m_composerView.OnAnimationClipChanged -= callback,
                HandleAnimationClipChanged);
            RegisterEvent(callback => m_composerView.OnCreatePreviewRequested += callback,
                callback => m_composerView.OnCreatePreviewRequested -= callback, CreatePreview);
            RegisterEvent(callback => m_composerView.OnFocusPreviewRequested += callback,
                callback => m_composerView.OnFocusPreviewRequested -= callback, FocusPreview);
            RegisterEvent(callback => m_composerView.OnReturnPreviousSceneRequested += callback,
                callback => m_composerView.OnReturnPreviousSceneRequested -= callback, ReturnToPreviousScene);
            RegisterEvent(callback => m_composerView.OnJumpFirstFrameRequested += callback,
                callback => m_composerView.OnJumpFirstFrameRequested -= callback, JumpToFirstFrame);
            RegisterEvent(callback => m_composerView.OnPreviousFrameRequested += callback,
                callback => m_composerView.OnPreviousFrameRequested -= callback, ShowPreviousFrame);
            RegisterEvent(callback => m_composerView.OnPlaybackToggled += callback,
                callback => m_composerView.OnPlaybackToggled -= callback, TogglePlayback);
            RegisterEvent(callback => m_composerView.OnNextFrameRequested += callback,
                callback => m_composerView.OnNextFrameRequested -= callback, ShowNextFrame);
            RegisterEvent(callback => m_composerView.OnJumpLastFrameRequested += callback,
                callback => m_composerView.OnJumpLastFrameRequested -= callback, JumpToLastFrame);
            RegisterEvent<int>(callback => m_timelineView.OnFrameRequested += callback,
                callback => m_timelineView.OnFrameRequested -= callback, HandleTimelineFrameRequested);
            RegisterEvent<int>(callback => m_composerView.OnCurrentFrameChanged += callback,
                callback => m_composerView.OnCurrentFrameChanged -= callback, HandleCurrentFrameChanged);
            RegisterEvent(callback => m_composerView.OnAddEventRequested += callback,
                callback => m_composerView.OnAddEventRequested -= callback, AddEvent);
            RegisterEvent(callback => m_composerView.OnDeleteSelectedEventRequested += callback,
                callback => m_composerView.OnDeleteSelectedEventRequested -= callback, DeleteSelectedEvent);
            RegisterEvent<AbilityEventCategory>(callback => m_composerView.OnEventCategoryChanged += callback,
                callback => m_composerView.OnEventCategoryChanged -= callback, HandleEventCategoryChanged);
            RegisterEvent<string>(callback => m_composerView.OnEventReceiverTypeNameChanged += callback,
                callback => m_composerView.OnEventReceiverTypeNameChanged -= callback, HandleEventReceiverTypeNameChanged);
            RegisterEvent<string>(callback => m_composerView.OnEventFunctionNameChanged += callback,
                callback => m_composerView.OnEventFunctionNameChanged -= callback, HandleEventFunctionNameChanged);
            RegisterEvent<string>(callback => m_timelineView.OnEventSelected += callback,
                callback => m_timelineView.OnEventSelected -= callback, HandleEventSelected);
            RegisterEvent(callback => m_composerView.OnApplyAnimationRequested += callback,
                callback => m_composerView.OnApplyAnimationRequested -= callback, ApplyAnimationEvents);
            RegisterEvent(callback => m_composerView.OnRestoreDraftRequested += callback,
                callback => m_composerView.OnRestoreDraftRequested -= callback, RestoreAnimationEvents);
        }

        // 更新预览来源并销毁旧的临时克隆
        private void HandlePreviewSourceChanged(GameObject previewPrefab)
        {
            m_composerData.SetPreviewSource(previewPrefab);
            m_timelineData.StopPlayback();
            m_previewController.ReturnToPreviousScene();
            RefreshEventFunctionChoices();
            RefreshView();
        }

        // 更新动画片段并重建时间轴刻度
        private void HandleAnimationClipChanged(AnimationClip animationClip)
        {
            m_previewController.ReturnToPreviousScene();
            ClearEventFunctionChoices();
            m_composerData.SetAnimationClip(animationClip);
            m_timelineData.SetAnimationClip(animationClip);
            LoadAnimationEvents();
            RefreshEventFunctionChoices();
            m_timelineView.SetTimelineData(m_timelineData);
            RefreshView();
        }

        // 根据当前数据创建临时预览对象
        private void CreatePreview()
        {
            if (m_composerData.PreviewSource == null)
            {
                QLog.Error("创建预览失败：请选择 Project 中的 Prefab 资源");
                return;
            }

            if (m_timelineData.Clip == null)
            {
                QLog.Error("创建预览失败：请选择 Animation Clip");
                return;
            }

            m_previewController.CreatePreview(m_composerData.PreviewSource, m_timelineData.Clip);
            RefreshEventFunctionChoices();
            SampleCurrentFrame(false);
            RefreshView();
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

        // 返回进入 AbilityPreview 前的场景集合
        public void ReturnToPreviousScene()
        {
            m_timelineData.StopPlayback();
            m_previewController.ReturnToPreviousScene();
            ClearEventFunctionChoices();
            RefreshView();
        }

        // 跳转到动画的第一帧
        private void JumpToFirstFrame()
        {
            SetCurrentFrame(0, true);
        }

        // 显示当前帧的前一帧
        private void ShowPreviousFrame()
        {
            SetCurrentFrame(m_timelineData.CurrentFrame - 1, true);
        }

        // 响应时间轴拖动并采样所请求的帧
        private void HandleTimelineFrameRequested(int frame)
        {
            SetCurrentFrame(frame, false);
        }

        // 响应帧输入框并跳转到指定帧
        private void HandleCurrentFrameChanged(int frame)
        {
            SetCurrentFrame(frame, true);
        }

        // 在当前帧创建空动画事件草稿
        private void AddEvent()
        {
            if (!m_timelineData.HasClip)
                return;

            RefreshEventFunctionChoices();
            m_timelineData.AddEvent(m_timelineData.CurrentFrame);
            RefreshView();
        }

        // 删除当前选中的动画事件草稿
        private void DeleteSelectedEvent()
        {
            m_timelineData.DeleteSelectedEvent();
            RefreshView();
        }

        // 选中时间轴上的事件标记
        private void HandleEventSelected(string eventId)
        {
            m_timelineData.SelectEvent(eventId);
            RefreshView();
        }

        // 更新选中事件的分类颜色
        private void HandleEventCategoryChanged(AbilityEventCategory category)
        {
            m_timelineData.SetSelectedEventCategory(category);
            RefreshView();
        }

        // 更新选中事件的接收类名称
        private void HandleEventReceiverTypeNameChanged(string receiverTypeName)
        {
            m_timelineData.SetSelectedEventReceiverTypeName(receiverTypeName);
            RefreshView();
        }

        // 更新选中事件的 Function 名称
        private void HandleEventFunctionNameChanged(string functionName)
        {
            m_timelineData.SetSelectedEventFunctionName(functionName);
            RefreshView();
        }

        // 将当前内存事件草稿写入独立 AnimationClip 资源
        private void ApplyAnimationEvents()
        {
            AnimationClip animationClip = m_timelineData.Clip;
            if (animationClip == null)
                return;

            string assetPath = AssetDatabase.GetAssetPath(animationClip);
            AnimationEvent[] animationEvents = CreateAnimationEvents();
            if (assetPath.EndsWith(".anim", System.StringComparison.OrdinalIgnoreCase))
            {
                Undo.RecordObject(animationClip, "应用 Ability Animation Events");
                AnimationUtility.SetAnimationEvents(animationClip, animationEvents);
                EditorUtility.SetDirty(animationClip);
                AssetDatabase.SaveAssets();
                return;
            }

            ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (modelImporter == null)
            {
                QLog.Error("当前 Animation Clip 不属于可写入的 .anim 或 ModelImporter 资源");
                return;
            }

            ModelImporterClipAnimation[] clipAnimations = modelImporter.clipAnimations;
            if (clipAnimations == null || clipAnimations.Length == 0)
                clipAnimations = modelImporter.defaultClipAnimations;

            int clipIndex = System.Array.FindIndex(clipAnimations, clip => clip.name == animationClip.name);
            if (clipIndex < 0)
            {
                QLog.Error($"ModelImporter 中未找到目标动画 Clip：{animationClip.name}");
                return;
            }

            clipAnimations[clipIndex].events = animationEvents;
            modelImporter.clipAnimations = clipAnimations;
            AssetDatabase.WriteImportSettingsIfDirty(assetPath);
            modelImporter.SaveAndReimport();
        }

        // 将内存事件草稿转换为 Unity AnimationEvent 数据
        private AnimationEvent[] CreateAnimationEvents()
        {
            AnimationEvent[] animationEvents = new AnimationEvent[m_timelineData.EventDraftValues.Count];
            for (int eventIndex = 0; eventIndex < m_timelineData.EventDraftValues.Count; eventIndex++)
            {
                AbilityEventDraft eventDraft = m_timelineData.EventDraftValues[eventIndex];
                animationEvents[eventIndex] = new AnimationEvent
                {
                    time = m_timelineData.FrameRate > 0f ? eventDraft.Frame / m_timelineData.FrameRate : 0f,
                    functionName = eventDraft.FunctionName
                };
            }

            return animationEvents;
        }

        // 从当前 AnimationClip 重新读取事件草稿
        private void RestoreAnimationEvents()
        {
            AnimationClip animationClip = m_timelineData.Clip;
            if (animationClip == null)
                return;

            m_timelineData.SetAnimationClip(animationClip);
            LoadAnimationEvents();
            RefreshView();
        }

        // 从当前 AnimationClip 读取已保存的 Animation Events
        private void LoadAnimationEvents()
        {
            if (m_timelineData.Clip == null)
                return;

            string assetPath = AssetDatabase.GetAssetPath(m_timelineData.Clip);
            if (assetPath.EndsWith(".anim", System.StringComparison.OrdinalIgnoreCase))
            {
                m_timelineData.LoadAnimationEvents(AnimationUtility.GetAnimationEvents(m_timelineData.Clip));
                return;
            }

            ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (modelImporter == null)
                return;

            ModelImporterClipAnimation[] clipAnimations = modelImporter.clipAnimations;
            if (clipAnimations == null || clipAnimations.Length == 0)
                clipAnimations = modelImporter.defaultClipAnimations;

            int clipIndex = System.Array.FindIndex(clipAnimations, clip => clip.name == m_timelineData.Clip.name);
            if (clipIndex >= 0)
                m_timelineData.LoadAnimationEvents(clipAnimations[clipIndex].events);
        }

        // 切换预览动画的播放与暂停状态
        private void TogglePlayback()
        {
            if (!m_timelineData.HasClip)
                return;

            if (!m_previewController.HasPreview)
                CreatePreview();

            if (!m_previewController.HasPreview)
                return;

            if (m_timelineData.IsPlaying)
            {
                m_timelineData.StopPlayback();
                RefreshView();
                return;
            }

            if (m_timelineData.CurrentFrame == m_timelineData.LastFrame)
            {
                m_timelineData.SetCurrentFrame(0);
                SampleCurrentFrame(true);
            }

            m_playbackStartFrame = m_timelineData.CurrentFrame;
            m_playbackElapsedTime = 0f;
            m_timelineData.StartPlayback();
            RefreshView();
        }

        // 显示当前帧的后一帧
        private void ShowNextFrame()
        {
            SetCurrentFrame(m_timelineData.CurrentFrame + 1, true);
        }

        // 跳转到动画的最后一帧
        private void JumpToLastFrame()
        {
            SetCurrentFrame(m_timelineData.LastFrame, true);
        }

        // 在播放状态下依据编辑器 Tick 推进播放头
        private void UpdatePreviewPlayback(float deltaTime)
        {
            if (m_timelineData == null || !m_timelineData.IsPlaying)
                return;

            m_playbackElapsedTime += deltaTime;
            int frameCount = m_timelineData.LastFrame + 1;
            if (frameCount <= 1 || m_timelineData.FrameRate <= 0f)
                return;

            int elapsedFrames = Mathf.FloorToInt(m_playbackElapsedTime * m_timelineData.FrameRate);
            int targetFrame = (m_playbackStartFrame + elapsedFrames) % frameCount;

            if (targetFrame != m_timelineData.CurrentFrame)
                SetCurrentFrame(targetFrame, false, false);
        }

        // 更新当前帧并同步采样预览对象
        private void SetCurrentFrame(int frame, bool scrollIntoView, bool stopPlayback = true)
        {
            if (!m_timelineData.HasClip)
                return;

            if (stopPlayback)
                m_timelineData.StopPlayback();

            m_timelineData.SetCurrentFrame(frame);
            SampleCurrentFrame(scrollIntoView);
            RefreshView(!m_timelineData.IsPlaying);
        }

        // 采样当前帧并按需将播放头滚动到可视范围
        private void SampleCurrentFrame(bool scrollIntoView)
        {
            if (m_previewController.HasPreview)
                m_previewController.SampleAnimation(m_timelineData.Clip, m_timelineData.CurrentTime);

            if (scrollIntoView)
                m_timelineView.ScrollFrameIntoView(m_timelineData.CurrentFrame);
        }

        // 刷新主视图文字、按钮状态与时间轴播放头
        private void RefreshView(bool refreshEventMarkers = true)
        {
            m_composerView.Refresh(m_timelineData, m_previewController.HasPreview);
            m_timelineView.RefreshCurrentFrame();
            if (refreshEventMarkers)
                m_timelineView.RefreshEventMarkers();
        }

        // 根据当前临时预览对象刷新可选的 Animation Event Function
        private void RefreshEventFunctionChoices()
        {
            GameObject animationEventReceiver = m_previewController.HasPreview
                ? m_previewController.AnimationEventReceiver
                : m_composerData.PreviewSource;
            m_composerView.SetEventFunctionChoices(
                AbilityEventFunctionResolver.Resolve(animationEventReceiver));
        }

        // 清空已销毁预览对象对应的 Function 候选
        private void ClearEventFunctionChoices()
        {
            m_composerView.SetEventFunctionChoices(new Dictionary<string, List<string>>());
        }
    }
}
