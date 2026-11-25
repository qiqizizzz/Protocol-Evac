/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色站立待机状态，负责清空移动意图
* │  类    名: ShooterIdleState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ShootingPlayer.Context;
using UnityEngine;

namespace Module.ShootingPlayer.HFSM.States.Ground
{
    // 站立待机状态
    public sealed class ShooterIdleState : BaseShooterState
    {
        private readonly ShooterContext m_context;

        public override ShooterStateId Id => ShooterStateId.GroundedIdle;
        public override ShooterStateId ParentId => ShooterStateId.Grounded;

        public ShooterIdleState(ShooterContext context)
        {
            m_context = context;
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
