/*
 * ┌───────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 时间轴数据，保存动画帧、事件草稿与分类窗口草稿
 * │  类    名: AbilityTimelineData.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Ability.Data.Window.Hit;
using Module.Ability.Data.Window.StepAdvance;
using Tools.AbilityComposer.Editor.View.Center.Event;
using UnityEngine;

namespace Tools.AbilityComposer.Editor.View.Center.Timeline
{
    public sealed class AbilityTimelineData
    {
        private readonly List<AbilityEventDraft> m_eventDraftValues = new List<AbilityEventDraft>();
        private readonly List<AbilityWindowDraft> m_hitWindowDraftValues = new List<AbilityWindowDraft>();
        private readonly List<AbilityWindowDraft> m_stepAdvanceWindowDraftValues = new List<AbilityWindowDraft>();

        public AnimationClip Clip { get; private set; }
        public float FrameRate { get; private set; }
        public int FrameCount { get; private set; }
        public int CurrentFrame { get; private set; }
        public bool IsPlaying { get; private set; }
        public IReadOnlyList<AbilityEventDraft> EventDraftValues => m_eventDraftValues;
        public IReadOnlyList<AbilityWindowDraft> HitWindowDraftValues => m_hitWindowDraftValues;
        public IReadOnlyList<AbilityWindowDraft> StepAdvanceWindowDraftValues => m_stepAdvanceWindowDraftValues;
        public AbilityEventDraft SelectedEvent { get; private set; }
        public AbilityWindowDraft SelectedWindow { get; private set; }
        public AbilityHitWindowTrackSO HitWindowTrack { get; private set; }
        public AbilityStepAdvanceWindowTrackSO StepAdvanceWindowTrack { get; private set; }
        public bool IsWindowInspectorActive { get; private set; }
        public bool IsHitWindowTrackEnabled { get; private set; } = true;
        public bool IsStepAdvanceWindowTrackEnabled { get; private set; } = true;
        public bool HasClip => Clip != null;
        public int LastFrame => Mathf.Max(FrameCount - 1, 0);
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
            SelectedEvent = null;
            SelectedWindow = null;
            IsWindowInspectorActive = false;
        }

        // 更新当前编辑的窗口轨道资产
        public void SetWindowTracks(AbilityHitWindowTrackSO hitWindowTrack, AbilityStepAdvanceWindowTrackSO stepAdvanceWindowTrack)
        {
            HitWindowTrack = hitWindowTrack;
            StepAdvanceWindowTrack = stepAdvanceWindowTrack;
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
            return AddWindow(AbilityWindowDraftType.Hit, startFrame, Mathf.Clamp(startFrame + 1, 0, LastFrame), 1f);
        }

        // 从外部能力数据加载一条窗口草稿
        public AbilityWindowDraft AddWindow(AbilityWindowDraftType type, int startFrame, int endFrame, float damage)
        {
            int clampedStartFrame = Mathf.Clamp(startFrame, 0, LastFrame);
            int clampedEndFrame = Mathf.Clamp(endFrame, clampedStartFrame, LastFrame);
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
            int clampedEndFrame = Mathf.Clamp(endFrame, clampedStartFrame, LastFrame);
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
            return type == AbilityWindowDraftType.Hit ? m_hitWindowDraftValues : m_stepAdvanceWindowDraftValues;
        }

        // 在两条独立轨道中查找窗口草稿
        private AbilityWindowDraft FindWindow(string windowId)
        {
            AbilityWindowDraft windowDraft = m_hitWindowDraftValues.Find(item => item.Id == windowId);
            return windowDraft ?? m_stepAdvanceWindowDraftValues.Find(item => item.Id == windowId);
        }

        // 返回全部窗口草稿供编辑器统一遍历
        public IEnumerable<AbilityWindowDraft> EnumerateWindows()
        {
            foreach (AbilityWindowDraft windowDraft in m_hitWindowDraftValues)
                yield return windowDraft;
            foreach (AbilityWindowDraft windowDraft in m_stepAdvanceWindowDraftValues)
                yield return windowDraft;
        }
    }
}
