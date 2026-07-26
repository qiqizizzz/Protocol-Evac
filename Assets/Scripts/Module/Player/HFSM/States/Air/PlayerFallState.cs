/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家下落状态，承接自然下落与落地转换
 * │  类    名: PlayerFallState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Player.HFSM.States.Air
{
    public sealed class PlayerFallState : BasePlayerState
    {
        public override PlayerStateId Id => PlayerStateId.AirborneFall;
        public override PlayerStateId ParentId => PlayerStateId.Airborne;
    }
}
