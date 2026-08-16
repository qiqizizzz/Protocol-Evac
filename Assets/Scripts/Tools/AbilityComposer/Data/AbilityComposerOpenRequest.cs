/*
 * ┌────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 打开请求，传递待编辑的动画片段
 * │  类    名: AbilityComposerOpenRequest.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────────────────────┘
 */

using UnityEngine;
using Module.Ability.Data.Window;

namespace Module.Ability.Data
{
    public static class AbilityComposerOpenRequest
    {
        private static AnimationClip m_requestedAnimationClip;
        private static AbilityWindowConfigSO m_requestedWindowConfig;

        public static AnimationClip RequestedAnimationClip => m_requestedAnimationClip;
        public static AbilityWindowConfigSO RequestedWindowConfig => m_requestedWindowConfig;

        // 设置下一次 Ability Composer 打开时需要编辑的动画
        public static void SetAnimationClip(AnimationClip animationClip)
        {
            m_requestedAnimationClip = animationClip;
            m_requestedWindowConfig = null;
        }

        // 设置下一次 Ability Composer 打开时需要编辑的窗口主体配置
        public static void SetWindowConfig(AbilityWindowConfigSO windowConfig)
        {
            m_requestedWindowConfig = windowConfig;
            m_requestedAnimationClip = windowConfig == null ? null : windowConfig.AnimationClip;
        }

        // 读取并清除待编辑动画请求
        public static AnimationClip ConsumeAnimationClip()
        {
            AnimationClip animationClip = m_requestedAnimationClip;
            m_requestedAnimationClip = null;
            return animationClip;
        }

        // 读取并清除待编辑窗口主体配置请求
        public static AbilityWindowConfigSO ConsumeWindowConfig()
        {
            AbilityWindowConfigSO windowConfig = m_requestedWindowConfig;
            m_requestedWindowConfig = null;
            return windowConfig;
        }
    }
}
