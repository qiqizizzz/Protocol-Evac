/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家普通攻击状态
 * │  类    名: PlayerNormalAttackState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.HFSM.Config.Skill;
using Module.Player.Input.Buffer;
using Utils.Timer;
using UnityEngine;

namespace Module.Player.HFSM.States.Skill
{
    public class PlayerNormalAttackState : BasePlayerState
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        private readonly PlayerContext m_context;
        private readonly PlayerNormalAttackConfigSO m_normalAttackConfig;

        private readonly DurationTimer m_normalAttackTimer;
        private int m_currentAttackIndex;
        
        public override PlayerStateId Id => PlayerStateId.SkillNormalAttack;
        public override PlayerStateId ParentId => PlayerStateId.Skill;

        public PlayerNormalAttackState(PlayerContext context, PlayerNormalAttackConfigSO normalAttackConfig)
        {
            m_context = context;
            m_normalAttackConfig = normalAttackConfig;
            m_normalAttackTimer = new DurationTimer();
        }

        public override void Enter()
        {
            m_currentAttackIndex = 0;
            m_context.NormalAttackIndex = m_currentAttackIndex;
            m_normalAttackTimer.Reset();
            m_context.InputBuffer.Consume(PlayerBufferedInputType.NormalAttack);
            m_context.IsStateFinished = false;
            m_context.IsMovementLocked = m_normalAttackConfig.LockMovement;
            m_context.RequestAnimReplay(PlayerStateId.SkillNormalAttack);
            
            m_normalAttackTimer.Start(m_normalAttackConfig.NormalAttackDuration);
        }

        public override void Exit()
        {
            m_normalAttackTimer.Reset();
            m_context.IsStateFinished = false;
            m_context.IsMovementLocked = false;
            m_context.NormalAttackIndex = 0;

            if (m_context.IsGrounded)
                m_context.RequestAnimReplay(m_context.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR
                    ? PlayerStateId.GroundedMove
                    : PlayerStateId.GroundedIdle);
        }

        public override void Tick(float deltaTime)
        {
            m_normalAttackTimer.Tick(deltaTime);

            if (!m_normalAttackTimer.IsFinished)
                return;

            if (tryAdvanceCombo())
                return;

            m_context.IsStateFinished = true;
        }

        // 尝试推进到下一段普通攻击
        private bool tryAdvanceCombo()
        {
            int nextAttackIndex = m_currentAttackIndex + 1;
            if (nextAttackIndex >= m_normalAttackConfig.StateClipCount)
                return false;

            if (!m_context.InputBuffer.Has(PlayerBufferedInputType.NormalAttack, Time.time, m_normalAttackConfig.NormalAttackBufferTime))
                return false;

            m_context.InputBuffer.Consume(PlayerBufferedInputType.NormalAttack);
            m_currentAttackIndex = nextAttackIndex;
            m_context.NormalAttackIndex = m_currentAttackIndex;

            m_normalAttackTimer.Reset();
            m_normalAttackTimer.Start(m_normalAttackConfig.GetStateDuration(m_currentAttackIndex));
            return true;
        }
    }
}
