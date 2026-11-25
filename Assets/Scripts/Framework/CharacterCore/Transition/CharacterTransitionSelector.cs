/*
* ┌────────────────────────────────────────────────────┐
* │  描    述: 角色状态转换选择器，按优先级裁决并提交转换规则
* │  类    名: CharacterTransitionSelector.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Character.HFSM;
using Utils.log;

namespace Framework.Character.Transition
{
    /// <summary>
    /// 状态转换选择器，每帧按优先级裁决出一条规则并提交给状态机
    /// </summary>
    /// <typeparam name="TStateId">角色状态标识枚举，默认值表示空状态</typeparam>
    /// <typeparam name="TPriority">角色状态转换优先级枚举</typeparam>
    public sealed class CharacterTransitionSelector<TStateId, TPriority>
        where TStateId : struct, Enum
        where TPriority : struct, Enum
    {
        private readonly CharacterStateMachine<TStateId> m_stateMachine;
        private readonly List<CharacterTransitionRule<TStateId, TPriority>> m_rules;

        /// <summary>
        /// 创建状态转换选择器并固定规则优先级
        /// </summary>
        /// <param name="stateMachine">角色层级状态机</param>
        /// <param name="rules">需要参与裁决的状态转换规则</param>
        public CharacterTransitionSelector(CharacterStateMachine<TStateId> stateMachine,
            IEnumerable<CharacterTransitionRule<TStateId, TPriority>> rules)
        {
            m_stateMachine = stateMachine;

            if (m_stateMachine == null)
                QLog.Error("创建状态转换选择器失败：stateMachine 为空");

            if (rules == null)
            {
                QLog.Error("创建状态转换选择器失败：rules 为空");
                m_rules = new List<CharacterTransitionRule<TStateId, TPriority>>();
                return;
            }

            List<CharacterTransitionRule<TStateId, TPriority>> validRules =
                new List<CharacterTransitionRule<TStateId, TPriority>>();
            foreach (CharacterTransitionRule<TStateId, TPriority> rule in rules)
            {
                if (rule == null)
                {
                    QLog.Error("创建状态转换选择器失败：规则集合中存在空规则");
                    continue;
                }

                validRules.Add(rule);
            }

            m_rules = validRules
                .OrderByDescending(rule => (int)(object)rule.Priority)
                .ThenByDescending(rule => rule.Order)
                .ToList();
        }

        /// <summary>
        /// 获取当前规则裁决出的下一目标状态，不执行状态切换
        /// </summary>
        /// <returns>下一目标状态标识，无可执行规则时返回默认状态标识</returns>
        public TStateId GetNextTargetStateId()
        {
            CharacterTransitionRule<TStateId, TPriority> rule = GetNextRule();
            return rule == null ? default : rule.TargetId;
        }

        /// <summary>
        /// 按优先级选择本帧第一条有效规则并提交状态转换
        /// </summary>
        public void Tick()
        {
            if (m_stateMachine == null)
                return;

            CharacterTransitionRule<TStateId, TPriority> rule = GetNextRule();
            if (rule != null)
                m_stateMachine.ChangeState(rule.TargetId, rule.AllowReentry);
        }

        // 返回当前活动路径中优先级最高的可执行规则
        private CharacterTransitionRule<TStateId, TPriority> GetNextRule()
        {
            if (m_stateMachine == null)
                return null;

            EqualityComparer<TStateId> idComparer = EqualityComparer<TStateId>.Default;
            IReadOnlyList<TStateId> activeStatePath = m_stateMachine.ActiveStatePath;

            for (int i = 0; i < m_rules.Count; i++)
            {
                CharacterTransitionRule<TStateId, TPriority> rule = m_rules[i];
                if (!rule.CanApply(activeStatePath))
                    continue;

                if (idComparer.Equals(rule.TargetId, m_stateMachine.CurrentLeafStateId) && !rule.AllowReentry)
                    continue;

                return rule;
            }

            return null;
        }
    }
}
