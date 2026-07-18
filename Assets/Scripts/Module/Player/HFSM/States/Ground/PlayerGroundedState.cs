/*
 * ┌────────────────────────────────────────────────┐
 * │  描    述: 玩家地面复合状态,负责提供地面状态默认子状态                      
 * │  类    名: PlayerGroundedState.cs       
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────────┘
 */

namespace Module.Player.HFSM.States.Ground
{
    public sealed class PlayerGroundedState : PlayerCompositeState
    {
        public override PlayerStateId Id => PlayerStateId.Grounded;
        public override PlayerStateId ParentId => PlayerStateId.None; //代表复合状态

        public override PlayerStateId GetInitialChildId()
        {
            return PlayerStateId.GroundedIdle;
        }
    }
}