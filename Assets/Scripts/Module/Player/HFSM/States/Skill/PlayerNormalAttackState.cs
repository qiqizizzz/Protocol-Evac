/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家普通攻击状态
 * │  类    名: PlayerNormalAttackState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.Input.Buffer;
using Module.Player.Skill;
using Module.Player.Skill.Core;
using Module.Player.Skill.Data;
using UnityEngine;

namespace Module.Player.HFSM.States.Skill
{
    public class PlayerNormalAttackState : BasePlayerState
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        private readonly PlayerContext m_context;
        private readonly PlayerSkillController m_skillController;
        private readonly PlayerNormalAttackConfigSO m_normalAttackConfig;
        
        public override PlayerStateId Id => PlayerStateId.SkillNormalAttack;
        public override PlayerStateId ParentId => PlayerStateId.Skill;

        public PlayerNormalAttackState(
            PlayerContext context,
            PlayerSkillController skillController,
            PlayerNormalAttackConfigSO normalAttackConfig)
        {
            m_context = context;
            m_skillController = skillController;
            m_normalAttackConfig = normalAttackConfig;
        }

        public override void Enter()
        {
            m_context.InputBuffer.Consume(PlayerBufferedInputType.NormalAttack);
            m_context.IsStateFinished = false;
            m_context.IsMovementLocked = m_normalAttackConfig.LockMovement;
            m_skillController.Open(PlayerSkillType.NormalAttack);

            if (!m_skillController.IsRunning)
                m_context.IsStateFinished = true;
        }

        public override void Exit()
        {
            m_skillController.Close();
            m_context.IsStateFinished = false;
            m_context.IsMovementLocked = false;
            m_context.NormalAttackIndex = 0;

            if (m_context.IsGrounded)
                m_context.RequestAnimReplay(m_context.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR
                    ? PlayerStateId.GroundedMove
                    : PlayerStateId.GroundedIdle,
                    m_normalAttackConfig.NormalAttackExitBlendDuration);
        }

        public override void Tick(float deltaTime)
        {
            if (!m_skillController.IsRunning)
            {
                m_context.IsStateFinished = true;
                return;
            }

            if (canAdvanceCombo())
                m_skillController.RequestNextStep();

            m_context.IsStateFinished = m_skillController.IsFinished;
        }

        // 判断当前普攻是否允许推进下一段
        private bool canAdvanceCombo()
        {
            return m_context.InputBuffer.Has(PlayerBufferedInputType.NormalAttack, Time.time, m_normalAttackConfig.NormalAttackBufferTime);
        }
    }
}
