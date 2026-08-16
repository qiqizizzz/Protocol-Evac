/*
 * ┌─────────────────────────────────────────────────────────┐
 * │  描    述: Ability 窗口轨道基类，保存轨道绑定的动画片段
 * │  类    名: AbilityWindowTrackBaseSO.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using UnityEngine;

namespace Module.Ability.Data.Window
{
    public abstract class AbilityWindowTrackBaseSO : ScriptableObject
    {
        [HideInInspector, SerializeField] private AnimationClip AnimationClipValue;

        protected abstract IReadOnlyList<AbilityWindowDataBase> WindowDataValues { get; }

        public AnimationClip AnimationClip => AnimationClipValue;

        public int WindowCount => WindowDataValues.Count;

        // 更新轨道绑定的动画片段
        public void SetAnimationClip(AnimationClip animationClip)
        {
            AnimationClipValue = animationClip;
        }

        // 查找指定时间处于活动状态的第一个窗口
        public bool TryGetActiveWindow<TWindow>(float normalizedTime, out TWindow activeWindow)
            where TWindow : AbilityWindowDataBase
        {
            for (int windowIndex = 0; windowIndex < WindowDataValues.Count; windowIndex++)
            {
                AbilityWindowDataBase window = WindowDataValues[windowIndex];
                if (!window.IsActiveAt(normalizedTime))
                    continue;

                activeWindow = window as TWindow;
                return activeWindow != null;
            }

            activeWindow = null;
            return false;
        }

        // 查找时间推进时跨过的第一个窗口
        public bool TryGetCrossedWindow<TWindow>(float previousNormalizedTime, float currentNormalizedTime, out TWindow crossedWindow)
            where TWindow : AbilityWindowDataBase
        {
            for (int windowIndex = 0; windowIndex < WindowDataValues.Count; windowIndex++)
            {
                AbilityWindowDataBase window = WindowDataValues[windowIndex];
                if (!window.IsCrossedBy(previousNormalizedTime, currentNormalizedTime))
                    continue;

                crossedWindow = window as TWindow;
                return crossedWindow != null;
            }

            crossedWindow = null;
            return false;
        }

        // 判断指定时间之后是否仍存在可等待的窗口
        public bool HasWindowAtOrAfter(float normalizedTime)
        {
            for (int windowIndex = 0; windowIndex < WindowDataValues.Count; windowIndex++)
            {
                if (WindowDataValues[windowIndex].EndNormalizedTime >= normalizedTime)
                    return true;
            }

            return false;
        }

        // 返回轨道内所有窗口最晚的结束时间
        public float GetLatestEndNormalizedTime()
        {
            float latestEndNormalizedTime = 0f;
            for (int windowIndex = 0; windowIndex < WindowDataValues.Count; windowIndex++)
                latestEndNormalizedTime = Mathf.Max(latestEndNormalizedTime,
                    WindowDataValues[windowIndex].EndNormalizedTime);

            return latestEndNormalizedTime;
        }
    }
}
