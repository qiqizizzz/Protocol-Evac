/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 打开请求，传递待编辑的动画片段 │
 * │  类    名: AbilityComposerOpenRequest.cs                    │
 * │  创    建: By qiqizizzz                                    │
 * └─────────────────────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Ability.Window
{
    public static class AbilityComposerOpenRequest
    {
        private static AnimationClip m_requestedAnimationClip;

        public static AnimationClip RequestedAnimationClip => m_requestedAnimationClip;

        // 设置下一次 Ability Composer 打开时需要编辑的动画
        public static void SetAnimationClip(AnimationClip animationClip)
        {
            m_requestedAnimationClip = animationClip;
        }

        // 读取并清除待编辑动画请求
        public static AnimationClip ConsumeAnimationClip()
        {
            AnimationClip animationClip = m_requestedAnimationClip;
            m_requestedAnimationClip = null;
            return animationClip;
        }
    }
}

