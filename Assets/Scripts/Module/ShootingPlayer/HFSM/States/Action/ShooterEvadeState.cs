/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色闪避状态，负责消费闪避输入并写入强制位移
* │  类    名: ShooterEvadeState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ChaisakiPlayer.Context;
using Module.ChaisakiPlayer.Core;
using Module.ChaisakiPlayer.Input.Buffer;
using Module.ShootingPlayer.Config;
using Module.ShootingPlayer.HFSM.Runtime;
using UnityEngine;
using Utils.Timer;

namespace Module.ShootingPlayer.HFSM.States.Action
{
    // 闪避状态，位移由强制速度驱动，不使用动画根运动
    public sealed class ShooterEvadeState : BaseShooterState
    {
        private readonly PlayerContext m_context;
        private readonly ShooterRuntimeState m_runtimeState;
        private readonly ShooterEvadeConfigSO m_evadeConfig;

        private readonly DurationTimer m_stateTimer;
        private readonly DurationTimer m_moveTimer;

        public override ShooterStateId Id => ShooterStateId.ActionEvade;
        public override ShooterStateId ParentId => ShooterStateId.Action;

        public ShooterEvadeState(PlayerContext context, ShooterRuntimeState runtimeState,
            ShooterEvadeConfigSO evadeConfig)
        {
            m_context = context;
            m_runtimeState = runtimeState;
            m_evadeConfig = evadeConfig;
            m_stateTimer = new DurationTimer();
            m_moveTimer = new DurationTimer();
        }

        // 进入闪避并写入强制位移速度
        public override void Enter()
        {
            m_stateTimer.Reset();
            m_moveTimer.Reset();
            m_context.Input.Buffer.Consume(PlayerBufferedInputType.Dodge);
            m_context.Action.IsStateFinished = false;
            m_context.Movement.IsMovementLocked = true;

            Vector3 evadeDirection = ResolveEvadeDirection();
            m_context.Movement.MoveDir = evadeDirection;
            m_context.Movement.SetForcedMoveVelocity(evadeDirection * m_evadeConfig.EvadeSpeed);
            m_runtimeState.ForceAnimatorRestart = true;

            m_moveTimer.Start(m_evadeConfig.EvadeDuration * m_evadeConfig.EvadeMoveEndNormalizedTime);
            m_stateTimer.Start(m_evadeConfig.EvadeDuration);
        }

        public override void Exit()
        {
            m_stateTimer.Reset();
            m_moveTimer.Reset();
            m_context.Action.IsStateFinished = false;
            m_context.Movement.IsMovementLocked = false;
            StopEvadeMovement();
        }

        public override void Tick(float deltaTime)
        {
            m_stateTimer.Tick(deltaTime);
            m_moveTimer.Tick(deltaTime);

            if (m_moveTimer.IsFinished)
                StopEvadeMovement();

            if (m_stateTimer.IsFinished)
            {
                m_context.Action.IsStateFinished = true;
                StopEvadeMovement();
            }
        }

        // 解析本次闪避方向，无输入时使用角色正前方
        private Vector3 ResolveEvadeDirection()
        {
            Vector3 inputDirection =
                PlayerMoveDirectionResolver.Resolve(m_context, m_context.Input.MoveInput);

            return inputDirection.sqrMagnitude > m_evadeConfig.EvadeInputThresholdSqr
                ? inputDirection.normalized
                : PlayerMoveDirectionResolver.ResolveForward(m_context);
        }

        // 截断闪避期间产生的全部水平位移来源
        private void StopEvadeMovement()
        {
            m_context.Movement.ClearForcedMoveVelocity();
            m_context.Movement.ClearHorizontalVelocity();
            m_context.Movement.ClearHorizontalMoveIntent();
        }
    }
}
