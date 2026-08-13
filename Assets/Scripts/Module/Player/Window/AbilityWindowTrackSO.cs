/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 窗口轨道配置，保存一个动画片段的通用时间窗口集合
 * │  类    名: AbilityWindowTrackSO.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using UnityEngine;

namespace Module.Player.Window
{
    [CreateAssetMenu(fileName = "AbilityWindowTrack", menuName = "配置/Ability/窗口轨道")]
    public sealed class AbilityWindowTrackSO : ScriptableObject
    {
        [SerializeField] private AnimationClip AnimationClipValue;
        [SerializeField] private List<AbilityWindowData> WindowValues = new List<AbilityWindowData>();

        public AnimationClip AnimationClip => AnimationClipValue;
        public IReadOnlyList<AbilityWindowData> Windows => WindowValues;

        // 更新该轨道对应的动画片段
        public void SetAnimationClip(AnimationClip animationClip)
        {
            AnimationClipValue = animationClip;
        }

        // 使用编辑器提交的窗口数据替换当前轨道
        public void SetWindows(IReadOnlyList<AbilityWindowData> windows)
        {
            WindowValues.Clear();
            for (int windowIndex = 0; windowIndex < windows.Count; windowIndex++)
            {
                AbilityWindowData windowData = windows[windowIndex];
                WindowValues.Add(new AbilityWindowData(windowData.Type, windowData.StartNormalizedTime,
                    windowData.EndNormalizedTime, windowData.Damage));
            }
        }
    }
}
