/*
 * ┌───────────────────────────────────────────────────────────┐
 * │  描    述: 敌人普攻条件节点，判断普通攻击技能当前是否可用
 * │  类    名: EnemyCanNormalAttackCondition.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using Module.Enemy.Skill;
using Module.Enemy.Skill.Core;

namespace Module.Enemy.Behavior.Conditions.Attack
{
    public sealed class EnemyCanNormalAttackCondition : ConditionBase
    {
        private readonly EnemySkillController m_skillController;

        // 创建敌人普通攻击可用条件节点
        public EnemyCanNormalAttackCondition(EnemySkillController skillController)
        {
            m_skillController = skillController;
        }

        protected override bool OnUpdate()
        {
            return m_skillController.CanOpen(EnemySkillType.NormalAttack);
        }
    }
}
