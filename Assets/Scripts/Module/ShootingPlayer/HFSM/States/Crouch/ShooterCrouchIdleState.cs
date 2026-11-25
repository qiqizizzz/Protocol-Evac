/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色蹲伏待机状态，负责清空移动意图
* │  类    名: ShooterCrouchIdleState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ShootingPlayer.Context;
using UnityEngine;

namespace Module.ShootingPlayer.HFSM.States.Crouch
{
    // 蹲伏待机状态
    public sealed class ShooterCrouchIdleState : BaseShooterState
    {
        private readonly ShooterContext m_context;

        public override ShooterStateId Id => ShooterStateId.CrouchedIdle;
        public override ShooterStateId ParentId => ShooterStateId.Crouched;

        public ShooterCrouchIdleState(ShooterContext context)
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
