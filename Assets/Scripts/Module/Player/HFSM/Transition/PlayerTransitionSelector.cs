/*
* ┌────────────────────────────────────────────────────┐
* │  描    述: 玩家状态转换选择器，按优先级裁决并提交转换规则
* │  类    名: PlayerTransitionSelector.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Linq;
using Module.Player.HFSM;
using Utils.log;

namespace Module.Player.HFSM.Transition
{
    public sealed class PlayerTransitionSelector
    {
        private readonly PlayerStateMachine m_stateMachine;
        private readonly List<PlayerTransitionRule> m_rules;

        /// <summary>
        /// 创建状态转换选择器并固定规则优先级
        /// </summary>
        /// <param name="stateMachine">玩家层级状态机</param>
        /// <param name="rules">需要参与裁决的状态转换规则</param>
        public PlayerTransitionSelector(PlayerStateMachine stateMachine, IEnumerable<PlayerTransitionRule> rules)
        {
            m_stateMachine = stateMachine;

            if (m_stateMachine == null)
                QLog.Error("创建状态转换选择器失败：stateMachine 为空");

            if (rules == null)
            {
                QLog.Error("创建状态转换选择器失败：rules 为空");
                m_rules = new List<PlayerTransitionRule>();
                return;
            }

            List<PlayerTransitionRule> validRules = new List<PlayerTransitionRule>();
            foreach (PlayerTransitionRule rule in rules)
            {
                if (rule == null)
                {
                    QLog.Error("创建状态转换选择器失败：规则集合中存在空规则");
                    continue;
                }

                validRules.Add(rule);
            }

            m_rules = validRules
                .OrderByDescending(rule => rule.Priority)
                .ThenByDescending(rule => rule.Order)
                .ToList();
        }

        // 获取当前规则裁决出的下一目标状态，不执行状态切换
        public PlayerStateId GetNextTargetStateId()
        {
            if (m_stateMachine == null)
                return PlayerStateId.None;

            for (int i = 0; i < m_rules.Count; i++)
            {
                PlayerTransitionRule rule = m_rules[i];

                if (!rule.CanApply(m_stateMachine.ActiveStatePath))
                    continue;

                if (rule.TargetId == m_stateMachine.CurrentLeafStateId)
                    return PlayerStateId.None;

                return rule.TargetId;
            }

            return PlayerStateId.None;
        }

        // 按优先级选择本帧第一条有效规则并提交状态转换
        public void Tick()
        {
            if (m_stateMachine == null)
                return;

            PlayerStateId targetStateId = GetNextTargetStateId();
            if (targetStateId != PlayerStateId.None)
                m_stateMachine.ChangeState(targetStateId);
        }
    }
}
