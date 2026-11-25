/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色死亡状态，持续锁定操作与位移
* │  类    名: ShooterDeadState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ShootingPlayer.Context;
using Module.ShootingPlayer.HFSM.Animation;
using Module.ShootingPlayer.HFSM.Runtime;

namespace Module.ShootingPlayer.HFSM.States.Disabled
{
    // 死亡状态，播放固定死亡动画后持续锁定
    public sealed class ShooterDeadState : BaseShooterState
    {
        private readonly ShooterContext m_context;
        private readonly ShooterRuntimeState m_runtimeState;

        public override ShooterStateId Id => ShooterStateId.DisabledDead;
        public override ShooterStateId ParentId => ShooterStateId.Disabled;

        public ShooterDeadState(ShooterContext context, ShooterRuntimeState runtimeState)
        {
            m_context = context;
            m_runtimeState = runtimeState;
        }

        // 进入死亡状态并锁定全部操作
        public override void Enter()
        {
            m_context.Input.IsInputLocked = true;
            m_context.Input.Buffer.ClearAll();
            m_context.Movement.IsMovementLocked = true;
            m_context.Movement.ClearForcedMoveVelocity();
            m_context.Movement.ClearHorizontalVelocity();
            m_context.Movement.ClearHorizontalMoveIntent();
            m_runtimeState.CurrentHurtAnimationId = ShooterHurtAnimationId.Die1;
            m_runtimeState.ForceAnimatorRestart = true;
        }

        // 退出死亡状态并恢复角色控制
        public override void Exit()
        {
            m_context.Input.IsInputLocked = false;
            m_context.Movement.IsMovementLocked = false;
            m_context.Movement.ClearForcedMoveVelocity();
            m_context.Movement.ClearHorizontalVelocity();
            m_context.Movement.ClearHorizontalMoveIntent();
            m_runtimeState.CurrentHurtAnimationId = ShooterHurtAnimationId.None;
        }
    }
}
