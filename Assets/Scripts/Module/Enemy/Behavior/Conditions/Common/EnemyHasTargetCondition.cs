/*
 * ┌───────────────────────────────────────────────────┐
 * │  描    述: 敌人目标条件节点，判断当前目标是否存在
 * │  类    名: EnemyHasTargetCondition.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using Module.Enemy.Context.Runtime;

namespace Module.Enemy.Behavior.Conditions.Common
{
    public sealed class EnemyHasTargetCondition : ConditionBase
    {
        private readonly EnemyTargetContext m_targetContext;

        // 创建敌人目标存在条件节点
        public EnemyHasTargetCondition(EnemyTargetContext targetContext)
        {
            m_targetContext = targetContext;
        }

        protected override bool OnUpdate()
        {
            return m_targetContext.HasTarget;
        }
    }
}

