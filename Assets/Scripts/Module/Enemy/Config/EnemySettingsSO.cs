/*
 * ┌──────────────────────────────────┐
 * │  描    述: 敌人模块配置聚合资产
 * │  类    名: EnemySettingsSO.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;
using Module.Enemy.Animation.Config;
using Module.Enemy.Behavior.Config;
using Module.Enemy.Skill.Data;

namespace Module.Enemy.Config
{
    [CreateAssetMenu(fileName = "EnemySettings", menuName = "配置/敌人/敌人配置聚合")]
    public sealed class EnemySettingsSO : ScriptableObject
    {
        [Header("数值配置")]
        [Tooltip("敌人的固有数值配置")]
        [SerializeField] private EnemyStatsConfigSO StatsConfigValue;

        [Header("行为配置")]
        [Tooltip("敌人的行为调度与意图配置")]
        [SerializeField] private EnemyBehaviorConfigSO BehaviorConfigValue;

        [Header("动画配置")]
        [Tooltip("敌人的基础动画片段配置")]
        [SerializeField] private EnemyAnimationConfigSO AnimationConfigValue;

        [Header("普攻配置")]
        [Tooltip("敌人的普通攻击配置")]
        [SerializeField] private EnemyNormalAttackConfigSO NormalAttackConfigValue;

        public EnemyStatsConfigSO StatsConfig => StatsConfigValue;
        public EnemyBehaviorConfigSO BehaviorConfig => BehaviorConfigValue;
        public EnemyAnimationConfigSO AnimationConfig => AnimationConfigValue;
        public EnemyNormalAttackConfigSO NormalAttackConfig => NormalAttackConfigValue;
    }
}
