/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人攻击距离条件节点，判断目标是否进入攻击范围
 * │  类    名: EnemyTargetInAttackRangeCondition.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using Module.Enemy.Context.Runtime;

namespace Module.Enemy.Behavior.Conditions.Attack
{
    public sealed class EnemyTargetInAttackRangeCondition : ConditionBase
    {
        private readonly EnemyTargetContext m_targetContext;

        // 创建敌人攻击距离条件节点
        public EnemyTargetInAttackRangeCondition(EnemyTargetContext targetContext)
        {
            m_targetContext = targetContext;
        }

        protected override bool OnUpdate()
        {
            return m_targetContext.IsInAttackRange;
        }
    }
}

