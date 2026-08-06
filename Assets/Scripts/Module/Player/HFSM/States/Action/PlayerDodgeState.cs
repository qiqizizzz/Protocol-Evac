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

        public override PlayerStateId Id => PlayerStateId.ActionDodge;
        public override PlayerStateId ParentId => PlayerStateId.Action;

        public PlayerDodgeState(PlayerContext context, PlayerDodgeConfigSO dodgeConfig)
        {
            m_context = context;
            m_dodgeConfig = dodgeConfig;
            m_dodgeTimer = new DurationTimer();
        }

        public override void Enter()
        {
            m_dodgeTimer.Reset();
            m_context.InputBuffer.Consume(PlayerBufferedInputType.Dodge);
            m_context.IsStateFinished = false;
            m_context.IsMovementLocked = true;

            Vector3 dodgeDirection = ResolveDodgeDirection();
            m_context.MoveDir = dodgeDirection;
            m_context.SetForcedMoveVelocity(dodgeDirection * m_dodgeConfig.DodgeSpeed);
            m_context.RequestAnimReplay(PlayerStateId.ActionDodge);
            
            m_dodgeTimer.Start(m_dodgeConfig.DodgeDuration);
        }

        public override void Exit()
        {
            m_dodgeTimer.Reset();
            m_context.IsStateFinished = false;
            m_context.IsMovementLocked = false;
            m_context.ClearForcedMoveVelocity();
        }

        public override void Tick(float deltaTime)
        {
            m_dodgeTimer.Tick(deltaTime);

            if (m_dodgeTimer.IsFinished)
                m_context.IsStateFinished = true;
        }

        // 解析本次闪避方向
        private Vector3 ResolveDodgeDirection()
        {
            Vector3 inputDirection = PlayerMoveDirectionResolver.Resolve(m_context, m_context.MoveInput);

            return inputDirection.sqrMagnitude > m_dodgeConfig.DodgeInputThresholdSqr
                ? inputDirection.normalized
                : PlayerMoveDirectionResolver.ResolveForward(m_context);
        }
    }
}
