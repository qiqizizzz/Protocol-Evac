/*
 * ┌───────────────────────────────────────────────────┐
 * │  描    述: 敌人行为控制器，负责构建与调度行为树
 * │  类    名: EnemyBehaviorController.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Trees;
using Module.Enemy.Ability.Core;
using Module.Enemy.Behavior.Actions;
using Module.Enemy.Context;
using UnityEngine;

namespace Module.Enemy.Behavior.Core
{
    public sealed class EnemyBehaviorController
    {
        private readonly EnemyContext m_context;
        private readonly EnemyAbilityController m_abilityController;
        private readonly GameObject m_owner;

        public BehaviorTree Tree { get; }

        // 创建敌人行为控制器并构建运行时树
        public EnemyBehaviorController(GameObject owner, EnemyContext context, EnemyAbilityController abilityController)
        {
            m_owner = owner;
            m_context = context;
            m_abilityController = abilityController;
            Tree = BuildTree();
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

        // 构建第一版能力请求行为树
        private BehaviorTree BuildTree()
        {
            return new BehaviorTreeBuilder(m_owner)
                .Name("Enemy Behavior Tree")
                .Selector("Root")
                    .Sequence("Ability Request")
                        .Condition("Has Ability Request", () => m_context.Action.HasAbilityRequest)
                        .AddNode(new EnemyPlayAbilityAction(m_abilityController, m_context.Action)
                        {
                            Name = "Play Ability"
                        })
                    .End()
                    .Do("Idle", () => TaskStatus.Continue)
                .End()
                .Build();
        }
    }
}

