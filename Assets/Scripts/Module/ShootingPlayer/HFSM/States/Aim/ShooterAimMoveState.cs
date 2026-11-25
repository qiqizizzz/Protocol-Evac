/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色站立瞄准移动状态，负责写入瞄准移速
* │  类    名: ShooterAimMoveState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ShootingPlayer.Context;
using Framework.Character.Runtime.Core;
using Module.ShootingPlayer.Config;

namespace Module.ShootingPlayer.HFSM.States.Aim
{
    // 站立瞄准移动状态
    public sealed class ShooterAimMoveState : BaseShooterState
    {
        private readonly ShooterContext m_context;
        private readonly ShooterAimConfigSO m_aimConfig;

        public override ShooterStateId Id => ShooterStateId.GroundedAimMove;
        public override ShooterStateId ParentId => ShooterStateId.Grounded;

        public ShooterAimMoveState(ShooterContext context, ShooterAimConfigSO aimConfig)
        {
            m_context = context;
            m_aimConfig = aimConfig;
        }

        public override void Enter()
        {
            UpdateMovementIntent();
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            UpdateMovementIntent();
        }

        // 根据当前输入更新瞄准移动方向与瞄准移速
        private void UpdateMovementIntent()
        {
            m_context.Movement.MoveDir =
                CharacterMoveDirectionResolver.Resolve(m_context, m_context.Input.MoveInput);
            m_context.Movement.TargetMoveSpeed = m_aimConfig.AimMoveSpeed;
        }
    }
}
