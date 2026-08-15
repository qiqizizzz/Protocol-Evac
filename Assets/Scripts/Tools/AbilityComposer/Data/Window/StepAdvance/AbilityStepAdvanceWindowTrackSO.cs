/*
 * ┌───────────────────────────────────────────────────────────┐
 * │  描    述: Ability 阶段推进窗口轨道，保存阶段推进窗口集合
 * │  类    名: AbilityStepAdvanceWindowTrackSO.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using UnityEngine;

namespace Module.Ability.Data.Window.StepAdvance
{
    [CreateAssetMenu(fileName = "AbilityStepAdvanceWindowTrack", menuName = "配置/Ability/窗口/阶段推进")]
    public sealed class AbilityStepAdvanceWindowTrackSO : AbilityWindowTrackBaseSO
    {
        [SerializeField] private List<AbilityStepAdvanceWindowData> WindowValues = new List<AbilityStepAdvanceWindowData>();

        protected override IReadOnlyList<AbilityWindowDataBase> WindowDataValues => WindowValues;

        public IReadOnlyList<AbilityStepAdvanceWindowData> Windows => WindowValues;

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
    }
}
