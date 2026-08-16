/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人战斗观察距离条件节点，判断目标是否进入近战观察范围
 * │  类    名: EnemyTargetInCombatObserveRangeCondition.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using Module.Enemy.Context.Runtime;

namespace Module.Enemy.Behavior.Conditions.Attack
{
    public sealed class EnemyTargetInCombatObserveRangeCondition : ConditionBase
    {
        private readonly EnemyTargetContext m_targetContext;

        // 创建敌人战斗观察距离条件节点
        public EnemyTargetInCombatObserveRangeCondition(EnemyTargetContext targetContext)
        {
            m_targetContext = targetContext;
        }

        // 判断目标是否进入近战观察距离
        protected override bool OnUpdate()
        {
            return m_targetContext.IsInCombatObserveRange;
        }
    }
}
