/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人战斗观察行为，冷却期间侧移并持续面向目标
 * │  类    名: EnemyCombatWaitAction.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Tasks.Actions;
using Module.Enemy.Context;
using Module.Enemy.Skill;
using Module.Enemy.Skill.Core;
using UnityEngine;

namespace Module.Enemy.Behavior.Actions.Attack
{
    public sealed class EnemyCombatWaitAction : ActionBase
    {
        private readonly EnemyContext m_context;
        private readonly EnemySkillController m_skillController;
        private readonly float m_observeDuration;

        private float m_observeRemainingTime;
        private float m_strafeDirectionSign;

        // 创建敌人战斗等待行为节点
        public EnemyCombatWaitAction(EnemyContext context, EnemySkillController skillController)
        {
            m_context = context;
            m_skillController = skillController;
            m_observeDuration = context.BehaviorConfig.CombatObserveDurationValue;
        }

        // 进入战斗观察时随机选择玩家的一侧
        protected override void OnStart()
        {
            m_observeRemainingTime = m_observeDuration;
            m_strafeDirectionSign = Random.value < 0.5f ? -1f : 1f;
        }

        // 冷却期间在目标侧方走动观察，并在观察结束后交回行为选择
        protected override TaskStatus OnUpdate()
        {
            Vector3 targetDirection = m_context.Target.CurrentTarget.position - m_context.Transform.position;
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 strafeDirection = Vector3.Cross(Vector3.up, targetDirection.normalized) * m_strafeDirectionSign;
                m_context.Movement.SetMoveDirection(strafeDirection);
                m_context.Movement.SetLookDirection(targetDirection);
            }

            m_observeRemainingTime = Mathf.Max(0f, m_observeRemainingTime - Time.deltaTime);
            if (!m_context.Target.IsInCombatObserveRange)
                return TaskStatus.Success;

            if (!m_context.Target.IsInAttackRange || m_observeRemainingTime > 0f
                || !m_skillController.CanOpen(EnemySkillType.NormalAttack))
                return TaskStatus.Continue;

            return TaskStatus.Success;
        }

        // 退出战斗观察时清理侧移请求
        protected override void OnExit()
        {
            m_context.Movement.StopMove();
        }
    }
}
