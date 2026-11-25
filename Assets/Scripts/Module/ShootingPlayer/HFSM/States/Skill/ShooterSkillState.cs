/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色技能复合状态，承载开火段落
* │  类    名: ShooterSkillState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.ShootingPlayer.HFSM.States.Skill
{
    // 技能分支的复合状态
    public sealed class ShooterSkillState : ShooterCompositeState
    {
        public override ShooterStateId Id => ShooterStateId.Skill;
        public override ShooterStateId ParentId => ShooterStateId.None;

        public override ShooterStateId GetInitialChildId()
        {
            return ShooterStateId.SkillShoot;
        }
    }
}
