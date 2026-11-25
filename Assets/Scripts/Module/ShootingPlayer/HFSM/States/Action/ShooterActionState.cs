/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色动作复合状态，承载闪避等一次性主动动作
* │  类    名: ShooterActionState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.ShootingPlayer.HFSM.States.Action
{
    // 动作分支的复合状态
    public sealed class ShooterActionState : ShooterCompositeState
    {
        public override ShooterStateId Id => ShooterStateId.Action;
        public override ShooterStateId ParentId => ShooterStateId.None;

        public override ShooterStateId GetInitialChildId()
        {
            return ShooterStateId.ActionEvade;
        }
    }
}
