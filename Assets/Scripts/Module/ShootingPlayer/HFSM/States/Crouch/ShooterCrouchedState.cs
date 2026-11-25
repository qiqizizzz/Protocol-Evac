/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色蹲伏复合状态，提供蹲伏分支默认子状态
* │  类    名: ShooterCrouchedState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.ShootingPlayer.HFSM.States.Crouch
{
    // 蹲伏分支的复合状态
    public sealed class ShooterCrouchedState : ShooterCompositeState
    {
        public override ShooterStateId Id => ShooterStateId.Crouched;
        public override ShooterStateId ParentId => ShooterStateId.None;

        public override ShooterStateId GetInitialChildId()
        {
            return ShooterStateId.CrouchedIdle;
        }
    }
}
