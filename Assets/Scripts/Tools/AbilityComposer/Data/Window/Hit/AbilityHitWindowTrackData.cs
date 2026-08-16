/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 命中窗口轨道数据，保存一个动画的命中窗口集合
 * │  类    名: AbilityHitWindowTrackData.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Module.Ability.Data.Window.Hit
{
    [Serializable]
    public sealed class AbilityHitWindowTrackData
    {
        [SerializeField] private List<AbilityHitWindowData> WindowValues = new List<AbilityHitWindowData>();

        public IReadOnlyList<AbilityHitWindowData> Windows => WindowValues;
        public int WindowCount => WindowValues.Count;

        // 使用编辑器提交的命中窗口替换轨道数据
        public void SetWindows(IReadOnlyList<AbilityHitWindowData> windows)
        {
            WindowValues.Clear();
            for (int windowIndex = 0; windowIndex < windows.Count; windowIndex++)
            {
                AbilityHitWindowData window = windows[windowIndex];
                WindowValues.Add(new AbilityHitWindowData(window.Id, window.StartNormalizedTime,
                    window.EndNormalizedTime, window.Damage, window.ReactionType));
            }
        }

        // 查找指定时间处于活动状态的第一个命中窗口
        public bool TryGetActiveWindow(float normalizedTime, out AbilityHitWindowData activeWindow)
        {
            for (int windowIndex = 0; windowIndex < WindowValues.Count; windowIndex++)
            {
                AbilityHitWindowData window = WindowValues[windowIndex];
                if (!window.IsActiveAt(normalizedTime))
                    continue;

                activeWindow = window;
                return true;
            }

            activeWindow = null;
            return false;
        }
    }
}

