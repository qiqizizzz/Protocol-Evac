/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色伤害控制器，负责生命扣除与受击请求写入
* │  类    名: ShooterDamageController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Combat.Damage;
using Module.Player.Config;
using Module.Player.Context;
using Module.ShootingPlayer.Config;
using UnityEngine;
using Utils.log;

namespace Module.ShootingPlayer.Damage
{
    // 射击角色伤害控制器，与千咲伤害控制器平行独立
    public sealed class ShooterDamageController
    {
        private readonly PlayerContext m_context;
        private readonly PlayerStatsConfigSO m_statsConfig;
        private readonly ShooterDamageConfigSO m_damageConfig;

        private float m_nextDamageAvailableTime;

        public float CurrentHealth => m_context.Damage.CurrentHealth;

        public bool IsGmInvincible { get; private set; }

        public ShooterDamageController(PlayerContext context, PlayerStatsConfigSO statsConfig,
            ShooterDamageConfigSO damageConfig)
        {
            m_context = context;
            m_statsConfig = statsConfig;
            m_damageConfig = damageConfig;
            m_context.Damage.InitHealth(m_statsConfig.MaxHealthValue);
        }

        // 尝试应用本次伤害并写入受击状态请求
        public bool TryTakeDamage(DamageData damageData)
        {
            if (IsGmInvincible)
                return false;

            if (damageData.Damage <= 0f)
            {
                QLog.Error($"射击角色受伤失败，伤害值必须大于 0：{damageData.Damage}");
                return false;
            }

            if (m_context.Damage.IsDead || m_context.Damage.HasPendingHurt ||
                Time.time < m_nextDamageAvailableTime)
                return false;

            m_context.Damage.ApplyDamage(damageData.Damage, damageData.ReactionType, damageData.HitDirection);
            m_nextDamageAvailableTime = Time.time + m_damageConfig.DamageInvulnerabilityDuration;
            return true;
        }

        // 恢复全部生命并清除伤害冷却
        public void RestoreFullHealth()
        {
            m_context.Damage.InitHealth(m_statsConfig.MaxHealthValue);
            m_nextDamageAvailableTime = 0f;
        }

        // 设置 GM 专用的无敌状态
        public void SetGmInvincible(bool isEnabled)
        {
            IsGmInvincible = isEnabled;
        }
    }
}
