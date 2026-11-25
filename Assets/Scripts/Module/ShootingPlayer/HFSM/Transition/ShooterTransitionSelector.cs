/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色状态转换选择器，按优先级裁决并提交转换规则
* │  类    名: ShooterTransitionSelector.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Linq;
using Utils.log;

namespace Module.ShootingPlayer.HFSM.Transition
{
    public sealed class ShooterTransitionSelector
    {
        private readonly ShooterStateMachine m_stateMachine;
        private readonly List<ShooterTransitionRule> m_rules;

        /// <summary>
        /// 创建状态转换选择器并固定规则优先级
        /// </summary>
        /// <param name="stateMachine">射击角色层级状态机</param>
        /// <param name="rules">需要参与裁决的状态转换规则</param>
        public ShooterTransitionSelector(ShooterStateMachine stateMachine, IEnumerable<ShooterTransitionRule> rules)
        {
            m_stateMachine = stateMachine;

            if (m_stateMachine == null)
                QLog.Error("创建状态转换选择器失败：stateMachine 为空");

            if (rules == null)
            {
                QLog.Error("创建状态转换选择器失败：rules 为空");
                m_rules = new List<ShooterTransitionRule>();
                return;
            }

            List<ShooterTransitionRule> validRules = new List<ShooterTransitionRule>();
            foreach (ShooterTransitionRule rule in rules)
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
        public ShooterStateId GetNextTargetStateId()
        {
            ShooterTransitionRule rule = GetNextRule();
            return rule == null ? ShooterStateId.None : rule.TargetId;
        }

        // 按优先级选择本帧第一条有效规则并提交状态转换
        public void Tick()
        {
            if (m_stateMachine == null)
                return;

            ShooterTransitionRule rule = GetNextRule();
            if (rule != null)
                m_stateMachine.ChangeState(rule.TargetId, rule.AllowReentry);
        }

        // 返回当前活动路径中优先级最高的可执行规则
        private ShooterTransitionRule GetNextRule()
        {
            if (m_stateMachine == null)
                return null;

            for (int i = 0; i < m_rules.Count; i++)
            {
                ShooterTransitionRule rule = m_rules[i];
                if (!rule.CanApply(m_stateMachine.ActiveStatePath))
                    continue;

                if (rule.TargetId == m_stateMachine.CurrentLeafStateId && !rule.AllowReentry)
                    continue;

                return rule;
            }

            return null;
        }
    }
}
