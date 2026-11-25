/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色蹲伏瞄准移动状态，负责写入蹲伏瞄准移速
* │  类    名: ShooterCrouchAimMoveState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ShootingPlayer.Context;
using Framework.Character.Runtime.Core;
using Module.ShootingPlayer.Config;

namespace Module.ShootingPlayer.HFSM.States.Crouch
{
    // 蹲伏瞄准移动状态
    public sealed class ShooterCrouchAimMoveState : BaseShooterState
    {
        private readonly ShooterContext m_context;
        private readonly ShooterCrouchConfigSO m_crouchConfig;

        public override ShooterStateId Id => ShooterStateId.CrouchedAimMove;
        public override ShooterStateId ParentId => ShooterStateId.Crouched;

        public ShooterCrouchAimMoveState(ShooterContext context, ShooterCrouchConfigSO crouchConfig)
        {
            m_context = context;
            m_crouchConfig = crouchConfig;
        }

        public override void Enter()
        {
            UpdateMovementIntent();
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            UpdateMovementIntent();
        }

        // 根据当前输入更新蹲伏瞄准的移动方向与移速
        private void UpdateMovementIntent()
        {
            m_context.Movement.MoveDir =
                CharacterMoveDirectionResolver.Resolve(m_context, m_context.Input.MoveInput);
            m_context.Movement.TargetMoveSpeed = m_crouchConfig.CrouchAimMoveSpeed;
        }
    }
}
