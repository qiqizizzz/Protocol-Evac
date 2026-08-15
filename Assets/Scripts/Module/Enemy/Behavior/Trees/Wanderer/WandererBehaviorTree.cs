/*
 * ┌───────────────────────────────────────────────────────────┐
 * │  描    述: 流浪者行为树，组合普通攻击与待机行为分支
 * │  类    名: WandererBehaviorTree.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Trees;
using Module.Enemy.Behavior.Actions.Attack;
using Module.Enemy.Behavior.Actions.Chase;
using Module.Enemy.Behavior.Actions.Common;
using Module.Enemy.Behavior.Actions.Patrol;
using Module.Enemy.Behavior.Conditions.Attack;
using Module.Enemy.Behavior.Conditions.Common;
using Module.Enemy.Behavior.Conditions.Patrol;
using Module.Enemy.Context;
using Module.Enemy.Skill.Core;
using Module.Navigation.Core;
using UnityEngine;

namespace Module.Enemy.Behavior.Trees.Wanderer
{
    public sealed class WandererBehaviorTree
    {
        public BehaviorTree Tree { get; }

        // 创建流浪者行为树
        public WandererBehaviorTree(GameObject owner, EnemyContext context, EnemySkillController skillController,
            INavigationController navigationController)
        {
            Tree = CreateTree(owner, context, skillController, navigationController);
        }

        // 组合流浪者当前可执行的行为分支
        private BehaviorTree CreateTree(GameObject owner, EnemyContext context, EnemySkillController skillController,
            INavigationController navigationController)
        {
            return new BehaviorTreeBuilder(owner)
                .Name("流浪者行为树")
                .Selector("选择行为")
                    .Sequence("普通攻击")
                        .AddNode(new EnemyHasTargetCondition(context.Target)
                        {
                            Name = "存在目标"
                        })
                        .AddNode(new EnemyTargetInAttackRangeCondition(context.Target)
                        {
                            Name = "目标进入攻击范围"
                        })
                        .AddNode(new EnemyCanNormalAttackCondition(skillController)
                        {
                            Name = "普通攻击可用"
                        })
                        .AddNode(new EnemyNormalAttackAction(skillController, context)
                        {
                            Name = "执行普通攻击"
                        })
                    .End()
                    .Sequence("战斗等待")
                        .AddNode(new EnemyHasTargetCondition(context.Target)
                        {
                            Name = "存在目标"
                        })
                        .AddNode(new EnemyTargetInAttackRangeCondition(context.Target)
                        {
                            Name = "目标处于攻击范围"
                        })
                        .AddNode(new EnemyCombatWaitAction(context, skillController)
                        {
                            Name = "等待普通攻击冷却"
                        })
                    .End()
                    .Sequence("追击目标")
                        .AddNode(new EnemyHasTargetCondition(context.Target)
                        {
                            Name = "存在目标"
                        })
                        .AddNode(new EnemyChaseAction(context, navigationController)
                        {
                            Name = "追击目标"
                        })
                    .End()
                    .Sequence("脱战归位")
                        .AddNode(new EnemyOutsidePatrolRangeCondition(context)
                        {
                            Name = "已离开出生区域"
                        })
                        .AddNode(new EnemyReturnToSpawnAction(context, navigationController)
                        {
                            Name = "返回出生点"
                        })
                    .End()
                    .AddNode(new EnemyPatrolAction(context, navigationController)
                    {
                        Name = "巡逻"
                    })
                    .AddNode(new EnemyIdleAction(context.Movement)
                    {
                        Name = "待机"
                    })
                .End()
                .Build();
        }
    }
}
