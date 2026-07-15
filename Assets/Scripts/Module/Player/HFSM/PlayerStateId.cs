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

        //这里后续可以修改名字,暂时这样
        Grounded,
        GroundedIdle,
        GroundedMove,
        GroundedSprint,

        Airborne,
        AirborneJump,
        AirborneFall,

        Action,
        ActionAttack,
        ActionSkill,
        ActionDodge,

        Disabled,
        DisabledHurt,
        DisabledDead
    }
}