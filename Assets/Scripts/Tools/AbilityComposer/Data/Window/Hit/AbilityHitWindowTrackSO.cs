/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 命中窗口轨道，保存一个动画的命中窗口集合
 * │  类    名: AbilityHitWindowTrackSO.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using UnityEngine;

namespace Module.Ability.Data.Window.Hit
{
    [CreateAssetMenu(fileName = "AbilityHitWindowTrack", menuName = "配置/Ability/窗口/命中")]
    public sealed class AbilityHitWindowTrackSO : AbilityWindowTrackBaseSO
    {
        [SerializeField] private List<AbilityHitWindowData> WindowValues = new List<AbilityHitWindowData>();

        protected override IReadOnlyList<AbilityWindowDataBase> WindowDataValues => WindowValues;

        public IReadOnlyList<AbilityHitWindowData> Windows => WindowValues;

        // 使用编辑器提交的命中窗口替换轨道数据
        public void SetWindows(IReadOnlyList<AbilityHitWindowData> windows)
        {
            WindowValues.Clear();
            for (int windowIndex = 0; windowIndex < windows.Count; windowIndex++)
            {
                AbilityHitWindowData window = windows[windowIndex];
                WindowValues.Add(new AbilityHitWindowData(window.Id, window.StartNormalizedTime,
                    window.EndNormalizedTime, window.Damage));
            }
        }
    }
}
