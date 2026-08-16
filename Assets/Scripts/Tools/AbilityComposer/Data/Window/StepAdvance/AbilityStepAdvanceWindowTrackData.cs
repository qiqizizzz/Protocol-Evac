/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 阶段推进窗口轨道数据，保存阶段推进窗口集合
 * │  类    名: AbilityStepAdvanceWindowTrackData.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Module.Ability.Data.Window.StepAdvance
{
    [Serializable]
    public sealed class AbilityStepAdvanceWindowTrackData
    {
        [SerializeField] private List<AbilityStepAdvanceWindowData> WindowValues = new List<AbilityStepAdvanceWindowData>();

        public IReadOnlyList<AbilityStepAdvanceWindowData> Windows => WindowValues;
        public int WindowCount => WindowValues.Count;

        // 使用编辑器提交的阶段推进窗口替换轨道数据
        public void SetWindows(IReadOnlyList<AbilityStepAdvanceWindowData> windows)
        {
            WindowValues.Clear();
            for (int windowIndex = 0; windowIndex < windows.Count; windowIndex++)
            {
                AbilityStepAdvanceWindowData window = windows[windowIndex];
                WindowValues.Add(new AbilityStepAdvanceWindowData(window.Id, window.StartNormalizedTime,
                    window.EndNormalizedTime));
            }
        }

        // 查找指定时间处于活动状态的第一个阶段推进窗口
        public bool TryGetActiveWindow(float normalizedTime, out AbilityStepAdvanceWindowData activeWindow)
        {
            for (int windowIndex = 0; windowIndex < WindowValues.Count; windowIndex++)
            {
                AbilityStepAdvanceWindowData window = WindowValues[windowIndex];
                if (!window.IsActiveAt(normalizedTime))
                    continue;

                activeWindow = window;
                return true;
            }

            activeWindow = null;
            return false;
        }

        // 查找时间推进时跨过的第一个阶段推进窗口
        public bool TryGetCrossedWindow(float previousNormalizedTime, float currentNormalizedTime,
            out AbilityStepAdvanceWindowData crossedWindow)
        {
            for (int windowIndex = 0; windowIndex < WindowValues.Count; windowIndex++)
            {
                AbilityStepAdvanceWindowData window = WindowValues[windowIndex];
                if (!window.IsCrossedBy(previousNormalizedTime, currentNormalizedTime))
                    continue;

                crossedWindow = window;
                return true;
            }

            crossedWindow = null;
            return false;
        }

        // 判断指定时间之后是否仍存在可等待的阶段推进窗口
        public bool HasWindowAtOrAfter(float normalizedTime)
        {
            for (int windowIndex = 0; windowIndex < WindowValues.Count; windowIndex++)
            {
                if (WindowValues[windowIndex].EndNormalizedTime >= normalizedTime)
                    return true;
            }

            return false;
        }
    }
}

