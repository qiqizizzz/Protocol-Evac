/*
 * ┌──────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 业务控制器，协调时间轴数据与场景预览
 * │  类    名: AbilityComposerController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using System.IO;
using Framework.QTower.Editor.Controller;
using Module.Ability.Data.Window;
using Module.Ability.Data.Window.Hit;
using Module.Ability.Data.Window.StepAdvance;
using Tools.AbilityComposer.Editor.Preview;
using Tools.AbilityComposer.Editor.Selection;
using Tools.AbilityComposer.Editor.View;
using Tools.AbilityComposer.Editor.View.Center.Event;
using Tools.AbilityComposer.Editor.View.Center.Timeline;
using Tools.AbilityComposer.Editor.View.Right.Event;
using UnityEditor;
using UnityEngine;
using Utils.log;

namespace Tools.AbilityComposer.Editor
{
    public sealed class AbilityComposerController : BaseEditorController
    {
        private AbilityComposerData m_composerData;
        private AbilityTimelineData m_timelineData;
        private AbilityComposerView m_composerView;
        private AbilityTimelineView m_timelineView;
        private AbilityPreviewController m_previewController;
        private readonly AbilityAnimationClipResolver m_animationClipResolver;
        private float m_playbackElapsedTime;
        private int m_playbackStartFrame;
        private int m_lastSaveUndoGroup = -1;

        // 注入当前窗口的时间轴、预览与视图依赖
        public AbilityComposerController(AbilityComposerData composerData, AbilityComposerView composerView,
            AbilityTimelineView timelineView, AbilityPreviewController previewController)
        {
            m_composerData = composerData;
            m_composerView = composerView;
            m_timelineView = timelineView;
            m_previewController = previewController;
            m_animationClipResolver = new AbilityAnimationClipResolver();
        }

        // 初始化 Composer 的数据、视图事件与时间轴
        protected override void OnEditorInit()
        {
            m_timelineData = new AbilityTimelineData();
            m_timelineData.SetAnimationClip(m_composerData.SelectedAnimationClip);
            SetWindowTracksForClip(m_composerData.SelectedAnimationClip);
            LoadAnimationEvents();
            LoadWindowTrack();
            RefreshEventFunctionChoices();
            m_timelineView.SetTimelineData(m_timelineData);
            RefreshPrefabAnimationClips(true);
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
            RegisterEvent<bool>(
                callback => m_composerView.OnShowGlobalAnimationsChanged += callback,
                callback => m_composerView.OnShowGlobalAnimationsChanged -= callback,
                HandleShowGlobalAnimationsChanged);
            RegisterEvent<AbilityWindowTrackBaseSO>(
                callback => m_composerView.OnWindowTrackChanged += callback,
                callback => m_composerView.OnWindowTrackChanged -= callback,
                HandleWindowTrackChanged);
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
            RegisterEvent(callback => m_composerView.OnAddWindowRequested += callback,
                callback => m_composerView.OnAddWindowRequested -= callback, AddWindow);
            RegisterEvent(callback => m_composerView.OnDeleteSelectedWindowRequested += callback,
                callback => m_composerView.OnDeleteSelectedWindowRequested -= callback, DeleteSelectedWindow);
            RegisterEvent<bool>(callback => m_composerView.OnHitWindowTrackToggled += callback,
                callback => m_composerView.OnHitWindowTrackToggled -= callback, HandleHitWindowTrackToggled);
            RegisterEvent<bool>(callback => m_composerView.OnStepAdvanceWindowTrackToggled += callback,
                callback => m_composerView.OnStepAdvanceWindowTrackToggled -= callback, HandleStepAdvanceWindowTrackToggled);
            RegisterEvent<AbilityEventCategory>(callback => m_composerView.OnEventCategoryChanged += callback,
                callback => m_composerView.OnEventCategoryChanged -= callback, HandleEventCategoryChanged);
            RegisterEvent<string>(callback => m_composerView.OnEventReceiverTypeNameChanged += callback,
                callback => m_composerView.OnEventReceiverTypeNameChanged -= callback, HandleEventReceiverTypeNameChanged);
            RegisterEvent<string>(callback => m_composerView.OnEventFunctionNameChanged += callback,
                callback => m_composerView.OnEventFunctionNameChanged -= callback, HandleEventFunctionNameChanged);
            RegisterEvent<string>(callback => m_timelineView.OnEventSelected += callback,
                callback => m_timelineView.OnEventSelected -= callback, HandleEventSelected);
            RegisterEvent<string>(callback => m_timelineView.OnWindowSelected += callback,
                callback => m_timelineView.OnWindowSelected -= callback, HandleWindowSelected);
            RegisterEvent<AbilityTimelineView.WindowFrameRangeRequest>(callback => m_timelineView.OnWindowFrameRangeChanged += callback,
                callback => m_timelineView.OnWindowFrameRangeChanged -= callback, HandleWindowFrameRangeChanged);
            RegisterEvent<AbilityTimelineView.EventMoveRequest>(callback => m_timelineView.OnEventMoved += callback,
                callback => m_timelineView.OnEventMoved -= callback, HandleEventMoved);
            RegisterEvent<AbilityWindowDraftType>(callback => m_composerView.OnWindowTypeChanged += callback,
                callback => m_composerView.OnWindowTypeChanged -= callback, HandleWindowTypeChanged);
            RegisterEvent<int, int>(callback => m_composerView.OnWindowFramesChanged += callback,
                callback => m_composerView.OnWindowFramesChanged -= callback, HandleWindowFramesChanged);
            RegisterEvent<float>(callback => m_composerView.OnWindowDamageChanged += callback,
                callback => m_composerView.OnWindowDamageChanged -= callback, HandleWindowDamageChanged);
            RegisterEvent(callback => m_composerView.OnSaveWindowRequested += callback,
                callback => m_composerView.OnSaveWindowRequested -= callback, SaveWindowTrack);
            RegisterEvent(callback => m_composerView.OnSaveEventRequested += callback,
                callback => m_composerView.OnSaveEventRequested -= callback, ApplyAnimationEvents);
            RegisterEvent(callback => m_composerView.OnCloseEventInspectorRequested += callback,
                callback => m_composerView.OnCloseEventInspectorRequested -= callback, CloseEventInspector);
            RegisterEvent(callback => m_composerView.OnCloseWindowInspectorRequested += callback,
                callback => m_composerView.OnCloseWindowInspectorRequested -= callback, CloseWindowInspector);
            RegisterEvent(callback => m_composerView.OnSaveAllRequested += callback,
                callback => m_composerView.OnSaveAllRequested -= callback, SaveAll);
            RegisterEvent(callback => m_composerView.OnUndoLastSaveRequested += callback,
                callback => m_composerView.OnUndoLastSaveRequested -= callback, UndoLastSave);
        }

        // 更新预览来源并销毁旧的临时克隆
        private void HandlePreviewSourceChanged(GameObject previewPrefab)
        {
            m_composerData.SetPreviewSource(previewPrefab);
            m_timelineData.StopPlayback();
            m_previewController.ReturnToPreviousScene();
            RefreshPrefabAnimationClips(false);
            RefreshEventFunctionChoices();
            RefreshView();
        }

        // 更新动画片段并重建时间轴刻度
        private void HandleAnimationClipChanged(AnimationClip animationClip)
        {
            m_previewController.ReturnToPreviousScene();
            ClearEventFunctionChoices();
            m_composerData.SetAnimationClip(animationClip);
            m_composerView.SetSelectedAnimationClip(animationClip);
            SetWindowTracksForClip(animationClip);
            m_timelineData.SetAnimationClip(animationClip);
            LoadAnimationEvents();
            LoadWindowTrack();
            RefreshEventFunctionChoices();
            m_timelineView.SetTimelineData(m_timelineData);
            RefreshView();
        }

        // 切换全局动画选择模式并刷新预制体动画候选
        private void HandleShowGlobalAnimationsChanged(bool showGlobalAnimations)
        {
            m_composerData.SetShowGlobalAnimations(showGlobalAnimations);
            m_composerView.SetShowGlobalAnimations(showGlobalAnimations);
            if (!showGlobalAnimations)
                RefreshPrefabAnimationClips(false);
        }

        // 切换当前编辑的通用窗口轨道资产
        private void HandleWindowTrackChanged(AbilityWindowTrackBaseSO windowTrack)
        {
            if (m_timelineData.SelectedWindow != null && m_timelineData.SelectedWindow.Type == AbilityWindowDraftType.StepAdvance)
            {
                AbilityStepAdvanceWindowTrackSO stepAdvanceTrack = windowTrack as AbilityStepAdvanceWindowTrackSO;
                if (stepAdvanceTrack == null)
                {
                    QLog.Error("技能推进窗口必须绑定 AbilityStepAdvanceWindowTrackSO");
                    return;
                }

                m_composerData.SetWindowTracks(m_composerData.SelectedHitWindowTrack, stepAdvanceTrack);
            }
            else
            {
                AbilityHitWindowTrackSO hitTrack = windowTrack as AbilityHitWindowTrackSO;
                if (hitTrack == null)
                {
                    QLog.Error("命中窗口必须绑定 AbilityHitWindowTrackSO");
                    return;
                }

                m_composerData.SetWindowTracks(hitTrack, m_composerData.SelectedStepAdvanceWindowTrack);
            }
            LoadWindowTrack();
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

        // 刷新拖动后的动画事件位置
        private void HandleEventMoved(AbilityTimelineView.EventMoveRequest request)
        {
            m_timelineData.SetEventFrame(request.EventId, request.Frame);
            RefreshView();
        }

        // 在当前帧创建一条通用窗口草稿
        private void AddWindow()
        {
            if (!m_timelineData.HasClip)
                return;

            m_timelineData.AddWindow(m_timelineData.CurrentFrame);
            RefreshView();
        }

        // 删除当前选中的窗口草稿
        private void DeleteSelectedWindow()
        {
            m_timelineData.DeleteSelectedWindow();
            RefreshView();
        }

        // 更新命中窗口轨道启用状态
        private void HandleHitWindowTrackToggled(bool isEnabled)
        {
            m_timelineData.SetHitWindowTrackEnabled(isEnabled);
            RefreshView();
        }

        // 更新技能推进窗口轨道启用状态
        private void HandleStepAdvanceWindowTrackToggled(bool isEnabled)
        {
            m_timelineData.SetStepAdvanceWindowTrackEnabled(isEnabled);
            RefreshView();
        }

        // 选中时间轴上的窗口区间
        private void HandleWindowSelected(string windowId)
        {
            m_timelineData.SelectWindow(windowId);
            RefreshView();
        }

        // 关闭事件检查器并取消事件选中
        private void CloseEventInspector()
        {
            m_timelineData.ClearEventSelection();
            RefreshView();
        }

        // 关闭窗口检查器并取消窗口选中
        private void CloseWindowInspector()
        {
            m_timelineData.ClearWindowSelection();
            RefreshView();
        }

        // 提交时间轴拖动后的窗口帧范围
        private void HandleWindowFrameRangeChanged(AbilityTimelineView.WindowFrameRangeRequest request)
        {
            m_timelineData.SelectWindow(request.WindowId);
            m_timelineData.SetSelectedWindowFrames(request.StartFrame, request.EndFrame);
            RefreshView();
        }

        // 更新选中窗口的业务类型
        private void HandleWindowTypeChanged(AbilityWindowDraftType type)
        {
            m_timelineData.SetSelectedWindowType(type);
            RefreshView();
        }

        // 更新选中窗口的帧范围
        private void HandleWindowFramesChanged(int startFrame, int endFrame)
        {
            m_timelineData.SetSelectedWindowFrames(startFrame, endFrame);
            RefreshView();
        }

        // 更新选中命中窗口的伤害参数
        private void HandleWindowDamageChanged(float damage)
        {
            m_timelineData.SetSelectedWindowDamage(damage);
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
            ApplyAnimationEvents(true);
            RefreshView();
        }

        // 将当前事件草稿写入动画资源，并按需创建独立 Undo 组
        private bool ApplyAnimationEvents(bool createUndoGroup)
        {
            AnimationClip animationClip = m_timelineData.Clip;
            if (animationClip == null)
                return false;

            string assetPath = AssetDatabase.GetAssetPath(animationClip);
            AnimationEvent[] animationEvents = CreateAnimationEvents();
            if (assetPath.EndsWith(".anim", System.StringComparison.OrdinalIgnoreCase))
            {
                if (createUndoGroup)
                    BeginSaveUndoGroup("保存 Ability Animation Events");

                Undo.RecordObject(animationClip, "保存 Ability Animation Events");
                AnimationUtility.SetAnimationEvents(animationClip, animationEvents);
                EditorUtility.SetDirty(animationClip);
                AssetDatabase.SaveAssets();
                if (createUndoGroup)
                    CompleteSaveUndoGroup();

                return true;
            }

            ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (modelImporter == null)
            {
                QLog.Error("当前 Animation Clip 不属于可写入的 .anim 或 ModelImporter 资源");
                return false;
            }

            ModelImporterClipAnimation[] clipAnimations = modelImporter.clipAnimations;
            if (clipAnimations == null || clipAnimations.Length == 0)
                clipAnimations = modelImporter.defaultClipAnimations;

            int clipIndex = System.Array.FindIndex(clipAnimations, clip => clip.name == animationClip.name);
            if (clipIndex < 0)
            {
                QLog.Error($"ModelImporter 中未找到目标动画 Clip：{animationClip.name}");
                return false;
            }

            if (createUndoGroup)
                BeginSaveUndoGroup("保存 Ability Animation Events");

            Undo.RecordObject(modelImporter, "保存 Ability Animation Events");
            clipAnimations[clipIndex].events = animationEvents;
            modelImporter.clipAnimations = clipAnimations;
            AssetDatabase.WriteImportSettingsIfDirty(assetPath);
            modelImporter.SaveAndReimport();
            if (createUndoGroup)
                CompleteSaveUndoGroup();

            return true;
        }

        // 一次提交当前事件和窗口两类草稿
        private void SaveAll()
        {
            BeginSaveUndoGroup("一键保存 Ability Composer");
            bool savedEvents = ApplyAnimationEvents(false);
            bool savedWindows = SaveWindowTrack(false);
            if (savedEvents || savedWindows)
                CompleteSaveUndoGroup();
            else
                m_lastSaveUndoGroup = -1;

            RefreshView();
        }

        // 仅撤销 Composer 最近一次保存并重载资产草稿
        private void UndoLastSave()
        {
            if (m_lastSaveUndoGroup < 0)
                return;

            Undo.RevertAllDownToGroup(m_lastSaveUndoGroup);
            m_lastSaveUndoGroup = -1;
            RestoreAnimationEvents();
            LoadWindowTrack();
            RefreshView();
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
            m_composerView.Refresh(m_timelineData, m_previewController.HasPreview, true, m_lastSaveUndoGroup >= 0);
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

        // 从选中的窗口轨道资产加载草稿到时间轴
        private void LoadWindowTrack()
        {
            m_timelineData.ClearWindows();
            AbilityHitWindowTrackSO hitTrack = m_composerData.SelectedHitWindowTrack;
            AbilityStepAdvanceWindowTrackSO stepAdvanceTrack = m_composerData.SelectedStepAdvanceWindowTrack;
            m_timelineData.SetWindowTracks(hitTrack, stepAdvanceTrack);
            if (!m_timelineData.HasClip)
                return;

            if (hitTrack != null && hitTrack.AnimationClip != null && hitTrack.AnimationClip != m_timelineData.Clip)
            {
                QLog.Error("命中窗口轨道绑定的 Animation Clip 与当前时间轴动画不一致");
                return;
            }

            if (hitTrack != null)
            foreach (AbilityHitWindowData windowData in hitTrack.Windows)
            {
                int startFrame = Mathf.RoundToInt(windowData.StartNormalizedTime * m_timelineData.LastFrame);
                int endFrame = Mathf.RoundToInt(windowData.EndNormalizedTime * m_timelineData.LastFrame);
                AbilityWindowDraft windowDraft = m_timelineData.AddWindow(AbilityWindowDraftType.Hit, startFrame, endFrame, windowData.Damage);
                windowDraft.SetId(windowData.Id);
            }

            if (stepAdvanceTrack != null)
            foreach (AbilityStepAdvanceWindowData windowData in stepAdvanceTrack.Windows)
            {
                int startFrame = Mathf.RoundToInt(windowData.StartNormalizedTime * m_timelineData.LastFrame);
                int endFrame = Mathf.RoundToInt(windowData.EndNormalizedTime * m_timelineData.LastFrame);
                AbilityWindowDraft windowDraft = m_timelineData.AddWindow(AbilityWindowDraftType.StepAdvance, startFrame, endFrame, 0f);
                windowDraft.SetId(windowData.Id);
            }

            m_timelineData.ClearWindowSelection();
        }

        // 查找当前动画唯一绑定的窗口轨道资产
        private AbilityHitWindowTrackSO FindWindowTrack(AnimationClip animationClip)
        {
            if (animationClip == null)
                return null;

            string[] windowTrackGuids = AssetDatabase.FindAssets("t:AbilityHitWindowTrackSO");
            foreach (string windowTrackGuid in windowTrackGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(windowTrackGuid);
                AbilityHitWindowTrackSO windowTrack = AssetDatabase.LoadAssetAtPath<AbilityHitWindowTrackSO>(assetPath);
                if (windowTrack.AnimationClip == animationClip)
                    return windowTrack;
            }

            return null;
        }

        // 查找当前动画绑定的命中与技能推进窗口轨道
        private void SetWindowTracksForClip(AnimationClip animationClip)
        {
            m_composerData.SetWindowTracks(FindWindowTrack(animationClip), FindStepAdvanceWindowTrack(animationClip));
        }

        // 查找当前动画唯一绑定的技能推进窗口轨道资产
        private AbilityStepAdvanceWindowTrackSO FindStepAdvanceWindowTrack(AnimationClip animationClip)
        {
            if (animationClip == null)
                return null;

            string[] trackGuids = AssetDatabase.FindAssets("t:AbilityStepAdvanceWindowTrackSO");
            foreach (string trackGuid in trackGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(trackGuid);
                AbilityStepAdvanceWindowTrackSO track = AssetDatabase.LoadAssetAtPath<AbilityStepAdvanceWindowTrackSO>(assetPath);
                if (track != null && track.AnimationClip == animationClip)
                    return track;
            }

            return null;
        }

        // 将时间轴窗口草稿写回选中的窗口轨道资产
        private void SaveWindowTrack()
        {
            SaveWindowTrack(true);
            RefreshView();
        }

        // 将当前窗口草稿写回窗口轨道资产，并按需创建独立 Undo 组
        private bool SaveWindowTrack(bool createUndoGroup)
        {
            AbilityHitWindowTrackSO windowTrack = m_composerData.SelectedHitWindowTrack;
            if (!m_timelineData.HasClip)
                return false;

            if (windowTrack == null)
            {
                windowTrack = CreateWindowTrack();
                if (windowTrack == null)
                    return false;
            }

            List<AbilityHitWindowData> windowValues = new List<AbilityHitWindowData>();
            foreach (AbilityWindowDraft windowDraft in m_timelineData.HitWindowDraftValues)
            {
                float startNormalizedTime = m_timelineData.LastFrame > 0
                    ? windowDraft.StartFrame / (float)m_timelineData.LastFrame
                    : 0f;
                float endNormalizedTime = m_timelineData.LastFrame > 0
                    ? windowDraft.EndFrame / (float)m_timelineData.LastFrame
                    : 0f;
                windowValues.Add(new AbilityHitWindowData(startNormalizedTime, endNormalizedTime, windowDraft.Damage));
            }

            if (createUndoGroup)
                BeginSaveUndoGroup("保存 Ability 窗口轨道");

            Undo.RecordObject(windowTrack, "保存 Ability 窗口轨道");
            windowTrack.SetAnimationClip(m_timelineData.Clip);
            windowTrack.SetWindows(windowValues);
            EditorUtility.SetDirty(windowTrack);
            AssetDatabase.SaveAssets();
            if (createUndoGroup)
                CompleteSaveUndoGroup();

            SaveStepAdvanceWindowTrack(createUndoGroup);
            return true;
        }

        // 将当前技能推进窗口草稿写回对应轨道资产
        private bool SaveStepAdvanceWindowTrack(bool createUndoGroup)
        {
            if (!m_timelineData.HasClip)
                return false;

            AbilityStepAdvanceWindowTrackSO track = m_composerData.SelectedStepAdvanceWindowTrack;
            if (track == null)
            {
                string clipName = CreateSafeAssetFileName(m_timelineData.Clip.name);
                string defaultName = $"{clipName}_StepAdvanceWindowTrack.asset";
                string assetPath = EditorUtility.SaveFilePanelInProject("新建技能推进窗口轨道", defaultName, "asset", "选择窗口轨道的保存位置");
                if (string.IsNullOrEmpty(assetPath))
                    return false;

                track = ScriptableObject.CreateInstance<AbilityStepAdvanceWindowTrackSO>();
                AssetDatabase.CreateAsset(track, assetPath);
                m_composerData.SetWindowTracks(m_composerData.SelectedHitWindowTrack, track);
            }

            List<AbilityStepAdvanceWindowData> values = new List<AbilityStepAdvanceWindowData>();
            foreach (AbilityWindowDraft draft in m_timelineData.StepAdvanceWindowDraftValues)
            {
                float start = m_timelineData.LastFrame > 0 ? draft.StartFrame / (float)m_timelineData.LastFrame : 0f;
                float end = m_timelineData.LastFrame > 0 ? draft.EndFrame / (float)m_timelineData.LastFrame : 0f;
                values.Add(new AbilityStepAdvanceWindowData(draft.Id, start, end));
            }

            Undo.RecordObject(track, "保存技能推进窗口轨道");
            track.SetAnimationClip(m_timelineData.Clip);
            track.SetWindows(values);
            EditorUtility.SetDirty(track);
            AssetDatabase.SaveAssets();
            return true;
        }

        // 让用户指定路径创建当前动画专属的窗口轨道资产
        private AbilityHitWindowTrackSO CreateWindowTrack()
        {
            string clipName = CreateSafeAssetFileName(m_timelineData.Clip.name);
            string defaultName = $"{clipName}_WindowTrack.asset";
            string assetPath = EditorUtility.SaveFilePanelInProject("新建 Ability 窗口轨道", defaultName, "asset", "选择窗口轨道的保存位置");
            if (string.IsNullOrEmpty(assetPath))
                return null;

            AbilityHitWindowTrackSO windowTrack = ScriptableObject.CreateInstance<AbilityHitWindowTrackSO>();
            AssetDatabase.CreateAsset(windowTrack, assetPath);
            AssetDatabase.SaveAssets();
            m_composerData.SetWindowTracks(windowTrack, m_composerData.SelectedStepAdvanceWindowTrack);
            m_timelineData.SetWindowTracks(windowTrack, m_composerData.SelectedStepAdvanceWindowTrack);
            return windowTrack;
        }

        // 创建可被底部撤销按钮定位的一次保存 Undo 组
        private void BeginSaveUndoGroup(string undoName)
        {
            if (m_lastSaveUndoGroup >= 0)
                Undo.CollapseUndoOperations(m_lastSaveUndoGroup);

            Undo.IncrementCurrentGroup();
            m_lastSaveUndoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
        }

        // 关闭当前保存 Undo 组，限制底部撤销只回退该组
        private void CompleteSaveUndoGroup()
        {
            Undo.CollapseUndoOperations(m_lastSaveUndoGroup);
        }

        // 刷新当前预制体依赖的动画候选并校正选择
        private void RefreshPrefabAnimationClips(bool includeCurrentAnimationClip)
        {
            IReadOnlyList<AnimationClip> resolvedClips = m_animationClipResolver.Resolve(m_composerData.PreviewSource);
            List<AnimationClip> animationClips = new List<AnimationClip>(resolvedClips);
            AnimationClip selectedAnimationClip = m_composerData.SelectedAnimationClip;
            bool shouldIncludeCurrent = includeCurrentAnimationClip || m_composerData.PreviewSource == null;
            if (shouldIncludeCurrent && selectedAnimationClip != null && !animationClips.Contains(selectedAnimationClip))
                animationClips.Insert(0, selectedAnimationClip);

            if (!m_composerData.IsShowingGlobalAnimations && !animationClips.Contains(selectedAnimationClip))
            {
                selectedAnimationClip = animationClips.Count > 0 ? animationClips[0] : null;
                HandleAnimationClipChanged(selectedAnimationClip);
            }

            m_composerView.SetPrefabAnimationClips(animationClips, selectedAnimationClip);
        }

        // 将动画名称转换为可用于 Unity 资产路径的文件名
        private static string CreateSafeAssetFileName(string fileName)
        {
            char[] fileNameCharacters = fileName.ToCharArray();
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            for (int characterIndex = 0; characterIndex < fileNameCharacters.Length; characterIndex++)
            {
                if (Array.IndexOf(invalidCharacters, fileNameCharacters[characterIndex]) >= 0)
                    fileNameCharacters[characterIndex] = '_';
            }

            return new string(fileNameCharacters);
        }

    }
}
