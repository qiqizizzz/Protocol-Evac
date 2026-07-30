/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家状态ID类
 * │  类    名: PlayerStateId.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Player.HFSM
{
    public enum PlayerStateId
    {
        None,

        Grounded,
        GroundedIdle,
        GroundedMove,
        GroundedSprint,

        Airborne,
        AirborneJump,
        AirborneFall,

        Action,
        ActionDodge,

        Skill,
        SkillNormalAttack,
        SkillSpecial,
        SkillUltimate,

        Disabled,
        DisabledHurt,
        DisabledDead
    }
}
