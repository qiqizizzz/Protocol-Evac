/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家技能段落数据，保存动画、窗口与伤害参数
 * │  类    名: PlayerSkillStepData.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System;
using NaughtyAttributes;
using UnityEngine;

namespace Module.Player.Skill.Data
{
    [Serializable]
    public sealed class PlayerSkillStepData
    {
        [Tooltip("技能段落动画片段")]
        [SerializeField] private AnimationClip AnimationClipValue;

        [Tooltip("技能段落持续时间")]
        [SerializeField, Min(0f)] private float DurationValue = 0.5f;

        [Tooltip("是否使用动画根运动位移")]
        [SerializeField] private bool UseRootMotionValue;

        [Tooltip("是否启用下一段推进窗口")]
        [SerializeField] private bool UseStepAdvanceWindowValue;

        [ShowIf(nameof(UseStepAdvanceWindowValue))]
        [AllowNesting]
        [Tooltip("下一段推进窗口开始时间（归一化）")]
        [SerializeField, Range(0f, 1f)] private float StepAdvanceOpenNormalizedTimeValue = 0.35f;

        [ShowIf(nameof(UseStepAdvanceWindowValue))]
        [AllowNesting]
        [Tooltip("下一段推进窗口结束时间（归一化）")]
        [SerializeField, Range(0f, 1f)] private float StepAdvanceCloseNormalizedTimeValue = 0.75f;

        [Tooltip("是否启用命中窗口")]
        [SerializeField] private bool UseHitWindowValue;

        [ShowIf(nameof(UseHitWindowValue))]
        [AllowNesting]
        [Tooltip("命中窗口开始时间（归一化）")]
        [SerializeField, Range(0f, 1f)] private float HitOpenNormalizedTimeValue;

        [ShowIf(nameof(UseHitWindowValue))]
        [AllowNesting]
        [Tooltip("命中窗口结束时间（归一化）")]
        [SerializeField, Range(0f, 1f)] private float HitCloseNormalizedTimeValue;

        [ShowIf(nameof(UseHitWindowValue))]
        [AllowNesting]
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

            normalizeWindow(ref openNormalizedTime, ref closeNormalizedTime);
            return true;
        }

        // 尝试读取命中窗口
        public bool TryGetHitWindow(out float openNormalizedTime, out float closeNormalizedTime)
        {
            openNormalizedTime = HitOpenNormalizedTimeValue;
            closeNormalizedTime = HitCloseNormalizedTimeValue;

            if (!UseHitWindowValue)
                return false;

            normalizeWindow(ref openNormalizedTime, ref closeNormalizedTime);
            return true;
        }

        // 约束归一化窗口并保证结束时间不早于开始时间
        private void normalizeWindow(ref float openNormalizedTime, ref float closeNormalizedTime)
        {
            openNormalizedTime = Mathf.Clamp01(openNormalizedTime);
            closeNormalizedTime = Mathf.Clamp01(closeNormalizedTime);
            if (closeNormalizedTime < openNormalizedTime)
                closeNormalizedTime = openNormalizedTime;
        }
    }
}

