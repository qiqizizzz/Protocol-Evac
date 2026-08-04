/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家状态动画段落数据，保存动画片段与持续时间
 * │  类    名: PlayerStateClipData.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System;
using TriInspector;
using UnityEngine;

namespace Module.Player.HFSM.Config.Common
{
    [Serializable]
    [DeclareFoldoutGroup("Animation", Title = "动画与段落", Expanded = true)]
    [DeclareFoldoutGroup("ComboWindow", Title = "连段窗口", Expanded = true)]
    public sealed class PlayerStateClipData
    {
        [Group("Animation")]
        [LabelText("动画片段")]
        [Tooltip("状态动画片段")]
        [SerializeField] private AnimationClip StateClipValue;

        [Group("Animation")]
        [LabelText("持续时间")]
        [Tooltip("状态持续时间")]
        [SerializeField, Min(0f)] private float StateDurationValue = 0.5f;

        [Group("ComboWindow")]
        [LabelText("启用连段窗口")]
        [Tooltip("是否启用连段窗口")]
        [SerializeField] private bool UseComboWindowValue;

        [Group("ComboWindow")]
        [ShowIf(nameof(UseComboWindowValue))]
        [LabelText("开始时间")]
        [Tooltip("连段窗口开始时间（归一化）")]
        [SerializeField, Range(0f, 1f)] private float ComboOpenNormalizedTimeValue = 0.35f;

        [Group("ComboWindow")]
        [ShowIf(nameof(UseComboWindowValue))]
        [LabelText("结束时间")]
        [Tooltip("连段窗口结束时间（归一化）")]
        [SerializeField, Range(0f, 1f)] private float ComboCloseNormalizedTimeValue = 0.75f;

        public AnimationClip StateClip => StateClipValue;

        public float StateDuration => StateDurationValue;

        public bool UseComboWindow => UseComboWindowValue;

        public float ComboOpenNormalizedTime => ComboOpenNormalizedTimeValue;

        public float ComboCloseNormalizedTime => ComboCloseNormalizedTimeValue;

        // 从动画片段同步状态持续时间
        public bool SyncDurationFromClip()
        {
            if (StateClipValue == null)
                return false;

            StateDurationValue = StateClipValue.length;
            return true;
        }

        // 尝试读取连段窗口
        public bool TryGetComboWindow(out float comboOpenNormalizedTime, out float comboCloseNormalizedTime)
        {
            comboOpenNormalizedTime = ComboOpenNormalizedTimeValue;
            comboCloseNormalizedTime = ComboCloseNormalizedTimeValue;

            if (!UseComboWindowValue)
                return false;

            comboOpenNormalizedTime = Mathf.Clamp01(comboOpenNormalizedTime);
            comboCloseNormalizedTime = Mathf.Clamp01(comboCloseNormalizedTime);
            if (comboCloseNormalizedTime < comboOpenNormalizedTime)
                comboCloseNormalizedTime = comboOpenNormalizedTime;

            return true;
        }
    }
}
