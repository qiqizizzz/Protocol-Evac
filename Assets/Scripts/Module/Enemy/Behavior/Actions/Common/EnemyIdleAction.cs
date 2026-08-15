/*
 * ┌───────────────────────────────────────────────┐
 * │  描    述: 敌人待机行为节点，结束本轮行为选择
 * │  类    名: EnemyIdleAction.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Tasks.Actions;

namespace Module.Enemy.Behavior.Actions.Common
{
    public sealed class EnemyIdleAction : ActionBase
    {
        protected override TaskStatus OnUpdate()
        {
            return TaskStatus.Success;
        }
    }
}

