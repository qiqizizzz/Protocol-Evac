/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 移动锁定窗口轨道，保存禁止移动的窗口集合
 * │  类    名: AbilityMovementLockWindowTrackSO.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using UnityEngine;

namespace Module.Ability.Data.Window.MovementLock
{
    [CreateAssetMenu(fileName = "AbilityMovementLockWindowTrack", menuName = "配置/Ability/窗口/移动锁定")]
    public sealed class AbilityMovementLockWindowTrackSO : AbilityWindowTrackBaseSO
    {
        [SerializeField] private List<AbilityMovementLockWindowData> WindowValues = new List<AbilityMovementLockWindowData>();

        protected override IReadOnlyList<AbilityWindowDataBase> WindowDataValues => WindowValues;

        public IReadOnlyList<AbilityMovementLockWindowData> Windows => WindowValues;

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
    }
}

