/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人伤害控制器，负责生命扣除、受击状态与强制位移
 * │  类    名: EnemyDamageController.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using Module.Combat.Damage;
using Module.Enemy.Config;
using Module.Enemy.Context;
using UnityEngine;
using Utils.log;

namespace Module.Enemy.Damage
{
    public sealed class EnemyDamageController
    {
        private readonly EnemyContext m_context;
        private readonly EnemyStatsConfigSO m_statsConfig;

        // 创建敌人伤害控制器并初始化生命值
        public EnemyDamageController(EnemyContext context, EnemyStatsConfigSO statsConfig)
        {
            m_context = context;
            m_statsConfig = statsConfig;
            m_context.Damage.InitHealth(m_statsConfig.MaxHealthValue);
        }

        // 尝试应用伤害、受击控制和命中位移
        public bool TryTakeDamage(DamageData damageData, float hurtDuration)
        {
            if (damageData.Damage <= 0f)
            {
                QLog.Error($"敌人受伤失败，伤害值必须大于 0：{damageData.Damage}");
                return false;
            }

            if (m_context.Damage.IsDead || m_context.Damage.IsHurt)
                return false;

            m_context.Damage.ApplyDamage(damageData.Damage, hurtDuration);
            return true;
        }

        // 计算覆盖受击动画与受击位移的硬直时长
        public float CalculateHurtDuration(DamageData damageData, float animationDuration, float minimumHurtDuration)
        {
            float hurtDuration = Mathf.Max(0f, animationDuration, minimumHurtDuration,
                damageData.HorizontalKnockbackDuration);
            if (damageData.VerticalLaunchSpeed <= 0f)
                return hurtDuration;

            float airborneDuration = damageData.VerticalLaunchSpeed * 2f / Mathf.Abs(Physics.gravity.y);
            return Mathf.Max(hurtDuration, airborneDuration);
        }

        // 根据本次命中数据写入受击位移与浮空请求
        public void ApplyHitMotion(DamageData damageData)
        {
            Vector3 horizontalVelocity = damageData.HitDirection * damageData.HorizontalKnockbackSpeed;
            m_context.Movement.SetForcedMove(horizontalVelocity, damageData.HorizontalKnockbackDuration,
                damageData.VerticalLaunchSpeed);
        }

        // 推进受击持续时间并返回是否刚完成受击
        public bool Tick(float deltaTime)
        {
            return m_context.Damage.TickHurt(deltaTime);
        }
    }
}
