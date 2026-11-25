/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家普通攻击状态
 * │  类    名: PlayerNormalAttackState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Ability.Data;
using Module.ChaisakiPlayer.Context;
using Module.ChaisakiPlayer.Core;
using Framework.Character.Runtime.Core;
using Framework.Character.Runtime.Input;
using Framework.Character.Runtime.Skill;
using Framework.Character.Runtime.Skill.Core;
using Module.ChaisakiPlayer.Skill.Data;
using UnityEngine;

namespace Module.ChaisakiPlayer.HFSM.States.Skill
{
    public class PlayerNormalAttackState : BasePlayerState
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        private readonly PlayerContext m_context;
        private readonly CharacterSkillController<PlayerStateId> m_skillController;
        private readonly PlayerNormalAttackConfigSO m_normalAttackConfig;
        
        public override PlayerStateId Id => PlayerStateId.SkillNormalAttack;
        public override PlayerStateId ParentId => PlayerStateId.Skill;

        public PlayerNormalAttackState(
            PlayerContext context,
            CharacterSkillController<PlayerStateId> skillController,
            PlayerNormalAttackConfigSO normalAttackConfig)
        {
            m_context = context;
            m_skillController = skillController;
            m_normalAttackConfig = normalAttackConfig;
        }

        public override void Enter()
        {
            m_context.Input.Buffer.Consume(CharacterBufferedInputType.NormalAttack);
            m_context.Action.IsStateFinished = false;
            m_context.Movement.IsMovementLocked = m_normalAttackConfig.LockMovement;
            m_skillController.Open(CharacterSkillType.NormalAttack);

            if (!m_skillController.IsRunning)
                m_context.Action.IsStateFinished = true;
        }

        public override void Exit()
        {
            m_skillController.Close();
            m_context.Action.IsStateFinished = false;
            m_context.Movement.IsMovementLocked = false;
            m_context.Movement.ClearTurnDirection();
            m_context.Action.NormalAttackIndex = 0;
            m_context.Action.NormalAttackPhase = AbilityStepPhase.Begin;

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
            UpdateAttackTurnDirection();

            if (!m_skillController.IsRunning)
            {
                m_context.Action.IsStateFinished = true;
                return;
            }

            if (CanAdvanceCombo())
                m_skillController.RequestNextStep();

            m_context.Action.IsStateFinished = m_skillController.IsFinished;
        }

        // 根据移动输入更新普攻期间的身体朝向修正
        private void UpdateAttackTurnDirection()
        {
            if (!m_normalAttackConfig.CanTurnDuringAttack)
            {
                m_context.Movement.ClearTurnDirection();
                return;
            }

            if (m_context.Input.MoveInput.sqrMagnitude <= MOVE_INPUT_THRESHOLD_SQR)
            {
                m_context.Movement.ClearTurnDirection();
                return;
            }

            Vector3 turnDirection = CharacterMoveDirectionResolver.Resolve(m_context, m_context.Input.MoveInput);
            m_context.Movement.SetTurnDirection(turnDirection);
        }

        // 判断当前普攻是否允许推进下一段
        private bool CanAdvanceCombo()
        {
            return m_context.Input.Buffer.Has(CharacterBufferedInputType.NormalAttack, Time.time, m_normalAttackConfig.NormalAttackBufferTime);
        }
    }
}
