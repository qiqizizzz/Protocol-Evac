/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家技能段落数据，保存动画、窗口与伤害参数
 * │  类    名: PlayerSkillStepData.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System;
using TriInspector;
using UnityEngine;

namespace Module.Player.Skill.Data
{
    [Serializable]
    [DeclareFoldoutGroup("Animation", Title = "动画与段落", Expanded = true)]
    [DeclareFoldoutGroup("StepAdvanceWindow", Title = "段落推进窗口", Expanded = true)]
    [DeclareFoldoutGroup("HitWindow", Title = "命中窗口", Expanded = true)]
    public sealed class PlayerSkillStepData
    {
        [Group("Animation")]
        [LabelText("动画片段")]
        [Tooltip("技能段落动画片段")]
        [SerializeField] private AnimationClip AnimationClipValue;

        [Group("Animation")]
        [LabelText("持续时间")]
        [Tooltip("技能段落持续时间")]
        [SerializeField, Min(0f)] private float DurationValue = 0.5f;

        [Group("Animation")]
        [LabelText("使用 Root Motion")]
        [Tooltip("是否使用动画根运动位移")]
        [SerializeField] private bool UseRootMotionValue;

        [Group("StepAdvanceWindow")]
        [LabelText("启用推进窗口")]
        [Tooltip("是否启用下一段推进窗口")]
        [SerializeField] private bool UseStepAdvanceWindowValue;

        [Group("StepAdvanceWindow")]
        [ShowIf(nameof(UseStepAdvanceWindowValue))]
        [LabelText("开始时间")]
        [Tooltip("下一段推进窗口开始时间（归一化）")]
        [SerializeField, Range(0f, 1f)] private float StepAdvanceOpenNormalizedTimeValue = 0.35f;

        [Group("StepAdvanceWindow")]
        [ShowIf(nameof(UseStepAdvanceWindowValue))]
        [LabelText("结束时间")]
        [Tooltip("下一段推进窗口结束时间（归一化）")]
        [SerializeField, Range(0f, 1f)] private float StepAdvanceCloseNormalizedTimeValue = 0.75f;

        [Group("HitWindow")]
        [LabelText("启用命中窗口")]
        [Tooltip("是否启用命中窗口")]
        [SerializeField] private bool UseHitWindowValue;

        [Group("HitWindow")]
        [ShowIf(nameof(UseHitWindowValue))]
        [LabelText("开始时间")]
        [Tooltip("命中窗口开始时间（归一化）")]
        [SerializeField, Range(0f, 1f)] private float HitOpenNormalizedTimeValue;

        [Group("HitWindow")]
        [ShowIf(nameof(UseHitWindowValue))]
        [LabelText("结束时间")]
        [Tooltip("命中窗口结束时间（归一化）")]
        [SerializeField, Range(0f, 1f)] private float HitCloseNormalizedTimeValue;

        [Group("HitWindow")]
        [ShowIf(nameof(UseHitWindowValue))]
        [LabelText("伤害")]
        [Tooltip("该段技能造成的伤害")]
        [SerializeField, Min(0f)] private float DamageValue;

        public AnimationClip AnimationClip => AnimationClipValue;

        public float Duration => DurationValue;

        public bool UseRootMotion => UseRootMotionValue;

        public bool UseStepAdvanceWindow => UseStepAdvanceWindowValue;

        public bool UseHitWindow => UseHitWindowValue;

        public float Damage => DamageValue;

        // 从动画片段同步技能段落持续时间
        public bool SyncDurationFromClip()
        {
            if (AnimationClipValue == null)
                return false;

            DurationValue = AnimationClipValue.length;
            return true;
        }

        // 尝试读取下一段推进窗口
        public bool TryGetStepAdvanceWindow(out float openNormalizedTime, out float closeNormalizedTime)
        {
            openNormalizedTime = StepAdvanceOpenNormalizedTimeValue;
            closeNormalizedTime = StepAdvanceCloseNormalizedTimeValue;

            if (!UseStepAdvanceWindowValue)
                return false;

            NormalizeWindow(ref openNormalizedTime, ref closeNormalizedTime);
            return true;
        }

        // 尝试读取命中窗口
        public bool TryGetHitWindow(out float openNormalizedTime, out float closeNormalizedTime)
        {
            openNormalizedTime = HitOpenNormalizedTimeValue;
            closeNormalizedTime = HitCloseNormalizedTimeValue;

            if (!UseHitWindowValue)
                return false;

            NormalizeWindow(ref openNormalizedTime, ref closeNormalizedTime);
            return true;
        }

        // 约束归一化窗口并保证结束时间不早于开始时间
        private void NormalizeWindow(ref float openNormalizedTime, ref float closeNormalizedTime)
        {
            openNormalizedTime = Mathf.Clamp01(openNormalizedTime);
            closeNormalizedTime = Mathf.Clamp01(closeNormalizedTime);
            if (closeNormalizedTime < openNormalizedTime)
                closeNormalizedTime = openNormalizedTime;
        }
    }
}
