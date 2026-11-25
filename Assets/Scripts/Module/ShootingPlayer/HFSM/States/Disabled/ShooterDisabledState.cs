/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色受控复合状态，提供受击默认子状态
* │  类    名: ShooterDisabledState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.ShootingPlayer.HFSM.States.Disabled
{
    // 受控分支的复合状态
    public sealed class ShooterDisabledState : ShooterCompositeState
    {
        public override ShooterStateId Id => ShooterStateId.Disabled;
        public override ShooterStateId ParentId => ShooterStateId.None;

        public override ShooterStateId GetInitialChildId()
        {
            return ShooterStateId.DisabledHurt;
        }
    }
}
