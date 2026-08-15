/*
 * ┌───────────────────────────────────────────────────┐
 * │  描    述: 敌人普通攻击配置，保存普攻专属执行参数
 * │  类    名: EnemyNormalAttackConfigSO.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────┘
 */

using Module.Ability.Data;
using TriInspector;
using UnityEngine;

namespace Module.Enemy.Skill.Data
{
    [CreateAssetMenu(fileName = "EnemyNormalAttackConfig", menuName = "配置/敌人/普攻/敌人普攻配置")]
    [DeclareFoldoutGroup("ExecutionSettings", Title = "执行设置", Expanded = true)]
    public sealed class EnemyNormalAttackConfigSO : AbilityConfigSO
    {
        [Group("ExecutionSettings")]
        [LabelText("冷却时间")]
        [Tooltip("普通攻击完成后的冷却时间")]
        [SerializeField, Min(0f)] private float CooldownValue;

        [Group("ExecutionSettings")]
        [LabelText("锁定移动")]
        [Tooltip("普通攻击期间是否锁定移动")]
        [SerializeField] private bool LockMovementValue;

        [Group("ExecutionSettings")]
        [LabelText("允许转向")]
        [Tooltip("普通攻击期间是否允许朝向目标旋转")]
        [SerializeField] private bool CanRotateValue;

        public float Cooldown => CooldownValue;
        public bool LockMovement => LockMovementValue;
        public bool CanRotate => CanRotateValue;
    }
}
