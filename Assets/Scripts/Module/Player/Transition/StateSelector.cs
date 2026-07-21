/*
* ┌────────────────────────────────────────────────────┐
* │  描    述: 玩家状态选择器，按优先级裁决并提交转换规则
* │  类    名: StateSelector.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Module.Player.HFSM;

namespace Module.Player.Transition
{
    public sealed class StateSelector
    {
        private readonly PlayerStateMachine m_stateMachine;
        private readonly List<StateRule> m_rules;

        /// <summary>
        /// 创建状态选择器并固定规则优先级
        /// </summary>
        /// <param name="stateMachine">玩家层级状态机</param>
        /// <param name="rules">需要参与裁决的状态转换规则</param>
        public StateSelector(
            PlayerStateMachine stateMachine,
            IEnumerable<StateRule> rules)
        {
            m_stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            m_rules = (rules ?? throw new ArgumentNullException(nameof(rules)))
                .OrderByDescending(rule => rule.Level)
                .ThenByDescending(rule => rule.Order)
                .ToList();
        }

        // 按优先级选择本帧第一条有效规则并提交状态转换
        public void Tick()
        {
            for (int i = 0; i < m_rules.Count; i++)
            {
                StateRule rule = m_rules[i];

                if (!rule.CanApply(m_stateMachine.ActiveStatePath))
                    continue;

                if (rule.TargetId == m_stateMachine.CurrentLeafStateId)
                    return;

                m_stateMachine.ChangeState(rule.TargetId);
                return;
            }
        }
    }
}
