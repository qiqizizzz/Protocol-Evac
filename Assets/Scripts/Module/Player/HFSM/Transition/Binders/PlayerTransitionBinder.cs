/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家状态转换绑定器，负责收集状态转换规则
 * │  类    名: PlayerTransitionBinder.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Player.HFSM.Transition;
using Utils.log;

namespace Module.Player.HFSM.Transition.Binders
{
    public sealed class PlayerTransitionBinder
    {
        private readonly List<PlayerTransitionRule> m_rules = new List<PlayerTransitionRule>();

        public IReadOnlyList<PlayerTransitionRule> Rules => m_rules;

        // 绑定一组玩家状态转换规则
        public void Bind(IReadOnlyList<PlayerTransitionRule> rules)
        {
            if (rules == null)
            {
                QLog.Error("绑定玩家状态转换规则失败：rules 为空");
                return;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i] == null)
                {
                    QLog.Error("绑定玩家状态转换规则失败：存在空规则");
                    continue;
                }

                m_rules.Add(rules[i]);
            }
        }
    }
}
