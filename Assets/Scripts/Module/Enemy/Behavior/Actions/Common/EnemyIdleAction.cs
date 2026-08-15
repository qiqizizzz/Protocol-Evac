/*
 * ┌───────────────────────────────────────────────┐
 * │  描    述: 敌人待机行为节点，结束本轮行为选择
 * │  类    名: EnemyIdleAction.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Tasks.Actions;
using Module.Enemy.Context.Runtime;

namespace Module.Enemy.Behavior.Actions.Common
{
    public sealed class EnemyIdleAction : ActionBase
    {
        private readonly EnemyMovementContext m_movementContext;

        // 创建敌人待机行为节点
        public EnemyIdleAction(EnemyMovementContext movementContext)
        {
            m_movementContext = movementContext;
        }

        // 保持停止移动并持续占用待机分支
        protected override TaskStatus OnUpdate()
        {
            m_movementContext.StopMove();
            return TaskStatus.Continue;
        }
    }
}
