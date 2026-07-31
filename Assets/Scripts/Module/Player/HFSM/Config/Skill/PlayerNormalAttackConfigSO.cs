/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家普攻配置，保存普攻纵切的基础时长与输入缓存参数
 * │  类    名: PlayerNormalAttackConfigSO.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.HFSM.Config.Common;
using UnityEngine;

namespace Module.Player.HFSM.Config.Skill
{
    [CreateAssetMenu(fileName = "PlayerNormalAttackConfig", menuName = "配置/玩家/技能/玩家普攻配置")]
    public sealed class PlayerNormalAttackConfigSO : PlayerStateCommonConfigSO
    {
        private const float DEFAULT_NORMAL_ATTACK_DURATION = 0.6f;

        [Header("输入容错")]
        [Tooltip("普攻输入缓存时间")]
        [SerializeField, Min(0f)] private float NormalAttackBufferTimeValue = 0.25f;

        [Header("控制锁定")]
        [Tooltip("普攻期间是否锁定移动")]
        [SerializeField] private bool LockMovementValue = true;

        public float NormalAttackDuration => GetStateDuration(0, DEFAULT_NORMAL_ATTACK_DURATION);

        public float NormalAttackBufferTime => NormalAttackBufferTimeValue;

        public bool LockMovement => LockMovementValue;
    }
}
