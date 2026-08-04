/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家普攻配置，保存普攻输入与状态控制参数
 * │  类    名: PlayerNormalAttackConfigSO.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Player.Skill.Data
{
    [CreateAssetMenu(fileName = "PlayerNormalAttackConfig", menuName = "配置/玩家/技能/玩家普攻配置")]
    public sealed class PlayerNormalAttackConfigSO : PlayerSkillConfigSO
    {
        [Header("输入容错")]
        [Tooltip("普攻输入缓存时间")]
        [SerializeField, Min(0f)] private float NormalAttackBufferTimeValue = 0.25f;

        [Header("控制锁定")]
        [Tooltip("普攻期间是否锁定移动")]
        [SerializeField] private bool LockMovementValue = true;

        [Header("退出过渡")]
        [Tooltip("普攻退出时回到 Idle/Move 的混合时长")]
        [SerializeField, Min(0f)] private float NormalAttackExitBlendDurationValue = 0.15f;

        public float NormalAttackDuration => GetStepDuration(0);

        public float NormalAttackBufferTime => NormalAttackBufferTimeValue;

        public bool LockMovement => LockMovementValue;

        public float NormalAttackExitBlendDuration => NormalAttackExitBlendDurationValue;

        // 尝试读取指定普攻段的连段窗口
        public bool TryGetComboWindow(int attackIndex, out float comboOpenNormalizedTime, out float comboCloseNormalizedTime)
        {
            comboOpenNormalizedTime = 0f;
            comboCloseNormalizedTime = 0f;

            PlayerSkillStepData stepData = GetStep(attackIndex);
            if (stepData == null)
                return false;

            return stepData.TryGetStepAdvanceWindow(out comboOpenNormalizedTime, out comboCloseNormalizedTime);
        }
    }
}

