/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家闪避状态，负责消费闪避输入并写入闪避位移意图
 * │  类    名: PlayerDodgeState.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.Core;
using Module.Player.HFSM.Config.Action;
using Module.Player.Input.Buffer;
using UnityEngine;
using Utils.Timer;

namespace Module.Player.HFSM.States.Action
{
    public sealed class PlayerDodgeState : BasePlayerState
    {
        private readonly PlayerContext m_context;
        private readonly PlayerDodgeConfigSO m_dodgeConfig;

        private DurationTimer m_dodgeTimer;
        private DurationTimer m_dodgeMoveTimer;

        public override PlayerStateId Id => PlayerStateId.ActionDodge;
        public override PlayerStateId ParentId => PlayerStateId.Action;

        public PlayerDodgeState(PlayerContext context, PlayerDodgeConfigSO dodgeConfig)
        {
            m_context = context;
            m_dodgeConfig = dodgeConfig;
            m_dodgeTimer = new DurationTimer();
            m_dodgeMoveTimer = new DurationTimer();
        }

        // 进入闪避状态并应用闪避配置的武器表现
        public override void Enter()
        {
            m_dodgeTimer.Reset();
            m_context.Input.Buffer.Consume(PlayerBufferedInputType.Dodge);
            m_context.Action.IsStateFinished = false;
            m_context.Movement.IsMovementLocked = true;
            m_context.Action.IsWeaponVisible = m_dodgeConfig.DodgeClipData.ShowWeapon;

            Vector3 dodgeDirection = ResolveDodgeDirection();
            m_context.Movement.MoveDir = dodgeDirection;
            m_context.Movement.SetForcedMoveVelocity(dodgeDirection * m_dodgeConfig.DodgeSpeed);
            m_context.Action.RequestAnimReplay(PlayerStateId.ActionDodge);
            
            m_dodgeTimer.Start(m_dodgeConfig.DodgeDuration);
            m_dodgeMoveTimer.Start(m_dodgeConfig.DodgeMoveDuration);
        }

        public override void Exit()
        {
            m_dodgeTimer.Reset();
            m_dodgeMoveTimer.Reset();
            m_context.Action.IsStateFinished = false;
            m_context.Movement.IsMovementLocked = false;
            StopDodgeMovement();
        }

        public override void Tick(float deltaTime)
        {
            m_dodgeTimer.Tick(deltaTime);
            m_dodgeMoveTimer.Tick(deltaTime);

            if (m_dodgeMoveTimer.IsFinished)
                StopDodgeMovement();

            if (m_dodgeTimer.IsFinished)
            {
                m_context.Action.IsStateFinished = true;
                StopDodgeMovement();
            }
        }

        // 解析本次闪避方向
        private Vector3 ResolveDodgeDirection()
        {
            Vector3 inputDirection = PlayerMoveDirectionResolver.Resolve(m_context, m_context.Input.MoveInput);

            return inputDirection.sqrMagnitude > m_dodgeConfig.DodgeInputThresholdSqr
                ? inputDirection.normalized
                : PlayerMoveDirectionResolver.ResolveForward(m_context);
        }

        // 截断闪避期间产生的全部水平位移来源
        private void StopDodgeMovement()
        {
            m_context.Movement.ClearForcedMoveVelocity();
            m_context.Movement.ClearHorizontalVelocity();
            m_context.Movement.ClearHorizontalMoveIntent();
            m_context.Action.ClearRootMotionDeltaPosition();
        }
    }
}
