/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色站立移动状态，负责写入走与跑的移动意图
* │  类    名: ShooterMoveState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ChaisakiPlayer.Context;
using Module.ChaisakiPlayer.Core;
using Module.ShootingPlayer.Config;
using UnityEngine;

namespace Module.ShootingPlayer.HFSM.States.Ground
{
    // 站立移动状态，只区分步行与奔跑两档
    public sealed class ShooterMoveState : BaseShooterState
    {
        private readonly PlayerContext m_context;
        private readonly ShooterMoveConfigSO m_moveConfig;

        public override ShooterStateId Id => ShooterStateId.GroundedMove;
        public override ShooterStateId ParentId => ShooterStateId.Grounded;

        public ShooterMoveState(PlayerContext context, ShooterMoveConfigSO moveConfig)
        {
            m_context = context;
            m_moveConfig = moveConfig;
        }

        public override void Enter()
        {
            UpdateMovementIntent();
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            UpdateMovementIntent();
        }

        // 根据当前输入更新本帧的移动方向与目标速度
        private void UpdateMovementIntent()
        {
            m_context.Movement.MoveDir =
                PlayerMoveDirectionResolver.Resolve(m_context, m_context.Input.MoveInput);
            m_context.Movement.TargetMoveSpeed = m_context.Input.IsWalkMode
                ? m_moveConfig.WalkSpeed
                : m_moveConfig.RunSpeed;
        }
    }
}
