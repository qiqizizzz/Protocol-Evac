/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家状态转换控制器，负责装配状态转换规则
 * │  类    名: PlayerTransitionController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.Controller;
using Module.ChaisakiPlayer.Context;
using Module.ChaisakiPlayer.HFSM.Config.Action;
using Module.ChaisakiPlayer.HFSM.Config.Air;
using Module.ChaisakiPlayer.HFSM.Transition;
using Module.ChaisakiPlayer.HFSM.Transition.Rules;
using Module.ChaisakiPlayer.Skill.Data;
using Utils.log;

// 转换规则数据结构来自 Character.Core
using PlayerTransitionRule = Framework.Character.Transition.CharacterTransitionRule<Module.ChaisakiPlayer.HFSM.PlayerStateId, Module.ChaisakiPlayer.HFSM.Transition.PlayerTransitionPriority>;

namespace Module.ChaisakiPlayer.HFSM.Transition.Controllers
{
    public sealed class PlayerTransitionController : BaseController
    {
        private readonly List<PlayerTransitionRule> m_rules = new List<PlayerTransitionRule>();
        private readonly PlayerContext m_context;
        private readonly PlayerAirConfigSO m_airConfig;
        private readonly PlayerDodgeConfigSO m_dodgeConfig;
        private readonly PlayerNormalAttackConfigSO m_normalAttackConfig;

        public IReadOnlyList<PlayerTransitionRule> Rules => m_rules;

        // 创建玩家状态转换控制器
        public PlayerTransitionController(PlayerContext context, PlayerAirConfigSO airConfig, PlayerDodgeConfigSO dodgeConfig,
            PlayerNormalAttackConfigSO normalAttackConfig)
        {
            m_context = context;
            m_airConfig = airConfig;
            m_dodgeConfig = dodgeConfig;
            m_normalAttackConfig = normalAttackConfig;
        }

        // 初始化玩家状态转换规则
        protected override void OnInit()
        {
            Register(PlayerMoveTransitionRules.Create(m_context));
            Register(PlayerAirTransitionRules.Create(m_context, m_airConfig));
            Register(PlayerActionTransitionRules.Create(m_context, m_dodgeConfig));
            Register(PlayerSkillTransitionRules.Create(m_context, m_normalAttackConfig));
            Register(PlayerDamageTransitionRules.Create(m_context, m_airConfig, m_dodgeConfig,
                m_normalAttackConfig));
        }

        // 注册一组玩家状态转换规则
        private void Register(IReadOnlyList<PlayerTransitionRule> rules)
        {
            if (rules == null)
            {
                QLog.Error("注册玩家状态转换规则失败：rules 为空");
                return;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i] == null)
                {
                    QLog.Error("注册玩家状态转换规则失败：存在空规则");
                    continue;
                }

                m_rules.Add(rules[i]);
            }
        }
    }
}
