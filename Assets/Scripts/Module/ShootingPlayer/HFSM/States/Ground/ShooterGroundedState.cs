/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色站立复合状态，提供站立分支默认子状态
* │  类    名: ShooterGroundedState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.ShootingPlayer.HFSM.States.Ground
{
    // 站立分支的复合状态
    public sealed class ShooterGroundedState : ShooterCompositeState
    {
        public override ShooterStateId Id => ShooterStateId.Grounded;
        public override ShooterStateId ParentId => ShooterStateId.None;

        public override ShooterStateId GetInitialChildId()
        {
            return ShooterStateId.GroundedIdle;
        }
    }
}
