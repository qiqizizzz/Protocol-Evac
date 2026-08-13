/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 窗口轨道基类，保存轨道绑定的动画片段      │
 * │  类    名: AbilityWindowTrackSO.cs                          │
 * │  创    建: By qiqizizzz                                    │
 * └─────────────────────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Ability.Window
{
    public abstract class AbilityWindowTrackSO : ScriptableObject
    {
        [SerializeField] private AnimationClip AnimationClipValue;

        public AnimationClip AnimationClip => AnimationClipValue;

        // 更新轨道绑定的动画片段
        public void SetAnimationClip(AnimationClip animationClip)
        {
            AnimationClipValue = animationClip;
        }
    }
}
