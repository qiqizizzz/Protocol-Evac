/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家普通攻击状态
 * │  类    名: PlayerNormalAttackState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.Input.Buffer;
using Module.Player.Skill.Data;
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
        private bool m_hasComboBufferedInput;
        
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
            m_hasComboBufferedInput = false;
            m_context.InputBuffer.Consume(PlayerBufferedInputType.NormalAttack);
            m_context.IsStateFinished = false;
            m_context.IsMovementLocked = m_normalAttackConfig.LockMovement;
            refreshRootMotionMoveEnabled();
            m_context.RequestAnimReplay(PlayerStateId.SkillNormalAttack);

            m_normalAttackTimer.Start(m_normalAttackConfig.NormalAttackDuration);
        }

        public override void Exit()
        {
            m_normalAttackTimer.Reset();
            m_hasComboBufferedInput = false;
            m_context.IsStateFinished = false;
            m_context.IsMovementLocked = false;
            m_context.NormalAttackIndex = 0;
            m_context.SetRootMotionMoveEnabled(false);

            if (m_context.IsGrounded)
                m_context.RequestAnimReplay(m_context.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR
                    ? PlayerStateId.GroundedMove
                    : PlayerStateId.GroundedIdle,
                    m_normalAttackConfig.NormalAttackExitBlendDuration);
        }

        public override void Tick(float deltaTime)
        {
            float previousNormalizedTime = m_normalAttackTimer.NormalizedTime;
            m_normalAttackTimer.Tick(deltaTime);
            refreshComboBufferedInput(previousNormalizedTime, m_normalAttackTimer.NormalizedTime);
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
            if (nextAttackIndex >= m_normalAttackConfig.StepCount)
                return false;

            if (!canAdvanceCombo())
                return false;

            m_context.InputBuffer.Consume(PlayerBufferedInputType.NormalAttack);
            m_hasComboBufferedInput = false;
            m_currentAttackIndex = nextAttackIndex;
            m_context.NormalAttackIndex = m_currentAttackIndex;
            refreshRootMotionMoveEnabled();

            m_normalAttackTimer.Reset();
            m_normalAttackTimer.Start(m_normalAttackConfig.GetStepDuration(m_currentAttackIndex));
            m_context.RequestAnimReplay(PlayerStateId.SkillNormalAttack);
            return true;
        }

        // 根据当前普攻段刷新根运动位移开关
        private void refreshRootMotionMoveEnabled()
        {
            PlayerSkillStepData stepData = m_normalAttackConfig.GetStep(m_currentAttackIndex);
            m_context.SetRootMotionMoveEnabled(stepData.UseRootMotion);
        }

        // 根据当前时间段刷新连段输入缓存
        private void refreshComboBufferedInput(float previousNormalizedTime, float currentNormalizedTime)
        {
            if (!m_normalAttackConfig.TryGetComboWindow(m_currentAttackIndex, out float comboOpenNormalizedTime, out float comboCloseNormalizedTime))
                return;

            if (!isNormalizedTimeInWindow(previousNormalizedTime, currentNormalizedTime, comboOpenNormalizedTime, comboCloseNormalizedTime))
                return;

            if (m_context.InputBuffer.Has(PlayerBufferedInputType.NormalAttack, Time.time, m_normalAttackConfig.NormalAttackBufferTime))
                m_hasComboBufferedInput = true;
        }

        // 判断当前归一化时间段是否覆盖连段窗口
        private bool isNormalizedTimeInWindow(float previousNormalizedTime, float currentNormalizedTime, float comboOpenNormalizedTime, float comboCloseNormalizedTime)
        {
            return currentNormalizedTime >= comboOpenNormalizedTime && previousNormalizedTime <= comboCloseNormalizedTime;
        }

        // 判断当前普攻是否允许推进下一段
        private bool canAdvanceCombo()
        {
            if (m_normalAttackConfig.TryGetComboWindow(m_currentAttackIndex, out _, out _))
                return m_hasComboBufferedInput;

            return m_context.InputBuffer.Has(PlayerBufferedInputType.NormalAttack, Time.time, m_normalAttackConfig.NormalAttackBufferTime);
        }
    }
}
