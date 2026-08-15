/*
 * ┌─────────────────────────────────────────────────────────┐
 * │  描    述: 敌人普通攻击行为节点，负责打开、等待与中断普通攻击
 * │  类    名: EnemyNormalAttackAction.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Tasks.Actions;
using Module.Enemy.Skill;
using Module.Enemy.Skill.Core;

namespace Module.Enemy.Behavior.Actions.Attack
{
    public sealed class EnemyNormalAttackAction : ActionBase
    {
        private readonly EnemySkillController m_skillController;

        private bool m_hasOpenedSkill;

        // 创建敌人普通攻击行为节点
        public EnemyNormalAttackAction(EnemySkillController skillController)
        {
            m_skillController = skillController;
        }

        // 尝试打开普通攻击
        protected override void OnStart()
        {
            m_hasOpenedSkill = m_skillController.TryOpen(EnemySkillType.NormalAttack);
        }

        // 等待技能时间轴完成
        protected override TaskStatus OnUpdate()
        {
            if (!m_hasOpenedSkill)
                return TaskStatus.Failure;

            m_skillController.RequestNextStep();
            if (m_skillController.IsRunning)
                return TaskStatus.Continue;

            return m_skillController.IsFinished ? TaskStatus.Success : TaskStatus.Failure;
        }

        // 中断节点时关闭仍在运行的技能
        protected override void OnExit()
        {
            if (m_hasOpenedSkill && m_skillController.IsRunning)
                m_skillController.Close();

            m_hasOpenedSkill = false;
        }
    }
}
