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
using Module.Ability.Data.Window.Audio;
using Module.Ability.Data.Window.Hit;
using Module.Ability.Data.Window.MovementLock;
using Module.Ability.Data.Window.StepAdvance;
using Module.Ability.Data.Window.Vfx;
using Module.Ability.Vfx;
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
        private enum ComposerUndoEntryType
        {
            EventDraft,
            SavedAsset
        }

        private readonly struct ComposerUndoEntry
        {
            public readonly ComposerUndoEntryType Type;
            public readonly List<AbilityEventDraft> EventDrafts;
            public readonly string SelectedEventId;
            public readonly int UnityUndoGroup;
            public readonly bool RestoreEvents;
            public readonly bool RestoreWindows;

            // 创建未保存事件草稿的撤销记录
            public ComposerUndoEntry(List<AbilityEventDraft> eventDrafts, string selectedEventId)
            {
                Type = ComposerUndoEntryType.EventDraft;
                EventDrafts = eventDrafts;
                SelectedEventId = selectedEventId;
                UnityUndoGroup = -1;
                RestoreEvents = false;
                RestoreWindows = false;
            }

            // 创建已保存资产的撤销记录
            public ComposerUndoEntry(int unityUndoGroup, bool restoreEvents, bool restoreWindows)
            {
                Type = ComposerUndoEntryType.SavedAsset;
                EventDrafts = null;
                SelectedEventId = null;
                UnityUndoGroup = unityUndoGroup;
                RestoreEvents = restoreEvents;
                RestoreWindows = restoreWindows;
            }
        }

        private AbilityComposerData m_composerData;
        private AbilityTimelineData m_timelineData;
        private AbilityComposerView m_composerView;
        private AbilityTimelineView m_timelineView;
        private AbilityPreviewController m_previewController;
        private readonly AbilityAnimationClipResolver m_animationClipResolver;
        private readonly Stack<ComposerUndoEntry> m_undoEntries = new Stack<ComposerUndoEntry>();
        private readonly List<string> m_vfxSocketIdChoices = new List<string>();
        private float m_playbackElapsedTime;
        private int m_playbackStartFrame;
        private int m_currentSaveUndoGroup = -1;
        private bool m_currentSaveRestoresEvents;
        private bool m_currentSaveRestoresWindows;

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
            SetWindowConfigForClip(m_composerData.SelectedAnimationClip);
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
            RegisterEvent<AbilityWindowConfigSO>(
                callback => m_composerView.OnWindowConfigChanged += callback,
                callback => m_composerView.OnWindowConfigChanged -= callback,
                HandleWindowConfigChanged);
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
            RegisterEvent<bool>(callback => m_composerView.OnMovementLockWindowTrackToggled += callback,
                callback => m_composerView.OnMovementLockWindowTrackToggled -= callback, HandleMovementLockWindowTrackToggled);
            RegisterEvent<bool>(callback => m_composerView.OnVfxWindowTrackToggled += callback,
                callback => m_composerView.OnVfxWindowTrackToggled -= callback, HandleVfxWindowTrackToggled);
            RegisterEvent<bool>(callback => m_composerView.OnAudioWindowTrackToggled += callback,
                callback => m_composerView.OnAudioWindowTrackToggled -= callback, HandleAudioWindowTrackToggled);
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
            RegisterEvent<AbilityVfxTriggerType>(callback => m_composerView.OnWindowVfxTriggerTypeChanged += callback,
                callback => m_composerView.OnWindowVfxTriggerTypeChanged -= callback, HandleWindowVfxTriggerTypeChanged);
            RegisterEvent<AbilityVfxTargetType>(callback => m_composerView.OnWindowVfxTargetTypeChanged += callback,
                callback => m_composerView.OnWindowVfxTargetTypeChanged -= callback, HandleWindowVfxTargetTypeChanged);
            RegisterEvent<GameObject>(callback => m_composerView.OnWindowVfxPrefabChanged += callback,
                callback => m_composerView.OnWindowVfxPrefabChanged -= callback, HandleWindowVfxPrefabChanged);
            RegisterEvent<string>(callback => m_composerView.OnWindowVfxSocketIdChanged += callback,
                callback => m_composerView.OnWindowVfxSocketIdChanged -= callback, HandleWindowVfxSocketIdChanged);
            RegisterEvent<AbilityVfxLifeMode>(callback => m_composerView.OnWindowVfxLifeModeChanged += callback,
                callback => m_composerView.OnWindowVfxLifeModeChanged -= callback, HandleWindowVfxLifeModeChanged);
            RegisterEvent<Vector3>(callback => m_composerView.OnWindowVfxPositionOffsetChanged += callback,
                callback => m_composerView.OnWindowVfxPositionOffsetChanged -= callback, HandleWindowVfxPositionOffsetChanged);
            RegisterEvent<Vector3>(callback => m_composerView.OnWindowVfxEulerOffsetChanged += callback,
                callback => m_composerView.OnWindowVfxEulerOffsetChanged -= callback, HandleWindowVfxEulerOffsetChanged);
            RegisterEvent<bool>(callback => m_composerView.OnWindowVfxFollowTargetChanged += callback,
                callback => m_composerView.OnWindowVfxFollowTargetChanged -= callback, HandleWindowVfxFollowTargetChanged);
            RegisterEvent<AbilityAudioTriggerType>(callback => m_composerView.OnWindowAudioTriggerTypeChanged += callback,
                callback => m_composerView.OnWindowAudioTriggerTypeChanged -= callback, HandleWindowAudioTriggerTypeChanged);
            RegisterEvent<AbilityAudioPlaybackType>(callback => m_composerView.OnWindowAudioPlaybackTypeChanged += callback,
                callback => m_composerView.OnWindowAudioPlaybackTypeChanged -= callback, HandleWindowAudioPlaybackTypeChanged);
            RegisterEvent<int, AudioClip>(callback => m_composerView.OnWindowAudioClipChanged += callback,
                callback => m_composerView.OnWindowAudioClipChanged -= callback, HandleWindowAudioClipChanged);
            RegisterEvent(callback => m_composerView.OnWindowAudioClipAddRequested += callback,
                callback => m_composerView.OnWindowAudioClipAddRequested -= callback, HandleWindowAudioClipAddRequested);
            RegisterEvent<int>(callback => m_composerView.OnWindowAudioClipRemoveRequested += callback,
                callback => m_composerView.OnWindowAudioClipRemoveRequested -= callback, HandleWindowAudioClipRemoveRequested);
            RegisterEvent<float>(callback => m_composerView.OnWindowAudioVolumeChanged += callback,
                callback => m_composerView.OnWindowAudioVolumeChanged -= callback, HandleWindowAudioVolumeChanged);
            RegisterEvent<float>(callback => m_composerView.OnWindowAudioPitchChanged += callback,
                callback => m_composerView.OnWindowAudioPitchChanged -= callback, HandleWindowAudioPitchChanged);
            RegisterEvent<float>(callback => m_composerView.OnWindowAudioRandomPitchRangeChanged += callback,
                callback => m_composerView.OnWindowAudioRandomPitchRangeChanged -= callback, HandleWindowAudioRandomPitchRangeChanged);
            RegisterEvent<bool>(callback => m_composerView.OnWindowAudioSpatialChanged += callback,
                callback => m_composerView.OnWindowAudioSpatialChanged -= callback, HandleWindowAudioSpatialChanged);
            RegisterEvent<bool>(callback => m_composerView.OnWindowAudioStopOnWindowEndChanged += callback,
                callback => m_composerView.OnWindowAudioStopOnWindowEndChanged -= callback, HandleWindowAudioStopOnWindowEndChanged);
            RegisterEvent<AbilityAudioTargetType>(callback => m_composerView.OnWindowAudioTargetTypeChanged += callback,
                callback => m_composerView.OnWindowAudioTargetTypeChanged -= callback, HandleWindowAudioTargetTypeChanged);
            RegisterEvent<string>(callback => m_composerView.OnWindowAudioSocketIdChanged += callback,
                callback => m_composerView.OnWindowAudioSocketIdChanged -= callback, HandleWindowAudioSocketIdChanged);
            RegisterEvent<Vector3>(callback => m_composerView.OnWindowAudioPositionOffsetChanged += callback,
                callback => m_composerView.OnWindowAudioPositionOffsetChanged -= callback, HandleWindowAudioPositionOffsetChanged);
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
            RegisterEvent(callback => m_composerView.OnUndoRequested += callback,
                callback => m_composerView.OnUndoRequested -= callback, UndoLastOperation);
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
            ClearUndoHistory();
            m_composerData.SetAnimationClip(animationClip);
            m_composerView.SetSelectedAnimationClip(animationClip);
            SetWindowConfigForClip(animationClip);
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

        // 切换当前编辑的窗口主体配置
        private void HandleWindowConfigChanged(AbilityWindowConfigSO windowConfig)
        {
            if (windowConfig.AnimationClip != m_timelineData.Clip)
            {
                QLog.Error("窗口主体配置绑定的 Animation Clip 与当前时间轴动画不一致");
                return;
            }

            m_composerData.SetWindowConfig(windowConfig);
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
            RecordEventDraftUndo();
            m_timelineData.AddEvent(m_timelineData.CurrentFrame);
            RefreshView();
        }

        // 删除当前选中的动画事件草稿
        private void DeleteSelectedEvent()
        {
            if (m_timelineData.SelectedEvent == null)
                return;

            RecordEventDraftUndo();
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
            AbilityEventDraft eventDraft = FindEventDraft(request.EventId);
            if (eventDraft == null || eventDraft.Frame == request.Frame)
                return;

            RecordEventDraftUndo();
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

        // 更新移动锁定窗口轨道启用状态
        private void HandleMovementLockWindowTrackToggled(bool isEnabled)
        {
            m_timelineData.SetMovementLockWindowTrackEnabled(isEnabled);
            RefreshView();
        }

        // 更新特效窗口轨道启用状态
        private void HandleVfxWindowTrackToggled(bool isEnabled)
        {
            m_timelineData.SetVfxWindowTrackEnabled(isEnabled);
            RefreshView();
        }

        // 更新音效窗口轨道启用状态
        private void HandleAudioWindowTrackToggled(bool isEnabled)
        {
            m_timelineData.SetAudioWindowTrackEnabled(isEnabled);
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

        // 更新选中特效窗口的触发方式
        private void HandleWindowVfxTriggerTypeChanged(AbilityVfxTriggerType triggerType)
        {
            m_timelineData.SetSelectedWindowVfxTriggerType(triggerType);
            RefreshView();
        }

        // 更新选中特效窗口的生成目标
        private void HandleWindowVfxTargetTypeChanged(AbilityVfxTargetType targetType)
        {
            m_timelineData.SetSelectedWindowVfxTargetType(targetType);
            RefreshView();
        }

        // 更新选中特效窗口的预制体
        private void HandleWindowVfxPrefabChanged(GameObject vfxPrefab)
        {
            m_timelineData.SetSelectedWindowVfxPrefab(vfxPrefab);
            RefreshView();
        }

        // 更新选中特效窗口的挂点 Id
        private void HandleWindowVfxSocketIdChanged(string socketId)
        {
            m_timelineData.SetSelectedWindowVfxSocketId(socketId);
            RefreshView();
        }

        // 更新选中特效窗口的生命周期
        private void HandleWindowVfxLifeModeChanged(AbilityVfxLifeMode lifeMode)
        {
            m_timelineData.SetSelectedWindowVfxLifeMode(lifeMode);
            RefreshView();
        }

        // 更新选中特效窗口的位置偏移
        private void HandleWindowVfxPositionOffsetChanged(Vector3 positionOffset)
        {
            m_timelineData.SetSelectedWindowVfxLocalPositionOffset(positionOffset);
            RefreshView();
        }

        // 更新选中特效窗口的旋转偏移
        private void HandleWindowVfxEulerOffsetChanged(Vector3 eulerOffset)
        {
            m_timelineData.SetSelectedWindowVfxLocalEulerOffset(eulerOffset);
            RefreshView();
        }

        // 更新选中特效窗口的跟随目标状态
        private void HandleWindowVfxFollowTargetChanged(bool followTarget)
        {
            m_timelineData.SetSelectedWindowVfxFollowTarget(followTarget);
            RefreshView();
        }

        // 更新选中音效窗口的触发方式
        private void HandleWindowAudioTriggerTypeChanged(AbilityAudioTriggerType triggerType)
        {
            m_timelineData.SetSelectedWindowAudioTriggerType(triggerType);
            RefreshView();
        }

        // 更新选中音效窗口的播放类型
        private void HandleWindowAudioPlaybackTypeChanged(AbilityAudioPlaybackType playbackType)
        {
            m_timelineData.SetSelectedWindowAudioPlaybackType(playbackType);
            RefreshView();
        }

        // 更新选中音效窗口的资源槽位
        private void HandleWindowAudioClipChanged(int clipSlotIndex, AudioClip audioClip)
        {
            m_timelineData.SetSelectedWindowAudioClip(clipSlotIndex, audioClip);
            RefreshView();
        }

        // 给选中音效窗口新增资源槽位
        private void HandleWindowAudioClipAddRequested()
        {
            m_timelineData.AddSelectedWindowAudioClip();
            RefreshView();
        }

        // 删除选中音效窗口的资源槽位
        private void HandleWindowAudioClipRemoveRequested(int clipSlotIndex)
        {
            m_timelineData.RemoveSelectedWindowAudioClip(clipSlotIndex);
            RefreshView();
        }

        // 更新选中音效窗口的音量
        private void HandleWindowAudioVolumeChanged(float volume)
        {
            m_timelineData.SetSelectedWindowAudioVolume(volume);
            RefreshView();
        }

        // 更新选中音效窗口的音高
        private void HandleWindowAudioPitchChanged(float pitch)
        {
            m_timelineData.SetSelectedWindowAudioPitch(pitch);
            RefreshView();
        }

        // 更新选中音效窗口的随机音高范围
        private void HandleWindowAudioRandomPitchRangeChanged(float randomPitchRange)
        {
            m_timelineData.SetSelectedWindowAudioRandomPitchRange(randomPitchRange);
            RefreshView();
        }

        // 更新选中音效窗口的空间化状态
        private void HandleWindowAudioSpatialChanged(bool spatial)
        {
            m_timelineData.SetSelectedWindowAudioSpatial(spatial);
            RefreshView();
        }

        // 更新选中音效窗口的窗口结束截断状态
        private void HandleWindowAudioStopOnWindowEndChanged(bool stopOnWindowEnd)
        {
            m_timelineData.SetSelectedWindowAudioStopOnWindowEnd(stopOnWindowEnd);
            RefreshView();
        }

        // 更新选中音效窗口的播放目标
        private void HandleWindowAudioTargetTypeChanged(AbilityAudioTargetType targetType)
        {
            m_timelineData.SetSelectedWindowAudioTargetType(targetType);
            RefreshView();
        }

        // 更新选中音效窗口的挂点 Id
        private void HandleWindowAudioSocketIdChanged(string socketId)
        {
            m_timelineData.SetSelectedWindowAudioSocketId(socketId);
            RefreshView();
        }

        // 更新选中音效窗口的位置偏移
        private void HandleWindowAudioPositionOffsetChanged(Vector3 positionOffset)
        {
            m_timelineData.SetSelectedWindowAudioLocalPositionOffset(positionOffset);
            RefreshView();
        }

        // 更新选中事件的分类颜色
        private void HandleEventCategoryChanged(AbilityEventCategory category)
        {
            if (m_timelineData.SelectedEvent == null || m_timelineData.SelectedEvent.Category == category)
                return;

            RecordEventDraftUndo();
            m_timelineData.SetSelectedEventCategory(category);
            RefreshView();
        }

        // 更新选中事件的接收类名称
        private void HandleEventReceiverTypeNameChanged(string receiverTypeName)
        {
            if (m_timelineData.SelectedEvent == null
                || m_timelineData.SelectedEvent.ReceiverTypeName == receiverTypeName)
                return;

            RecordEventDraftUndo();
            m_timelineData.SetSelectedEventReceiverTypeName(receiverTypeName);
            RefreshView();
        }

        // 更新选中事件的 Function 名称
        private void HandleEventFunctionNameChanged(string functionName)
        {
            if (m_timelineData.SelectedEvent == null || m_timelineData.SelectedEvent.FunctionName == functionName)
                return;

            RecordEventDraftUndo();
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
                    BeginSaveUndoGroup("保存 Ability Animation Events", true, false);

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
                BeginSaveUndoGroup("保存 Ability Animation Events", true, false);

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
            BeginSaveUndoGroup("一键保存 Ability Composer", true, true);
            bool savedEvents = ApplyAnimationEvents(false);
            bool savedWindows = SaveAllWindowTracks();
            if (savedEvents || savedWindows)
                CompleteSaveUndoGroup();
            else
                CancelSaveUndoGroup();

            RefreshView();
        }

        // 撤销 Composer 最近一次草稿编辑或资产保存
        private void UndoLastOperation()
        {
            if (m_undoEntries.Count == 0)
                return;

            ComposerUndoEntry undoEntry = m_undoEntries.Pop();
            if (undoEntry.Type == ComposerUndoEntryType.EventDraft)
            {
                m_timelineData.RestoreEventDraftSnapshot(undoEntry.EventDrafts, undoEntry.SelectedEventId);
            }
            else
            {
                Undo.RevertAllDownToGroup(undoEntry.UnityUndoGroup);
                if (undoEntry.RestoreEvents)
                    RestoreAnimationEvents();
                if (undoEntry.RestoreWindows)
                    LoadWindowTrack();
            }

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

            LoadAnimationEvents();
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
            RefreshVfxSocketIdChoices();
            m_composerView.Refresh(m_timelineData, m_previewController.HasPreview, true, m_undoEntries.Count > 0,
                m_vfxSocketIdChoices);
            m_timelineView.RefreshCurrentFrame();
            if (refreshEventMarkers)
                m_timelineView.RefreshEventMarkers();
        }

        // 从当前预览实例与预览来源收集特效挂点候选
        private void RefreshVfxSocketIdChoices()
        {
            m_vfxSocketIdChoices.Clear();
            if (m_previewController.HasPreview)
                CollectVfxSocketIdChoices(m_previewController.PreviewRoot);

            CollectVfxSocketIdChoices(m_composerData.PreviewSource);
        }

        // 从指定对象层级收集特效挂点候选
        private void CollectVfxSocketIdChoices(GameObject socketSource)
        {
            if (socketSource == null)
                return;

            VfxSocketBinder[] socketBinders = socketSource.GetComponentsInChildren<VfxSocketBinder>(true);
            for (int binderIndex = 0; binderIndex < socketBinders.Length; binderIndex++)
                socketBinders[binderIndex].CollectSocketIds(m_vfxSocketIdChoices);
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

        // 从当前窗口主体配置加载草稿到时间轴
        private void LoadWindowTrack()
        {
            m_timelineData.ClearWindows();
            AbilityWindowConfigSO windowConfig = m_composerData.SelectedWindowConfig;
            m_timelineData.SetWindowConfig(windowConfig);

            if (!m_timelineData.HasClip || windowConfig == null)
                return;

            AbilityHitWindowTrackData hitTrack = windowConfig.UseHitWindow ? windowConfig.HitWindowTrack : null;
            AbilityStepAdvanceWindowTrackData stepAdvanceTrack = windowConfig.UseStepAdvanceWindow
                ? windowConfig.StepAdvanceWindowTrack
                : null;
            AbilityMovementLockWindowTrackData movementLockTrack = windowConfig.UseMovementLockWindow
                ? windowConfig.MovementLockWindowTrack
                : null;
            AbilityVfxWindowTrackData vfxTrack = windowConfig.UseVfxWindow
                ? windowConfig.VfxWindowTrack
                : null;
            AbilityAudioWindowTrackData audioTrack = windowConfig.UseAudioWindow
                ? windowConfig.AudioWindowTrack
                : null;

            if (windowConfig != null && windowConfig.AnimationClip != m_timelineData.Clip)
            {
                QLog.Error("窗口主体配置绑定的 Animation Clip 与当前时间轴动画不一致");
                return;
            }

            if (hitTrack != null)
            {
                foreach (AbilityHitWindowData windowData in hitTrack.Windows)
                {
                    int startFrame = ConvertStartNormalizedTimeToFrame(windowData.StartNormalizedTime);
                    int endFrame = ConvertEndNormalizedTimeToBoundaryFrame(windowData.EndNormalizedTime);
                    AbilityWindowDraft windowDraft = m_timelineData.AddWindow(AbilityWindowDraftType.Hit,
                        startFrame, endFrame, windowData.Damage);
                    windowDraft.SetId(windowData.Id);
                }
            }

            if (stepAdvanceTrack != null)
            {
                foreach (AbilityStepAdvanceWindowData windowData in stepAdvanceTrack.Windows)
                {
                    int startFrame = ConvertStartNormalizedTimeToFrame(windowData.StartNormalizedTime);
                    int endFrame = ConvertEndNormalizedTimeToBoundaryFrame(windowData.EndNormalizedTime);
                    AbilityWindowDraft windowDraft = m_timelineData.AddWindow(AbilityWindowDraftType.StepAdvance,
                        startFrame, endFrame, 0f);
                    windowDraft.SetId(windowData.Id);
                }
            }

            if (movementLockTrack != null)
            {
                foreach (AbilityMovementLockWindowData windowData in movementLockTrack.Windows)
                {
                    int startFrame = ConvertStartNormalizedTimeToFrame(windowData.StartNormalizedTime);
                    int endFrame = ConvertEndNormalizedTimeToBoundaryFrame(windowData.EndNormalizedTime);
                    AbilityWindowDraft windowDraft = m_timelineData.AddWindow(AbilityWindowDraftType.MovementLock,
                        startFrame, endFrame, 0f);
                    windowDraft.SetId(windowData.Id);
                }
            }

            if (vfxTrack != null)
            {
                foreach (AbilityVfxWindowData windowData in vfxTrack.Windows)
                {
                    int startFrame = ConvertStartNormalizedTimeToFrame(windowData.StartNormalizedTime);
                    int endFrame = ConvertEndNormalizedTimeToBoundaryFrame(windowData.EndNormalizedTime);
                    AbilityWindowDraft windowDraft = m_timelineData.AddWindow(AbilityWindowDraftType.Vfx,
                        startFrame, endFrame, 0f);
                    windowDraft.SetId(windowData.Id);
                    windowDraft.SetVfxTriggerType(windowData.TriggerType);
                    windowDraft.SetVfxTargetType(windowData.TargetType);
                    windowDraft.SetVfxPrefab(windowData.VfxPrefab);
                    windowDraft.SetVfxSocketId(windowData.SocketId);
                    windowDraft.SetVfxLifeMode(windowData.LifeMode);
                    windowDraft.SetVfxLocalPositionOffset(windowData.LocalPositionOffset);
                    windowDraft.SetVfxLocalEulerOffset(windowData.LocalEulerOffset);
                    windowDraft.SetVfxFollowTarget(windowData.FollowTarget);
                }
            }

            if (audioTrack != null)
            {
                foreach (AbilityAudioWindowData windowData in audioTrack.Windows)
                {
                    int startFrame = ConvertStartNormalizedTimeToFrame(windowData.StartNormalizedTime);
                    int endFrame = ConvertEndNormalizedTimeToBoundaryFrame(windowData.EndNormalizedTime);
                    AbilityWindowDraft windowDraft = m_timelineData.AddWindow(AbilityWindowDraftType.Audio,
                        startFrame, endFrame, 0f);
                    windowDraft.SetId(windowData.Id);
                    windowDraft.SetAudioTriggerType(windowData.TriggerType);
                    windowDraft.SetAudioPlaybackType(windowData.PlaybackType);
                    windowDraft.SetAudioClips(windowData.AudioClips);
                    windowDraft.SetAudioVolume(windowData.Volume);
                    windowDraft.SetAudioPitch(windowData.Pitch);
                    windowDraft.SetAudioRandomPitchRange(windowData.RandomPitchRange);
                    windowDraft.SetAudioSpatial(windowData.Spatial);
                    windowDraft.SetAudioStopOnWindowEnd(windowData.StopOnWindowEnd);
                    windowDraft.SetAudioTargetType(windowData.TargetType);
                    windowDraft.SetAudioSocketId(windowData.SocketId);
                    windowDraft.SetAudioLocalPositionOffset(windowData.LocalPositionOffset);
                }
            }

            m_timelineData.ClearWindowSelection();
        }

        // 将运行时归一化起始时间转换为编辑器起始帧
        private int ConvertStartNormalizedTimeToFrame(float normalizedTime)
        {
            if (!m_timelineData.HasClip)
                return 0;

            int frame = Mathf.RoundToInt(normalizedTime * m_timelineData.LastFrame);
            return Mathf.Clamp(frame, 0, m_timelineData.LastFrame);
        }

        // 将运行时归一化结束时间转换为编辑器右边界帧
        private int ConvertEndNormalizedTimeToBoundaryFrame(float normalizedTime)
        {
            if (!m_timelineData.HasClip)
                return 0;

            int runtimeEndFrame = Mathf.RoundToInt(normalizedTime * m_timelineData.LastFrame);
            return Mathf.Clamp(runtimeEndFrame + 1, 1, m_timelineData.LastBoundaryFrame);
        }

        // 将编辑器起始帧转换为运行时归一化起始时间
        private float ConvertStartFrameToNormalizedTime(int startFrame)
        {
            if (m_timelineData.LastFrame <= 0)
                return 0f;

            int runtimeStartFrame = Mathf.Clamp(startFrame, 0, m_timelineData.LastFrame);
            return runtimeStartFrame / (float)m_timelineData.LastFrame;
        }

        // 将编辑器右边界帧转换为运行时归一化结束时间
        private float ConvertEndBoundaryFrameToNormalizedTime(int endBoundaryFrame)
        {
            if (m_timelineData.LastFrame <= 0)
                return 0f;

            int runtimeEndFrame = Mathf.Clamp(endBoundaryFrame - 1, 0, m_timelineData.LastFrame);
            return runtimeEndFrame / (float)m_timelineData.LastFrame;
        }

        // 查找当前动画唯一绑定的窗口主体配置
        private AbilityWindowConfigSO FindWindowConfig(AnimationClip animationClip, out bool hasDuplicate)
        {
            hasDuplicate = false;
            if (animationClip == null)
                return null;

            AbilityWindowConfigSO matchedConfig = null;
            string[] configGuids = AssetDatabase.FindAssets("t:AbilityWindowConfigSO");
            foreach (string configGuid in configGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(configGuid);
                AbilityWindowConfigSO windowConfig = AssetDatabase.LoadAssetAtPath<AbilityWindowConfigSO>(assetPath);
                if (windowConfig == null || windowConfig.AnimationClip != animationClip)
                    continue;

                if (matchedConfig == null)
                {
                    matchedConfig = windowConfig;
                    continue;
                }

                hasDuplicate = true;
                QLog.Error($"动画 {animationClip.name} 绑定了多个窗口主体配置，请保留唯一配置");
                return null;
            }

            return matchedConfig;
        }

        // 匹配当前动画唯一绑定的窗口主体配置
        private void SetWindowConfigForClip(AnimationClip animationClip)
        {
            AbilityWindowConfigSO windowConfig = FindWindowConfig(animationClip, out bool hasDuplicate);
            if (hasDuplicate)
            {
                m_composerData.SetWindowConfig(null);
                return;
            }

            m_composerData.SetWindowConfig(windowConfig);
        }

        // 将当前选中窗口写回所属类型的轨道数据
        private void SaveWindowTrack()
        {
            switch (m_timelineData.SelectedWindow.Type)
            {
                case AbilityWindowDraftType.Hit:
                    SaveHitWindowTrack(true);
                    break;
                case AbilityWindowDraftType.StepAdvance:
                    SaveStepAdvanceWindowTrack(true);
                    break;
                case AbilityWindowDraftType.MovementLock:
                    SaveMovementLockWindowTrack(true);
                    break;
                case AbilityWindowDraftType.Vfx:
                    SaveVfxWindowTrack(true);
                    break;
                case AbilityWindowDraftType.Audio:
                    SaveAudioWindowTrack(true);
                    break;
            }

            RefreshView();
        }

        // 保存全部已启用或包含草稿的窗口轨道
        private bool SaveAllWindowTracks()
        {
            bool savedWindowTrack = false;
            AbilityWindowConfigSO windowConfig = m_composerData.SelectedWindowConfig;
            bool hasHitWindowTrack = windowConfig != null && windowConfig.UseHitWindow;
            bool hasStepAdvanceWindowTrack = windowConfig != null && windowConfig.UseStepAdvanceWindow;
            bool hasMovementLockWindowTrack = windowConfig != null && windowConfig.UseMovementLockWindow;
            bool hasVfxWindowTrack = windowConfig != null && windowConfig.UseVfxWindow;
            bool hasAudioWindowTrack = windowConfig != null && windowConfig.UseAudioWindow;

            if (hasHitWindowTrack || m_timelineData.HitWindowDraftValues.Count > 0)
                savedWindowTrack |= SaveHitWindowTrack(false);
            if (hasStepAdvanceWindowTrack || m_timelineData.StepAdvanceWindowDraftValues.Count > 0)
                savedWindowTrack |= SaveStepAdvanceWindowTrack(false);
            if (hasMovementLockWindowTrack || m_timelineData.MovementLockWindowDraftValues.Count > 0)
                savedWindowTrack |= SaveMovementLockWindowTrack(false);
            if (hasVfxWindowTrack || m_timelineData.VfxWindowDraftValues.Count > 0)
                savedWindowTrack |= SaveVfxWindowTrack(false);
            if (hasAudioWindowTrack || m_timelineData.AudioWindowDraftValues.Count > 0)
                savedWindowTrack |= SaveAudioWindowTrack(false);

            return savedWindowTrack;
        }

        // 将当前命中窗口草稿写回对应轨道数据
        private bool SaveHitWindowTrack(bool createUndoGroup)
        {
            if (!m_timelineData.HasClip)
                return false;

            AbilityWindowConfigSO windowConfig = GetOrCreateWindowConfig();
            if (windowConfig == null)
                return false;

            if (createUndoGroup)
                BeginSaveUndoGroup("保存命中窗口轨道", false, true);

            AbilityHitWindowTrackData windowTrack = windowConfig.HitWindowTrack;

            List<AbilityHitWindowData> windowValues = new List<AbilityHitWindowData>();
            foreach (AbilityWindowDraft windowDraft in m_timelineData.HitWindowDraftValues)
            {
                float startNormalizedTime = ConvertStartFrameToNormalizedTime(windowDraft.StartFrame);
                float endNormalizedTime = ConvertEndBoundaryFrameToNormalizedTime(windowDraft.EndFrame);
                windowValues.Add(new AbilityHitWindowData(windowDraft.Id, startNormalizedTime, endNormalizedTime,
                    windowDraft.Damage));
            }

            Undo.RecordObject(windowConfig, "保存命中窗口轨道");
            windowTrack.SetWindows(windowValues);
            windowConfig.SetHitWindow(true, windowTrack);
            EditorUtility.SetDirty(windowConfig);
            m_composerData.SetWindowConfig(windowConfig);
            m_timelineData.SetWindowConfig(windowConfig);
            AssetDatabase.SaveAssets();
            if (createUndoGroup)
                CompleteSaveUndoGroup();

            return true;
        }

        // 将当前技能推进窗口草稿写回对应轨道数据
        private bool SaveStepAdvanceWindowTrack(bool createUndoGroup)
        {
            if (!m_timelineData.HasClip)
                return false;

            AbilityWindowConfigSO windowConfig = GetOrCreateWindowConfig();
            if (windowConfig == null)
                return false;

            if (createUndoGroup)
                BeginSaveUndoGroup("保存技能推进窗口轨道", false, true);

            AbilityStepAdvanceWindowTrackData track = windowConfig.StepAdvanceWindowTrack;

            List<AbilityStepAdvanceWindowData> values = new List<AbilityStepAdvanceWindowData>();
            foreach (AbilityWindowDraft draft in m_timelineData.StepAdvanceWindowDraftValues)
            {
                float start = ConvertStartFrameToNormalizedTime(draft.StartFrame);
                float end = ConvertEndBoundaryFrameToNormalizedTime(draft.EndFrame);
                values.Add(new AbilityStepAdvanceWindowData(draft.Id, start, end));
            }

            Undo.RecordObject(windowConfig, "保存技能推进窗口轨道");
            track.SetWindows(values);
            windowConfig.SetStepAdvanceWindow(true, track);
            EditorUtility.SetDirty(windowConfig);
            m_composerData.SetWindowConfig(windowConfig);
            m_timelineData.SetWindowConfig(windowConfig);
            AssetDatabase.SaveAssets();
            if (createUndoGroup)
                CompleteSaveUndoGroup();

            return true;
        }

        // 将当前移动锁定窗口草稿写回对应轨道数据
        private bool SaveMovementLockWindowTrack(bool createUndoGroup)
        {
            if (!m_timelineData.HasClip)
                return false;

            AbilityWindowConfigSO windowConfig = GetOrCreateWindowConfig();
            if (windowConfig == null)
                return false;

            if (createUndoGroup)
                BeginSaveUndoGroup("保存移动锁定窗口轨道", false, true);

            AbilityMovementLockWindowTrackData track = windowConfig.MovementLockWindowTrack;

            List<AbilityMovementLockWindowData> values = new List<AbilityMovementLockWindowData>();
            foreach (AbilityWindowDraft draft in m_timelineData.MovementLockWindowDraftValues)
            {
                float start = ConvertStartFrameToNormalizedTime(draft.StartFrame);
                float end = ConvertEndBoundaryFrameToNormalizedTime(draft.EndFrame);
                values.Add(new AbilityMovementLockWindowData(draft.Id, start, end));
            }

            Undo.RecordObject(windowConfig, "保存移动锁定窗口轨道");
            track.SetWindows(values);
            windowConfig.SetMovementLockWindow(true, track);
            EditorUtility.SetDirty(windowConfig);
            m_composerData.SetWindowConfig(windowConfig);
            m_timelineData.SetWindowConfig(windowConfig);
            AssetDatabase.SaveAssets();
            if (createUndoGroup)
                CompleteSaveUndoGroup();

            return true;
        }

        // 将当前特效窗口草稿写回对应轨道数据
        private bool SaveVfxWindowTrack(bool createUndoGroup)
        {
            if (!m_timelineData.HasClip)
                return false;

            AbilityWindowConfigSO windowConfig = GetOrCreateWindowConfig();
            if (windowConfig == null)
                return false;

            if (createUndoGroup)
                BeginSaveUndoGroup("保存特效窗口轨道", false, true);

            AbilityVfxWindowTrackData track = windowConfig.VfxWindowTrack;

            List<AbilityVfxWindowData> values = new List<AbilityVfxWindowData>();
            foreach (AbilityWindowDraft draft in m_timelineData.VfxWindowDraftValues)
            {
                float start = ConvertStartFrameToNormalizedTime(draft.StartFrame);
                float end = ConvertEndBoundaryFrameToNormalizedTime(draft.EndFrame);
                values.Add(new AbilityVfxWindowData(draft.Id, start, end, draft.VfxTriggerType,
                    draft.VfxTargetType, draft.VfxPrefab, draft.VfxSocketId, draft.VfxLifeMode,
                    draft.VfxLocalPositionOffset, draft.VfxLocalEulerOffset, draft.VfxFollowTarget));
            }

            Undo.RecordObject(windowConfig, "保存特效窗口轨道");
            track.SetWindows(values);
            windowConfig.SetVfxWindow(true, track);
            EditorUtility.SetDirty(windowConfig);
            m_composerData.SetWindowConfig(windowConfig);
            m_timelineData.SetWindowConfig(windowConfig);
            AssetDatabase.SaveAssets();
            if (createUndoGroup)
                CompleteSaveUndoGroup();

            return true;
        }

        // 将当前音效窗口草稿写回对应轨道数据
        private bool SaveAudioWindowTrack(bool createUndoGroup)
        {
            if (!m_timelineData.HasClip)
                return false;

            AbilityWindowConfigSO windowConfig = GetOrCreateWindowConfig();
            if (windowConfig == null)
                return false;

            if (createUndoGroup)
                BeginSaveUndoGroup("保存音效窗口轨道", false, true);

            AbilityAudioWindowTrackData track = windowConfig.AudioWindowTrack;

            List<AbilityAudioWindowData> values = new List<AbilityAudioWindowData>();
            foreach (AbilityWindowDraft draft in m_timelineData.AudioWindowDraftValues)
            {
                float start = ConvertStartFrameToNormalizedTime(draft.StartFrame);
                float end = ConvertEndBoundaryFrameToNormalizedTime(draft.EndFrame);
                values.Add(new AbilityAudioWindowData(draft.Id, start, end, draft.AudioTriggerType,
                    draft.AudioPlaybackType, draft.AudioClips, draft.AudioVolume, draft.AudioPitch,
                    draft.AudioRandomPitchRange, draft.AudioSpatial,
                    draft.AudioStopOnWindowEnd, draft.AudioTargetType, draft.AudioSocketId,
                    draft.AudioLocalPositionOffset));
            }

            Undo.RecordObject(windowConfig, "保存音效窗口轨道");
            track.SetWindows(values);
            windowConfig.SetAudioWindow(true, track);
            EditorUtility.SetDirty(windowConfig);
            m_composerData.SetWindowConfig(windowConfig);
            m_timelineData.SetWindowConfig(windowConfig);
            AssetDatabase.SaveAssets();
            if (createUndoGroup)
                CompleteSaveUndoGroup();

            return true;
        }

        // 获取当前动画的窗口主体配置，缺失时只创建一个主体资产
        private AbilityWindowConfigSO GetOrCreateWindowConfig()
        {
            AbilityWindowConfigSO selectedConfig = m_composerData.SelectedWindowConfig;
            if (selectedConfig != null)
                return selectedConfig;

            AbilityWindowConfigSO windowConfig = FindWindowConfig(m_timelineData.Clip, out bool hasDuplicate);
            if (hasDuplicate)
                return null;

            if (windowConfig != null)
            {
                m_composerData.SetWindowConfig(windowConfig);
                m_timelineData.SetWindowConfig(windowConfig);
                return windowConfig;
            }

            string clipName = CreateSafeAssetFileName(m_timelineData.Clip.name);
            string defaultName = $"{clipName}_WindowConfig.asset";
            string assetPath = EditorUtility.SaveFilePanelInProject("新建 Ability 窗口主体配置", defaultName,
                "asset", "选择窗口主体配置的保存位置");
            if (string.IsNullOrEmpty(assetPath))
                return null;

            windowConfig = ScriptableObject.CreateInstance<AbilityWindowConfigSO>();
            windowConfig.SetAnimationClip(m_timelineData.Clip);
            AssetDatabase.CreateAsset(windowConfig, assetPath);
            EditorUtility.SetDirty(windowConfig);
            m_composerData.SetWindowConfig(windowConfig);
            m_timelineData.SetWindowConfig(windowConfig);
            return windowConfig;
        }

        // 创建可被底部撤销按钮定位的一次保存 Undo 组
        private void BeginSaveUndoGroup(string undoName, bool restoreEvents, bool restoreWindows)
        {
            Undo.IncrementCurrentGroup();
            m_currentSaveUndoGroup = Undo.GetCurrentGroup();
            m_currentSaveRestoresEvents = restoreEvents;
            m_currentSaveRestoresWindows = restoreWindows;
            Undo.SetCurrentGroupName(undoName);
        }

        // 关闭当前保存 Undo 组并记录到 Composer 撤销历史
        private void CompleteSaveUndoGroup()
        {
            if (m_currentSaveUndoGroup < 0)
                return;

            Undo.CollapseUndoOperations(m_currentSaveUndoGroup);
            if (m_currentSaveRestoresEvents)
                DiscardPendingEventDraftUndoEntries();

            m_undoEntries.Push(new ComposerUndoEntry(m_currentSaveUndoGroup,
                m_currentSaveRestoresEvents, m_currentSaveRestoresWindows));
            CancelSaveUndoGroup();
        }

        // 记录当前事件草稿与选中状态供后续撤销
        private void RecordEventDraftUndo()
        {
            string selectedEventId = m_timelineData.SelectedEvent == null
                ? null
                : m_timelineData.SelectedEvent.Id;
            m_undoEntries.Push(new ComposerUndoEntry(m_timelineData.CreateEventDraftSnapshot(), selectedEventId));
        }

        // 查找指定稳定标识对应的事件草稿
        private AbilityEventDraft FindEventDraft(string eventId)
        {
            for (int eventIndex = 0; eventIndex < m_timelineData.EventDraftValues.Count; eventIndex++)
            {
                AbilityEventDraft eventDraft = m_timelineData.EventDraftValues[eventIndex];
                if (eventDraft.Id == eventId)
                    return eventDraft;
            }

            return null;
        }

        // 移除已被事件资产保存包含的未保存草稿历史
        private void DiscardPendingEventDraftUndoEntries()
        {
            while (m_undoEntries.Count > 0
                && m_undoEntries.Peek().Type == ComposerUndoEntryType.EventDraft)
                m_undoEntries.Pop();
        }

        // 取消当前尚未提交的资产保存 Undo 组记录
        private void CancelSaveUndoGroup()
        {
            m_currentSaveUndoGroup = -1;
            m_currentSaveRestoresEvents = false;
            m_currentSaveRestoresWindows = false;
        }

        // 切换动画时清空不再适用于新时间轴的撤销历史
        private void ClearUndoHistory()
        {
            m_undoEntries.Clear();
            CancelSaveUndoGroup();
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
