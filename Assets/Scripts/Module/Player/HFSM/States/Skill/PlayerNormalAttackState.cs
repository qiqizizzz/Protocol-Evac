/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家普通攻击状态
 * │  类    名: PlayerNormalAttackState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Player.HFSM.States.Skill
{
    public class PlayerNormalAttackState : BasePlayerState
    {
        public override PlayerStateId Id => PlayerStateId.SkillNormalAttack;
        public override PlayerStateId ParentId => PlayerStateId.Skill;
    }
}