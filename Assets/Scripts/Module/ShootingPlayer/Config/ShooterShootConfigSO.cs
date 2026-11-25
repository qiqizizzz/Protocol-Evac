/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色射击技能配置，复用通用能力段落数据
* │  类    名: ShooterShootConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Ability.Data;
using UnityEngine;

namespace Module.ShootingPlayer.Config
{
    [CreateAssetMenu(fileName = "ShooterShootConfig", menuName = "配置/射击玩家/射击技能配置")]
    public sealed class ShooterShootConfigSO : AbilityConfigSO
    {
        private const int SHOOT_STEP_INDEX = 0;

        public const int REQUIRED_STEP_COUNT = 1;

        [Header("输入容错")]
        [Tooltip("开火输入缓存时间")]
        [SerializeField, Min(0f)] private float ShootBufferTimeValue = 0.2f;

        [Header("控制锁定")]
        [Tooltip("射击期间是否锁定水平位移")]
        [SerializeField] private bool LockMovementValue = true;

        [Tooltip("蹲伏状态下是否允许开火")]
        [SerializeField] private bool CanFireCrouchedValue = true;

        [Header("射击间隔")]
        [Tooltip("两次开火之间的最小间隔，动画本身更长时以动画为准")]
        [SerializeField, Min(0f)] private float FireIntervalValue = 0f;

        public float ShootBufferTime => ShootBufferTimeValue;

        public bool LockMovement => LockMovementValue;

        public bool CanFireCrouched => CanFireCrouchedValue;

        public float FireInterval => FireIntervalValue;

        // 射击段落的总持续时间
        public float ShootDuration => GetStep(SHOOT_STEP_INDEX)?.TotalDuration ?? 0f;
    }
}
