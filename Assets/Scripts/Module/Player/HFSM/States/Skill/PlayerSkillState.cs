/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家技能复合状态
 * │  类    名: PlayerSkillState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Player.HFSM.States.Skill
{
    public class PlayerSkillState : PlayerCompositeState
    {
        public override PlayerStateId Id => PlayerStateId.Skill;
        public override PlayerStateId ParentId => PlayerStateId.None;
        public override PlayerStateId GetInitialChildId()
        {
            return PlayerStateId.SkillNormalAttack;
        }
    }
}