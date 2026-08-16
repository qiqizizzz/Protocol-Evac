/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 移动锁定窗口轨道数据，保存禁止移动的时间区间集合
 * │  类    名: AbilityMovementLockWindowTrackData.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Module.Ability.Data.Window.MovementLock
{
    [Serializable]
    public sealed class AbilityMovementLockWindowTrackData
    {
        [SerializeField] private List<AbilityMovementLockWindowData> WindowValues = new List<AbilityMovementLockWindowData>();

        public IReadOnlyList<AbilityMovementLockWindowData> Windows => WindowValues;
        public int WindowCount => WindowValues.Count;

        // 使用编辑器提交的移动锁定窗口替换轨道数据
        public void SetWindows(IReadOnlyList<AbilityMovementLockWindowData> windows)
        {
            WindowValues.Clear();
            for (int windowIndex = 0; windowIndex < windows.Count; windowIndex++)
            {
                AbilityMovementLockWindowData window = windows[windowIndex];
                WindowValues.Add(new AbilityMovementLockWindowData(window.Id, window.StartNormalizedTime,
                    window.EndNormalizedTime));
            }
        }

        // 返回轨道内所有移动锁定窗口最晚的结束时间
        public float GetLatestEndNormalizedTime()
        {
            float latestEndNormalizedTime = 0f;
            for (int windowIndex = 0; windowIndex < WindowValues.Count; windowIndex++)
                latestEndNormalizedTime = Mathf.Max(latestEndNormalizedTime,
                    WindowValues[windowIndex].EndNormalizedTime);

            return latestEndNormalizedTime;
        }
    }
}

