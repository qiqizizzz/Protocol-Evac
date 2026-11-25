/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色蹲伏瞄准待机状态，蹲伏位移由 2D 混合树内部表达
* │  类    名: ShooterCrouchAimState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ShootingPlayer.Context;
using Module.ShootingPlayer.Config;
using UnityEngine;

namespace Module.ShootingPlayer.HFSM.States.Crouch
{
    // 蹲伏瞄准待机状态，位移档位交给混合树中心点
    public sealed class ShooterCrouchAimState : BaseShooterState
    {
        private readonly ShooterContext m_context;
        private readonly ShooterCrouchConfigSO m_crouchConfig;

        public override ShooterStateId Id => ShooterStateId.CrouchedAim;
        public override ShooterStateId ParentId => ShooterStateId.Crouched;

        public ShooterCrouchAimState(ShooterContext context, ShooterCrouchConfigSO crouchConfig)
        {
            m_context = context;
            m_crouchConfig = crouchConfig;
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
