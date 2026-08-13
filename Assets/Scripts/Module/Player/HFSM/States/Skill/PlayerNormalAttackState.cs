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
            m_context.Input.Buffer.Consume(PlayerBufferedInputType.NormalAttack);
            m_context.Action.IsStateFinished = false;
            m_context.Movement.IsMovementLocked = m_normalAttackConfig.LockMovement;
            m_skillController.Open(PlayerSkillType.NormalAttack);

            if (!m_skillController.IsRunning)
                m_context.Action.IsStateFinished = true;
        }

        public override void Exit()
        {
            m_skillController.Close();
            m_context.Action.IsStateFinished = false;
            m_context.Movement.IsMovementLocked = false;
            m_context.Action.NormalAttackIndex = 0;
            m_context.Action.NormalAttackPhase = PlayerSkillStepPhase.Begin;

            if (m_context.Movement.IsGrounded &&
                m_context.Input.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR)
            {
                m_context.Action.RequestAnimReplay(PlayerStateId.GroundedMove,
                    m_normalAttackConfig.NormalAttackExitBlendDuration);
            }

            // 无移动输入时保留 attackXX_end -> Standchange -> Grounded_Locomotion 的完整收招表现
        }

        public override void Tick(float deltaTime)
        {
            if (!m_skillController.IsRunning)
            {
                m_context.Action.IsStateFinished = true;
                return;
            }

            if (CanAdvanceCombo())
                m_skillController.RequestNextStep();

            m_context.Action.IsStateFinished = m_skillController.IsFinished;
        }

        // 判断当前普攻是否允许推进下一段
        private bool CanAdvanceCombo()
        {
            return m_context.Input.Buffer.Has(PlayerBufferedInputType.NormalAttack, Time.time, m_normalAttackConfig.NormalAttackBufferTime);
        }
    }
}
