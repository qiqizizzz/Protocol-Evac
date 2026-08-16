/*
 * ┌────────────────────────────────────────────────────────┐
 * │  描    述: 玩家伤害控制器，负责生命扣除与受击请求写入
 * │  类    名: PlayerDamageController.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────────────────┘
 */

using Module.Combat.Damage;
using Module.Player.Config;
using Module.Player.Context;
using Module.Player.HFSM.Animation.Type;
using Module.Player.HFSM.Config.Disabled;
using UnityEngine;
using Utils.log;

namespace Module.Player.Damage
{
    public sealed class PlayerDamageController
    {
        private readonly PlayerContext m_context;
        private readonly PlayerStatsConfigSO m_statsConfig;
        private readonly PlayerDamageConfigSO m_damageConfig;

        private float m_nextDamageAvailableTime;

        // 创建玩家伤害控制器并初始化生命值
        public PlayerDamageController(PlayerContext context, PlayerStatsConfigSO statsConfig, PlayerDamageConfigSO damageConfig)
        {
            m_context = context;
            m_statsConfig = statsConfig;
            m_damageConfig = damageConfig;
            m_context.Damage.InitHealth(m_statsConfig.MaxHealthValue);
        }

        // 尝试应用本次伤害并写入受击状态请求
        public bool TryTakeDamage(DamageData damageData)
        {
            if (damageData.Damage <= 0f)
            {
                QLog.Error($"玩家受伤失败，伤害值必须大于 0：{damageData.Damage}");
                return false;
            }

            if (m_context.Damage.IsDead || m_context.Damage.HasPendingHurt ||
                m_context.Damage.HurtAnimationId != PlayerHurtAnimationId.None ||
                Time.time < m_nextDamageAvailableTime)
                return false;

            m_context.Damage.ApplyDamage(damageData.Damage, damageData.ReactionType, damageData.HitDirection);
            m_nextDamageAvailableTime = Time.time + m_damageConfig.DamageInvulnerabilityDuration;
            return true;
        }
    }
}
