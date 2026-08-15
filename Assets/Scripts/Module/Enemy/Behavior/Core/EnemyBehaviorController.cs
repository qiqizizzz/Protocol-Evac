/*
 * ┌───────────────────────────────────────────────────┐
 * │  描    述: 敌人行为控制器，负责持有、推进与重置行为树
 * │  类    名: EnemyBehaviorController.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;

namespace Module.Enemy.Behavior.Core
{
    public sealed class EnemyBehaviorController
    {
        public BehaviorTree Tree { get; }

        // 创建敌人行为控制器并接收已组合的运行时树
        public EnemyBehaviorController(BehaviorTree tree)
        {
            Tree = tree;
        }

        // 推进敌人行为树
        public TaskStatus Tick()
        {
            return Tree.Tick();
        }

        // 中断并重置当前行为分支
        public void Reset()
        {
            Tree.Reset();
        }
    }
}
