/*
 * ┌───────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 时间轴数据，保存动画帧、事件草稿与分类窗口草稿
 * │  类    名: AbilityTimelineData.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Ability.Data.Window;
using Module.Ability.Data.Window.Audio;
using Module.Ability.Data.Window.Hit;
using Module.Ability.Data.Window.MovementLock;
using Module.Ability.Data.Window.StepAdvance;
using Module.Ability.Data.Window.Vfx;
using Tools.AbilityComposer.Editor.View.Center.Event;
using UnityEngine;
using Utils.log;

namespace Tools.AbilityComposer.Editor.View.Center.Timeline
{
    public sealed class AbilityTimelineData
    {
        private readonly List<AbilityEventDraft> m_eventDraftValues = new List<AbilityEventDraft>();
        private readonly List<AbilityWindowDraft> m_hitWindowDraftValues = new List<AbilityWindowDraft>();
        private readonly List<AbilityWindowDraft> m_stepAdvanceWindowDraftValues = new List<AbilityWindowDraft>();
        private readonly List<AbilityWindowDraft> m_movementLockWindowDraftValues = new List<AbilityWindowDraft>();
        private readonly List<AbilityWindowDraft> m_vfxWindowDraftValues = new List<AbilityWindowDraft>();
        private readonly List<AbilityWindowDraft> m_audioWindowDraftValues = new List<AbilityWindowDraft>();

        public AnimationClip Clip { get; private set; }
        public float FrameRate { get; private set; }
        public int FrameCount { get; private set; }
        public int CurrentFrame { get; private set; }
        public bool IsPlaying { get; private set; }
        public IReadOnlyList<AbilityEventDraft> EventDraftValues => m_eventDraftValues;
        public IReadOnlyList<AbilityWindowDraft> HitWindowDraftValues => m_hitWindowDraftValues;
        public IReadOnlyList<AbilityWindowDraft> StepAdvanceWindowDraftValues => m_stepAdvanceWindowDraftValues;
        public IReadOnlyList<AbilityWindowDraft> MovementLockWindowDraftValues => m_movementLockWindowDraftValues;
        public IReadOnlyList<AbilityWindowDraft> VfxWindowDraftValues => m_vfxWindowDraftValues;
        public IReadOnlyList<AbilityWindowDraft> AudioWindowDraftValues => m_audioWindowDraftValues;
        public AbilityEventDraft SelectedEvent { get; private set; }
        public AbilityWindowDraft SelectedWindow { get; private set; }
        public AbilityWindowConfigSO WindowConfig { get; private set; }
        public AbilityHitWindowTrackData HitWindowTrack { get; private set; }
        public AbilityStepAdvanceWindowTrackData StepAdvanceWindowTrack { get; private set; }
        public AbilityMovementLockWindowTrackData MovementLockWindowTrack { get; private set; }
        public AbilityVfxWindowTrackData VfxWindowTrack { get; private set; }
        public AbilityAudioWindowTrackData AudioWindowTrack { get; private set; }
        public bool IsWindowInspectorActive { get; private set; }
        public bool IsHitWindowTrackEnabled { get; private set; } = true;
        public bool IsStepAdvanceWindowTrackEnabled { get; private set; } = true;
        public bool IsMovementLockWindowTrackEnabled { get; private set; } = true;
        public bool IsVfxWindowTrackEnabled { get; private set; } = true;
        public bool IsAudioWindowTrackEnabled { get; private set; } = true;
        public bool HasClip => Clip != null;
        public int LastFrame => Mathf.Max(FrameCount - 1, 0);
        public int LastBoundaryFrame => HasClip ? Mathf.Max(FrameCount, 1) : 0;
        public float CurrentTime => FrameRate > 0f ? CurrentFrame / FrameRate : 0f;

        // 切换当前时间轴编辑的动画片段
        public void SetAnimationClip(AnimationClip animationClip)
        {
            Clip = animationClip;
            FrameRate = animationClip == null ? 0f : animationClip.frameRate;
            FrameCount = animationClip == null ? 0 : Mathf.Max(Mathf.RoundToInt(animationClip.length * FrameRate) + 1, 1);
            CurrentFrame = 0;
            IsPlaying = false;
            m_eventDraftValues.Clear();
            m_hitWindowDraftValues.Clear();
            m_stepAdvanceWindowDraftValues.Clear();
            m_movementLockWindowDraftValues.Clear();
            m_vfxWindowDraftValues.Clear();
            m_audioWindowDraftValues.Clear();
            SelectedEvent = null;
            SelectedWindow = null;
            IsWindowInspectorActive = false;
        }

        // 更新当前编辑的窗口主体配置与三类轨道数据
        public void SetWindowConfig(AbilityWindowConfigSO windowConfig)
        {
            WindowConfig = windowConfig;
            HitWindowTrack = windowConfig == null ? null : windowConfig.HitWindowTrack;
            StepAdvanceWindowTrack = windowConfig == null ? null : windowConfig.StepAdvanceWindowTrack;
            MovementLockWindowTrack = windowConfig == null ? null : windowConfig.MovementLockWindowTrack;
            VfxWindowTrack = windowConfig == null ? null : windowConfig.VfxWindowTrack;
            AudioWindowTrack = windowConfig == null ? null : windowConfig.AudioWindowTrack;
        }

        // 设置命中窗口轨道的显示状态
        public void SetHitWindowTrackEnabled(bool isEnabled)
        {
            IsHitWindowTrackEnabled = isEnabled;
        }

        // 设置技能推进窗口轨道的显示状态
        public void SetStepAdvanceWindowTrackEnabled(bool isEnabled)
        {
            IsStepAdvanceWindowTrackEnabled = isEnabled;
        }

        // 设置移动锁定窗口轨道的显示状态
        public void SetMovementLockWindowTrackEnabled(bool isEnabled)
        {
            IsMovementLockWindowTrackEnabled = isEnabled;
        }

        // 设置特效窗口轨道的显示状态
        public void SetVfxWindowTrackEnabled(bool isEnabled)
        {
            IsVfxWindowTrackEnabled = isEnabled;
        }

        // 设置音效窗口轨道的显示状态
        public void SetAudioWindowTrackEnabled(bool isEnabled)
        {
            IsAudioWindowTrackEnabled = isEnabled;
        }

        // 从 AnimationClip 的事件数据重建内存草稿
        public void LoadAnimationEvents(IReadOnlyList<AnimationEvent> animationEvents)
        {
            m_eventDraftValues.Clear();
            SelectedEvent = null;
            for (int eventIndex = 0; eventIndex < animationEvents.Count; eventIndex++)
            {
                AnimationEvent animationEvent = animationEvents[eventIndex];
                AbilityEventDraft eventDraft = AddEvent(Mathf.RoundToInt(animationEvent.time * FrameRate));
                eventDraft.SetFunctionName(animationEvent.functionName);
            }

            SelectedEvent = null;
        }

        // 创建当前事件草稿的独立副本供撤销使用
        public List<AbilityEventDraft> CreateEventDraftSnapshot()
        {
            List<AbilityEventDraft> snapshot = new List<AbilityEventDraft>(m_eventDraftValues.Count);
            for (int eventIndex = 0; eventIndex < m_eventDraftValues.Count; eventIndex++)
                snapshot.Add(m_eventDraftValues[eventIndex].Clone());

            return snapshot;
        }

        // 从撤销快照恢复事件草稿与选中状态
        public void RestoreEventDraftSnapshot(IReadOnlyList<AbilityEventDraft> snapshot, string selectedEventId)
        {
            m_eventDraftValues.Clear();
            for (int eventIndex = 0; eventIndex < snapshot.Count; eventIndex++)
                m_eventDraftValues.Add(snapshot[eventIndex].Clone());

            SelectedEvent = string.IsNullOrEmpty(selectedEventId)
                ? null
                : m_eventDraftValues.Find(eventDraft => eventDraft.Id == selectedEventId);
            IsWindowInspectorActive = SelectedEvent == null && SelectedWindow != null;
        }

        // 将播放头设置到有效帧范围内
        public void SetCurrentFrame(int frame)
        {
            CurrentFrame = Mathf.Clamp(frame, 0, LastFrame);
        }

        // 开始按 Editor 时间推进时间轴
        public void StartPlayback()
        {
            if (!HasClip)
                return;

            IsPlaying = true;
        }

        // 停止播放并保留当前播放头位置
        public void StopPlayback()
        {
            IsPlaying = false;
        }

        // 在指定帧创建并选中空事件草稿
        public AbilityEventDraft AddEvent(int frame)
        {
            AbilityEventDraft eventDraft = new AbilityEventDraft(Mathf.Clamp(frame, 0, LastFrame));
            m_eventDraftValues.Add(eventDraft);
            SelectedEvent = eventDraft;
            IsWindowInspectorActive = false;
            return eventDraft;
        }

        // 删除当前选中的事件草稿
        public void DeleteSelectedEvent()
        {
            if (SelectedEvent == null)
                return;

            m_eventDraftValues.Remove(SelectedEvent);
            SelectedEvent = null;
        }

        // 按唯一标识选中指定事件草稿
        public void SelectEvent(string eventId)
        {
            SelectedEvent = m_eventDraftValues.Find(eventDraft => eventDraft.Id == eventId);
            IsWindowInspectorActive = false;
        }

        // 取消当前事件选中状态
        public void ClearEventSelection()
        {
            SelectedEvent = null;
            IsWindowInspectorActive = SelectedWindow != null;
        }

        // 更新选中事件的编辑器分类
        public void SetSelectedEventCategory(AbilityEventCategory category)
        {
            if (SelectedEvent == null)
                return;

            SelectedEvent.SetCategory(category);
        }

        // 更新指定事件所在帧
        public void SetEventFrame(string eventId, int frame)
        {
            AbilityEventDraft eventDraft = m_eventDraftValues.Find(item => item.Id == eventId);
            if (eventDraft == null)
                return;

            eventDraft.SetFrame(Mathf.Clamp(frame, 0, LastFrame));
        }

        // 更新选中事件的接收类名称
        public void SetSelectedEventReceiverTypeName(string receiverTypeName)
        {
            if (SelectedEvent == null)
                return;

            SelectedEvent.SetReceiverTypeName(receiverTypeName);
        }

        // 更新选中事件的 Function 名称
        public void SetSelectedEventFunctionName(string functionName)
        {
            if (SelectedEvent == null)
                return;

            SelectedEvent.SetFunctionName(functionName);
        }

        // 在当前帧创建默认长度并选中的通用窗口
        public AbilityWindowDraft AddWindow(int frame)
        {
            int startFrame = Mathf.Clamp(frame, 0, LastFrame);
            return AddWindow(AbilityWindowDraftType.Hit, startFrame, Mathf.Clamp(startFrame + 1, 1, LastBoundaryFrame), 1f);
        }

        // 从外部能力数据加载一条窗口草稿
        public AbilityWindowDraft AddWindow(AbilityWindowDraftType type, int startFrame, int endFrame, float damage)
        {
            int clampedStartFrame = Mathf.Clamp(startFrame, 0, LastFrame);
            int clampedEndFrame = Mathf.Clamp(endFrame, clampedStartFrame + 1, LastBoundaryFrame);
            AbilityWindowDraft windowDraft = new AbilityWindowDraft(clampedStartFrame, clampedEndFrame);
            windowDraft.SetType(type);
            windowDraft.SetDamage(Mathf.Max(0f, damage));
            GetDraftList(type).Add(windowDraft);
            SelectedWindow = windowDraft;
            IsWindowInspectorActive = true;
            return windowDraft;
        }

        // 清空当前动画片段关联的窗口草稿
        public void ClearWindows()
        {
            m_hitWindowDraftValues.Clear();
            m_stepAdvanceWindowDraftValues.Clear();
            m_movementLockWindowDraftValues.Clear();
            m_vfxWindowDraftValues.Clear();
            m_audioWindowDraftValues.Clear();
            SelectedWindow = null;
            IsWindowInspectorActive = false;
        }

        // 取消当前窗口选中状态
        public void ClearWindowSelection()
        {
            SelectedWindow = null;
            IsWindowInspectorActive = false;
        }

        // 删除当前选中的窗口草稿
        public void DeleteSelectedWindow()
        {
            if (SelectedWindow == null)
                return;

            GetDraftList(SelectedWindow.Type).Remove(SelectedWindow);
            SelectedWindow = null;
            IsWindowInspectorActive = SelectedEvent == null;
        }

        // 按唯一标识选中指定窗口草稿
        public void SelectWindow(string windowId)
        {
            SelectedWindow = FindWindow(windowId);
            IsWindowInspectorActive = true;
        }

        // 更新选中窗口的业务类型
        public void SetSelectedWindowType(AbilityWindowDraftType type)
        {
            if (SelectedWindow == null)
                return;

            if (SelectedWindow.Type == type)
                return;

            GetDraftList(SelectedWindow.Type).Remove(SelectedWindow);
            SelectedWindow.SetType(type);
            GetDraftList(type).Add(SelectedWindow);
        }

        // 更新选中窗口的左右边界
        public void SetSelectedWindowFrames(int startFrame, int endFrame)
        {
            if (SelectedWindow == null)
                return;

            int clampedStartFrame = Mathf.Clamp(startFrame, 0, LastFrame);
            int clampedEndFrame = Mathf.Clamp(endFrame, clampedStartFrame + 1, LastBoundaryFrame);
            SelectedWindow.SetFrames(clampedStartFrame, clampedEndFrame);
        }

        // 更新选中命中窗口的伤害参数
        public void SetSelectedWindowDamage(float damage)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetDamage(Mathf.Max(0f, damage));
        }

        // 返回指定类型的窗口草稿列表
        private List<AbilityWindowDraft> GetDraftList(AbilityWindowDraftType type)
        {
            switch (type)
            {
                case AbilityWindowDraftType.Hit:
                    return m_hitWindowDraftValues;
                case AbilityWindowDraftType.StepAdvance:
                    return m_stepAdvanceWindowDraftValues;
                case AbilityWindowDraftType.MovementLock:
                    return m_movementLockWindowDraftValues;
                case AbilityWindowDraftType.Vfx:
                    return m_vfxWindowDraftValues;
                case AbilityWindowDraftType.Audio:
                    return m_audioWindowDraftValues;
                default:
                    QLog.Error($"未支持的 Ability 窗口草稿类型：{type}");
                    return m_hitWindowDraftValues;
            }
        }

        // 在三条独立轨道中查找窗口草稿
        private AbilityWindowDraft FindWindow(string windowId)
        {
            AbilityWindowDraft windowDraft = m_hitWindowDraftValues.Find(item => item.Id == windowId);
            windowDraft ??= m_stepAdvanceWindowDraftValues.Find(item => item.Id == windowId);
            windowDraft ??= m_movementLockWindowDraftValues.Find(item => item.Id == windowId);
            windowDraft ??= m_vfxWindowDraftValues.Find(item => item.Id == windowId);
            return windowDraft ?? m_audioWindowDraftValues.Find(item => item.Id == windowId);
        }

        // 返回全部窗口草稿供编辑器统一遍历
        public IEnumerable<AbilityWindowDraft> EnumerateWindows()
        {
            foreach (AbilityWindowDraft windowDraft in m_hitWindowDraftValues)
                yield return windowDraft;
            foreach (AbilityWindowDraft windowDraft in m_stepAdvanceWindowDraftValues)
                yield return windowDraft;
            foreach (AbilityWindowDraft windowDraft in m_movementLockWindowDraftValues)
                yield return windowDraft;
            foreach (AbilityWindowDraft windowDraft in m_vfxWindowDraftValues)
                yield return windowDraft;
            foreach (AbilityWindowDraft windowDraft in m_audioWindowDraftValues)
                yield return windowDraft;
        }

        // 更新选中特效窗口的触发方式
        public void SetSelectedWindowVfxTriggerType(AbilityVfxTriggerType triggerType)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetVfxTriggerType(triggerType);
        }

        // 更新选中特效窗口的生成目标
        public void SetSelectedWindowVfxTargetType(AbilityVfxTargetType targetType)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetVfxTargetType(targetType);
        }

        // 更新选中特效窗口的预制体
        public void SetSelectedWindowVfxPrefab(GameObject vfxPrefab)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetVfxPrefab(vfxPrefab);
        }

        // 更新选中特效窗口的挂点 Id
        public void SetSelectedWindowVfxSocketId(string socketId)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetVfxSocketId(socketId);
        }

        // 更新选中特效窗口的生命周期模式
        public void SetSelectedWindowVfxLifeMode(AbilityVfxLifeMode lifeMode)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetVfxLifeMode(lifeMode);
        }

        // 更新选中特效窗口的位置偏移
        public void SetSelectedWindowVfxLocalPositionOffset(Vector3 localPositionOffset)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetVfxLocalPositionOffset(localPositionOffset);
        }

        // 更新选中特效窗口的旋转偏移
        public void SetSelectedWindowVfxLocalEulerOffset(Vector3 localEulerOffset)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetVfxLocalEulerOffset(localEulerOffset);
        }

        // 更新选中特效窗口的跟随状态
        public void SetSelectedWindowVfxFollowTarget(bool followTarget)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetVfxFollowTarget(followTarget);
        }

        // 更新选中音效窗口的触发方式
        public void SetSelectedWindowAudioTriggerType(AbilityAudioTriggerType triggerType)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetAudioTriggerType(triggerType);
        }

        // 更新选中音效窗口的播放类型
        public void SetSelectedWindowAudioPlaybackType(AbilityAudioPlaybackType playbackType)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetAudioPlaybackType(playbackType);
        }

        // 更新选中音效窗口的资源槽位
        public void SetSelectedWindowAudioClip(int clipSlotIndex, AudioClip audioClip)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetAudioClip(clipSlotIndex, audioClip);
        }

        // 给选中音效窗口新增一个资源槽位
        public void AddSelectedWindowAudioClip()
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.AddAudioClip(null);
        }

        // 删除选中音效窗口的资源槽位
        public void RemoveSelectedWindowAudioClip(int clipSlotIndex)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.RemoveAudioClip(clipSlotIndex);
        }

        // 更新选中音效窗口的音量
        public void SetSelectedWindowAudioVolume(float volume)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetAudioVolume(Mathf.Clamp01(volume));
        }

        // 更新选中音效窗口的音高
        public void SetSelectedWindowAudioPitch(float pitch)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetAudioPitch(Mathf.Max(0.1f, pitch));
        }

        // 更新选中音效窗口的随机音高范围
        public void SetSelectedWindowAudioRandomPitchRange(float randomPitchRange)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetAudioRandomPitchRange(Mathf.Clamp01(randomPitchRange));
        }

        // 更新选中音效窗口的空间化状态
        public void SetSelectedWindowAudioSpatial(bool spatial)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetAudioSpatial(spatial);
        }

        // 更新选中音效窗口的窗口结束截断状态
        public void SetSelectedWindowAudioStopOnWindowEnd(bool stopOnWindowEnd)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetAudioStopOnWindowEnd(stopOnWindowEnd);
        }

        // 更新选中音效窗口的播放目标
        public void SetSelectedWindowAudioTargetType(AbilityAudioTargetType targetType)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetAudioTargetType(targetType);
        }

        // 更新选中音效窗口的挂点 Id
        public void SetSelectedWindowAudioSocketId(string socketId)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetAudioSocketId(socketId);
        }

        // 更新选中音效窗口的位置偏移
        public void SetSelectedWindowAudioLocalPositionOffset(Vector3 localPositionOffset)
        {
            if (SelectedWindow == null)
                return;

            SelectedWindow.SetAudioLocalPositionOffset(localPositionOffset);
        }
    }
}
