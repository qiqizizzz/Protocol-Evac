/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家普攻配置，保存普攻输入与状态控制参数
 * │  类    名: PlayerNormalAttackConfigSO.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Ability.Data;
using UnityEngine;

namespace Module.Player.Skill.Data
{
    [CreateAssetMenu(fileName = "PlayerNormalAttackConfig", menuName = "配置/玩家/技能/玩家普攻配置")]
    public sealed class PlayerNormalAttackConfigSO : AbilityConfigSO
    {
        [Header("输入容错")]
        [Tooltip("普攻输入缓存时间")]
        [SerializeField] private float NormalAttackBufferTimeValue = 0.25f;

        [Header("控制锁定")]
        [Tooltip("普攻期间是否锁定移动")]
        [SerializeField] private bool LockMovementValue = true;

        [Tooltip("普攻期间是否允许通过移动输入修正角色朝向")]
        [SerializeField] private bool CanTurnDuringAttackValue = true;

        [Tooltip("普攻进入收招后允许接下一段的额外缓冲时间")]
        [SerializeField, Min(0f)] private float ComboRecoveryBufferTimeValue = 0.18f;

        [Header("退出过渡")]
        [Tooltip("普攻退出时回到 Idle/Move 的混合时长")]
        [SerializeField] private float NormalAttackExitBlendDurationValue = 0.15f;

        public float NormalAttackDuration => GetStepDuration(0);

        public float NormalAttackBufferTime => NormalAttackBufferTimeValue;

        public bool LockMovement => LockMovementValue;

        public bool CanTurnDuringAttack => CanTurnDuringAttackValue;

        public float ComboRecoveryBufferTime => ComboRecoveryBufferTimeValue;

        public float NormalAttackExitBlendDuration => NormalAttackExitBlendDurationValue;

        private void OnValidate()
        {
            NormalAttackBufferTimeValue = Mathf.Max(0f, NormalAttackBufferTimeValue);
            ComboRecoveryBufferTimeValue = Mathf.Max(0f, ComboRecoveryBufferTimeValue);
            NormalAttackExitBlendDurationValue = Mathf.Max(0f, NormalAttackExitBlendDurationValue);
        }

    }
}

