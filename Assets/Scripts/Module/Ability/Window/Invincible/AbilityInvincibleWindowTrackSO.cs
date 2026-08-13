/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 无敌窗口轨道，保存一个动画的无敌窗口集合 │
 * │  类    名: AbilityInvincibleWindowTrackSO.cs                │
 * │  创    建: By qiqizizzz                                    │
 * └─────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using UnityEngine;

namespace Module.Ability.Window.Invincible
{
    [CreateAssetMenu(fileName = "AbilityInvincibleWindowTrack", menuName = "配置/Ability/窗口/无敌")]
    public sealed class AbilityInvincibleWindowTrackSO : AbilityWindowTrackSO
    {
        [SerializeField] private List<AbilityInvincibleWindowData> WindowValues = new List<AbilityInvincibleWindowData>();

        public IReadOnlyList<AbilityInvincibleWindowData> Windows => WindowValues;

        // 使用编辑器提交的无敌窗口替换轨道数据
        public void SetWindows(IReadOnlyList<AbilityInvincibleWindowData> windows)
        {
            WindowValues.Clear();
            for (int windowIndex = 0; windowIndex < windows.Count; windowIndex++)
            {
                AbilityInvincibleWindowData window = windows[windowIndex];
                WindowValues.Add(new AbilityInvincibleWindowData(window.Id, window.StartNormalizedTime,
                    window.EndNormalizedTime));
            }
        }
    }
}
