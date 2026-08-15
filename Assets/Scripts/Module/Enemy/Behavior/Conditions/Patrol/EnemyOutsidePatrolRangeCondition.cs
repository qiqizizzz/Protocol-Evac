/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人巡逻范围条件，判断是否需要回到出生区域
 * │  类    名: EnemyOutsidePatrolRangeCondition.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using Module.Enemy.Context;
using UnityEngine;

namespace Module.Enemy.Behavior.Conditions.Patrol
{
    public sealed class EnemyOutsidePatrolRangeCondition : ConditionBase
    {
        private readonly EnemyContext m_context;
        private readonly float m_patrolRadiusSqr;

        // 创建敌人巡逻范围条件节点
        public EnemyOutsidePatrolRangeCondition(EnemyContext context)
        {
            m_context = context;
            float patrolRadius = context.BehaviorConfig.PatrolRadiusValue;
            m_patrolRadiusSqr = patrolRadius * patrolRadius;
        }

        // 判断敌人是否已离开出生区域
        protected override bool OnUpdate()
        {
            Vector3 offset = m_context.Transform.position - m_context.SpawnPosition;
            offset.y = 0f;
            return offset.sqrMagnitude > m_patrolRadiusSqr;
        }
    }
}

