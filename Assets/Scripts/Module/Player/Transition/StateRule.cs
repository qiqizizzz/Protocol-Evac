/*
* ┌──────────────────────────────────────────────────┐
* │  描    述: 玩家状态转换规则，描述来源、目标与触发条件
* │  类    名: StateRule.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Module.Player.HFSM;

namespace Module.Player.Transition
{
    public sealed class StateRule
    {
        public PlayerStateId SourceId { get; }
        public PlayerStateId TargetId { get; }
        public RuleLevel Level { get; }
        public int Order { get; }

        private readonly Func<bool> m_condition;

        /// <summary>
        /// 创建一条玩家状态转换规则
        /// </summary>
        /// <param name="sourceId">规则适用的来源状态，None 表示任意状态</param>
        /// <param name="targetId">规则满足时的目标状态</param>
        /// <param name="level">规则所属优先级层级</param>
        /// <param name="condition">规则触发条件</param>
        /// <param name="order">同层级规则的优先顺序，数值越大越优先</param>
        public StateRule(
            PlayerStateId sourceId,
            PlayerStateId targetId,
            RuleLevel level,
            Func<bool> condition,
            int order = 0)
        {
            if (targetId == PlayerStateId.None)
                throw new ArgumentException("状态转换规则的目标状态不能是 None", nameof(targetId));

            SourceId = sourceId;
            TargetId = targetId;
            Level = level;
            Order = order;
            m_condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        /// <summary>
        /// 判断规则是否适用于当前活动路径且满足触发条件
        /// </summary>
        /// <param name="activeStatePath">当前活动状态路径</param>
        /// <returns>规则当前是否可以触发</returns>
        public bool CanApply(IReadOnlyList<PlayerStateId> activeStatePath)
        {
            if (SourceId == PlayerStateId.None)
                return m_condition();

            for (int i = 0; i < activeStatePath.Count; i++)
            {
                if (activeStatePath[i] == SourceId)
                    return m_condition();
            }

            return false;
        }
    }
}
