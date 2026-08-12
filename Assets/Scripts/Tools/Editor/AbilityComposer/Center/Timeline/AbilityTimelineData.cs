/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 时间轴数据，保存动画帧与播放状态
 * │  类    名: AbilityTimelineData.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Tools.Editor.AbilityComposer.Center.Event;
using UnityEngine;

namespace Tools.Editor.AbilityComposer.Center.Timeline
{
    public sealed class AbilityTimelineData
    {
        private readonly List<AbilityEventDraft> m_eventDraftValues = new List<AbilityEventDraft>();

        public AnimationClip Clip { get; private set; }
        public float FrameRate { get; private set; }
        public int FrameCount { get; private set; }
        public int CurrentFrame { get; private set; }
        public bool IsPlaying { get; private set; }
        public IReadOnlyList<AbilityEventDraft> EventDraftValues => m_eventDraftValues;
        public AbilityEventDraft SelectedEvent { get; private set; }
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
        }

        // 更新选中事件的编辑器分类
        public void SetSelectedEventCategory(AbilityEventCategory category)
        {
            if (SelectedEvent == null)
                return;

            SelectedEvent.SetCategory(category);
        }

        // 更新选中事件的 Function 名称
        public void SetSelectedEventFunctionName(string functionName)
        {
            if (SelectedEvent == null)
                return;

            SelectedEvent.SetFunctionName(functionName);
        }
    }
}
