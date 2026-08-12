/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 时间轴数据，保存动画帧与播放状态
 * │  类    名: AbilityTimelineData.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Tools.Editor.AbilityComposer.Center.Timeline
{
    public sealed class AbilityTimelineData
    {
        public AnimationClip Clip { get; private set; }
        public float FrameRate { get; private set; }
        public int FrameCount { get; private set; }
        public int CurrentFrame { get; private set; }
        public bool IsPlaying { get; private set; }
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
    }
}
