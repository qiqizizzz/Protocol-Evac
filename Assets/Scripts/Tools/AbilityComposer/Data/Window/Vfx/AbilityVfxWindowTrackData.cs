/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 特效窗口轨道数据，保存一个动画的特效窗口集合
 * │  类    名: AbilityVfxWindowTrackData.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Module.Ability.Data.Window.Vfx
{
    [Serializable]
    public sealed class AbilityVfxWindowTrackData
    {
        [SerializeField] private List<AbilityVfxWindowData> WindowValues = new List<AbilityVfxWindowData>();

        public IReadOnlyList<AbilityVfxWindowData> Windows => WindowValues;
        public int WindowCount => WindowValues.Count;

        // 使用编辑器提交的特效窗口替换轨道数据
        public void SetWindows(IReadOnlyList<AbilityVfxWindowData> windows)
        {
            WindowValues.Clear();
            for (int windowIndex = 0; windowIndex < windows.Count; windowIndex++)
            {
                AbilityVfxWindowData window = windows[windowIndex];
                WindowValues.Add(new AbilityVfxWindowData(window.Id, window.StartNormalizedTime,
                    window.EndNormalizedTime, window.TriggerType, window.TargetType, window.VfxPrefab,
                    window.SocketId, window.LifeMode, window.LocalPositionOffset, window.LocalEulerOffset,
                    window.FollowTarget));
            }
        }

        // 查找指定时间处于活动状态的全部特效窗口
        public void GetActiveWindows(float normalizedTime, List<AbilityVfxWindowData> results)
        {
            results.Clear();
            for (int windowIndex = 0; windowIndex < WindowValues.Count; windowIndex++)
            {
                AbilityVfxWindowData window = WindowValues[windowIndex];
                if (window.IsActiveAt(normalizedTime))
                    results.Add(window);
            }
        }
    }
}

