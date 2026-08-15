/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人战斗等待行为，冷却期间停步并持续面向目标
 * │  类    名: EnemyCombatWaitAction.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Tasks.Actions;
using Module.Enemy.Context;
using Module.Enemy.Skill;
using Module.Enemy.Skill.Core;

namespace Module.Enemy.Behavior.Actions.Attack
{
    public sealed class EnemyCombatWaitAction : ActionBase
    {
        private readonly EnemyContext m_context;
        private readonly EnemySkillController m_skillController;

        // 创建敌人战斗等待行为节点
        public EnemyCombatWaitAction(EnemyContext context, EnemySkillController skillController)
        {
            m_context = context;
            m_skillController = skillController;
        }

        // 冷却期间保持面对目标，冷却结束后交回行为选择
        protected override TaskStatus OnUpdate()
        {
            m_context.Movement.StopMove();
            m_context.Movement.SetLookDirection(
                m_context.Target.CurrentTarget.position - m_context.Transform.position);
            return m_skillController.CanOpen(EnemySkillType.NormalAttack)
                ? TaskStatus.Success
                : TaskStatus.Continue;
        }
    }
}

