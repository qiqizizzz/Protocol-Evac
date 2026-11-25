/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色站立瞄准待机状态，负责锁定瞄准姿态下的移速
* │  类    名: ShooterAimState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ChaisakiPlayer.Context;
using Module.ShootingPlayer.Config;
using UnityEngine;

namespace Module.ShootingPlayer.HFSM.States.Aim
{
    // 站立瞄准待机状态
    public sealed class ShooterAimState : BaseShooterState
    {
        private readonly PlayerContext m_context;
        private readonly ShooterAimConfigSO m_aimConfig;

        public override ShooterStateId Id => ShooterStateId.GroundedAim;
        public override ShooterStateId ParentId => ShooterStateId.Grounded;

        public ShooterAimState(PlayerContext context, ShooterAimConfigSO aimConfig)
        {
            m_context = context;
            m_aimConfig = aimConfig;
        }

        public override void Enter()
        {
            m_context.Movement.MoveDir = Vector3.zero;
            m_context.Movement.TargetMoveSpeed = 0f;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            m_context.Movement.MoveDir = Vector3.zero;
            m_context.Movement.TargetMoveSpeed = 0f;
        }
    }
}
