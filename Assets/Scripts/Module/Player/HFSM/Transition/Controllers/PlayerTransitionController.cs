/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家状态转换控制器，负责装配状态转换规则
 * │  类    名: PlayerTransitionController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Player.Context;
using Module.Player.HFSM.Config.Action;
using Module.Player.HFSM.Config.Air;
using Module.Player.HFSM.Transition;
using Module.Player.HFSM.Transition.Rules;
using Module.Player.Skill.Data;
using Utils.log;

namespace Module.Player.HFSM.Transition.Controllers
{
    public sealed class PlayerTransitionController
    {
        private readonly List<PlayerTransitionRule> m_rules = new List<PlayerTransitionRule>();

        public IReadOnlyList<PlayerTransitionRule> Rules => m_rules;

        // 初始化玩家状态转换规则
        public void Init(PlayerContext context, PlayerAirConfigSO airConfig, PlayerDodgeConfigSO dodgeConfig, PlayerNormalAttackConfigSO normalAttackConfig)
        {
            register(PlayerMoveTransitionRules.Create(context));
            register(PlayerAirTransitionRules.Create(context, airConfig));
            register(PlayerActionTransitionRules.Create(context, dodgeConfig));
            register(PlayerSkillTransitionRules.Create(context, normalAttackConfig));
        }

        // 注册一组玩家状态转换规则
        private void register(IReadOnlyList<PlayerTransitionRule> rules)
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
