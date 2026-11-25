/*
* ┌──────────────────────────────────────────────────┐
* │  描    述: 角色状态转换规则，描述来源、目标与触发条件
* │  类    名: CharacterTransitionRule.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Framework.Character.HFSM;
using Utils.log;

namespace Framework.Character.Transition
{
    /// <summary>
    /// 角色状态转换规则，各角色只贡献条件，匹配流程由框架统一实现
    /// </summary>
    /// <typeparam name="TStateId">角色状态标识枚举，默认值表示空状态</typeparam>
    /// <typeparam name="TPriority">角色状态转换优先级枚举</typeparam>
    public sealed class CharacterTransitionRule<TStateId, TPriority>
        where TStateId : struct, Enum
        where TPriority : struct, Enum
    {
        public TStateId SourceId { get; }
        public TStateId TargetId { get; }
        public TPriority Priority { get; }
        public int Order { get; }
        public bool AllowReentry { get; }

        private readonly Func<bool> m_condition;

        /// <summary>
        /// 创建一条状态转换规则
        /// </summary>
        /// <param name="sourceId">规则适用的来源状态，默认值表示任意状态</param>
        /// <param name="targetId">规则满足时的目标状态</param>
        /// <param name="priority">规则优先级</param>
        /// <param name="condition">规则触发条件</param>
        /// <param name="order">同优先级规则的优先顺序，数值越大越优先</param>
        /// <param name="allowReentry">目标已是当前叶子状态时是否强制重新进入</param>
        public CharacterTransitionRule(TStateId sourceId, TStateId targetId, TPriority priority, Func<bool> condition,
            int order = 0, bool allowReentry = false)
        {
            if (EqualityComparer<TStateId>.Default.Equals(targetId, default))
                QLog.Error($"创建状态转换规则失败：目标状态不能是默认状态标识，来源状态 {sourceId}");

            if (condition == null)
                QLog.Error("创建状态转换规则失败：condition 为空");

            SourceId = sourceId;
            TargetId = targetId;
            Priority = priority;
            Order = order;
            AllowReentry = allowReentry;
            m_condition = condition;
        }

        /// <summary>
        /// 判断规则是否适用于当前活动路径且满足触发条件
        /// </summary>
        /// <param name="activeStatePath">当前活动状态路径</param>
        /// <returns>规则当前是否可以触发</returns>
        public bool CanApply(IReadOnlyList<TStateId> activeStatePath)
        {
            EqualityComparer<TStateId> idComparer = EqualityComparer<TStateId>.Default;

            if (idComparer.Equals(TargetId, default) || m_condition == null)
                return false;

            if (idComparer.Equals(SourceId, default))
                return m_condition();

            if (activeStatePath == null)
            {
                QLog.Error("规则匹配失败：activeStatePath 为空");
                return false;
            }

            for (int i = 0; i < activeStatePath.Count; i++)
            {
                if (idComparer.Equals(activeStatePath[i], SourceId))
                    return m_condition();
            }

            return false;
        }
    }
}
