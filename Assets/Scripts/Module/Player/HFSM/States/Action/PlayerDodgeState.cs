/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家闪避状态，负责消费闪避输入并写入闪避位移意图
 * │  类    名: PlayerDodgeState.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.Config.Action;
using Module.Player.Context;
using Module.Player.Core;
using Module.Player.Input.Buffer;
using UnityEngine;

namespace Module.Player.HFSM.States.Action
{
    public sealed class PlayerDodgeState : BasePlayerState
    {
        private readonly PlayerContext m_context;
        private readonly PlayerDodgeConfigSO m_dodgeConfig;

        private float m_elapsedTime;

        public override PlayerStateId Id => PlayerStateId.ActionDodge;
        public override PlayerStateId ParentId => PlayerStateId.Action;

        public PlayerDodgeState(PlayerContext context, PlayerDodgeConfigSO dodgeConfig)
        {
            m_context = context;
            m_dodgeConfig = dodgeConfig;
        }

        public override void Enter()
        {
            m_elapsedTime = 0f;
            m_context.InputBuffer.Consume(PlayerBufferedInputType.Dodge);
            m_context.IsActionFinished = false;
            m_context.IsMovementLocked = true;

            Vector3 dodgeDirection = resolveDodgeDirection();
            m_context.MoveDir = dodgeDirection;
            m_context.SetForcedMoveVelocity(dodgeDirection * m_dodgeConfig.DodgeSpeed);
            m_context.RequestAnimReplay(PlayerStateId.ActionDodge);
        }

        public override void Exit()
        {
            m_elapsedTime = 0f;
            m_context.IsActionFinished = false;
            m_context.IsMovementLocked = false;
            m_context.ClearForcedMoveVelocity();
        }

        public override void Tick(float deltaTime)
        {
            m_elapsedTime += deltaTime;

            if (m_elapsedTime >= m_dodgeConfig.DodgeDuration)
                m_context.IsActionFinished = true;
        }

        // 解析本次闪避方向
        private Vector3 resolveDodgeDirection()
        {
            Vector3 inputDirection = PlayerMoveDirectionResolver.Resolve(m_context, m_context.MoveInput);

            return inputDirection.sqrMagnitude > m_dodgeConfig.DodgeInputThresholdSqr
                ? inputDirection.normalized
                : PlayerMoveDirectionResolver.ResolveForward(m_context);
        }
    }
}
