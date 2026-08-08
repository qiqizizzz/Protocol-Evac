/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家技能时间轴
 * │  类    名: PlayerSkillTimeline.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.HFSM;
using Module.Player.Input.Buffer;
using Module.Player.Skill;
using Module.Player.Skill.Data;
using Utils.log;
using Utils.Timer;

namespace Module.Player.Skill.Core
{
    internal sealed class PlayerSkillTimeline
    {
        private readonly DurationTimer m_stepTimer;

        private PlayerSkillConfigSO m_currentConfig;
        private int m_currentStepIndex;
        private bool m_isStepAdvanceRequested;
        private bool m_isStepAdvanceBuffered;

        public PlayerSkillType? CurrentSkillType { get; private set; }
        public int CurrentStepIndex => m_currentStepIndex;
        public PlayerSkillStepPhase CurrentPhase { get; private set; }
        public float NormalizedTime => m_stepTimer.NormalizedTime;
        public bool IsRunning { get; private set; }
        public bool IsFinished { get; private set; }
        public PlayerSkillStepData CurrentStep => m_currentConfig?.GetStep(m_currentStepIndex);

        public PlayerSkillTimeline()
        {
            m_stepTimer = new DurationTimer();
            Reset();
        }

        #region 生命周期
        public void Open(PlayerSkillType skillType, PlayerSkillConfigSO config, PlayerContext context)
        {
            Reset();

            CurrentSkillType = skillType;
            m_currentConfig = config;
            m_currentStepIndex = 0;
            IsRunning = true;
            context.Action.IsStateFinished = false;

            EnterCurrentStepBegin(context);
        }

        public void Tick(float deltaTime, PlayerContext context)
        {
            if (!IsRunning)
                return;

            float previousNormalizedTime = m_stepTimer.NormalizedTime;
            m_stepTimer.Tick(deltaTime);
            if (CurrentPhase == PlayerSkillStepPhase.Begin)
                UpdateStepAdvanceBuffer(previousNormalizedTime, m_stepTimer.NormalizedTime);

            if (!m_stepTimer.IsFinished)
                return;

            if (CurrentPhase == PlayerSkillStepPhase.Begin)
            {
                if (TryAdvanceStep(context))
                    return;

                EnterCurrentStepRecovery(context);
                return;
            }

            Finish(context);
        }

        public void Close(PlayerContext context)
        {
            Reset();
            context.Action.IsStateFinished = false;
            context.Action.SetRootMotionMoveEnabled(false);
            context.Action.IsWeaponVisible = false;
        }
        #endregion
        
        // 记录进入下一段的请求，等待推进窗口满足后执行
        public void RequestNextStep()
        {
            if (!IsRunning || CurrentPhase != PlayerSkillStepPhase.Begin)
                return;

            m_isStepAdvanceRequested = true;
        }

        // 尝试按当前阶段配置提前结束技能
        public bool TryFinishEarly(PlayerContext context)
        {
            if (!IsRunning)
                return false;

            PlayerSkillStepData stepData = CurrentStep;
            if (stepData == null)
                return false;

            bool canEndEarly = CurrentPhase switch
            {
                PlayerSkillStepPhase.Begin => stepData.BeginCanEndEarly,
                PlayerSkillStepPhase.Recovery => stepData.RecoveryCanEndEarly,
                _ => false
            };
            if (!canEndEarly)
                return false;

            Finish(context);
            return true;
        }

        // 进入当前技能段落的攻击阶段
        private void EnterCurrentStepBegin(PlayerContext context)
        {
            PlayerSkillStepData stepData = CurrentStep;
            if (stepData == null)
            {
                QLog.Error($"进入玩家技能段落失败：{CurrentSkillType} 第 {m_currentStepIndex} 段配置为空");
                Finish(context);
                return;
            }

            m_isStepAdvanceRequested = false;
            m_isStepAdvanceBuffered = false;
            CurrentPhase = PlayerSkillStepPhase.Begin;
            m_stepTimer.Reset();
            m_stepTimer.Start(stepData.BeginDuration);
            context.Action.SetRootMotionMoveEnabled(stepData.BeginUseRootMotion);
            context.Action.IsWeaponVisible = stepData.ShowWeapon;

            if (CurrentSkillType == PlayerSkillType.NormalAttack)
            {
                context.Action.NormalAttackIndex = m_currentStepIndex;
                context.Action.NormalAttackPhase = PlayerSkillStepPhase.Begin;
                context.Action.RequestAnimReplay(PlayerStateId.SkillNormalAttack);
            }
        }

        // 进入当前技能段落的收招阶段
        private void EnterCurrentStepRecovery(PlayerContext context)
        {
            PlayerSkillStepData stepData = CurrentStep;
            CurrentPhase = PlayerSkillStepPhase.Recovery;
            m_isStepAdvanceRequested = false;
            m_isStepAdvanceBuffered = false;
            m_stepTimer.Reset();
            m_stepTimer.Start(stepData.RecoveryDuration);
            context.Action.SetRootMotionMoveEnabled(stepData.RecoveryUseRootMotion);
            context.Action.IsWeaponVisible = stepData.ShowWeapon;

            if (CurrentSkillType == PlayerSkillType.NormalAttack)
            {
                context.Action.NormalAttackPhase = PlayerSkillStepPhase.Recovery;
                context.Action.RequestAnimReplay(PlayerStateId.SkillNormalAttack);
            }
        }

        // 刷新段落推进缓存
        private void UpdateStepAdvanceBuffer(float previousNormalizedTime, float currentNormalizedTime)
        {
            if (!m_isStepAdvanceRequested || m_isStepAdvanceBuffered)
                return;

            PlayerSkillStepData stepData = CurrentStep;
            if (stepData == null)
                return;

            if (!stepData.TryGetStepAdvanceWindow(out float openNormalizedTime, out float closeNormalizedTime))
            {
                m_isStepAdvanceBuffered = true;
                m_isStepAdvanceRequested = false;
                return;
            }

            if (!IsNormalizedTimeInWindow(previousNormalizedTime, currentNormalizedTime, openNormalizedTime, closeNormalizedTime))
            {
                m_isStepAdvanceRequested = false;
                return;
            }

            m_isStepAdvanceBuffered = true;
            m_isStepAdvanceRequested = false;
        }

        // 尝试推进到下一段技能
        private bool TryAdvanceStep(PlayerContext context)
        {
            if (!m_isStepAdvanceBuffered)
                return false;

            int nextStepIndex = m_currentStepIndex + 1;
            if (m_currentConfig == null || nextStepIndex >= m_currentConfig.StepCount)
                return false;

            if (CurrentSkillType == PlayerSkillType.NormalAttack)
                context.Input.Buffer.Consume(PlayerBufferedInputType.NormalAttack);

            m_currentStepIndex = nextStepIndex;
            EnterCurrentStepBegin(context);
            return IsRunning;
        }

        // 判断归一化时间段是否覆盖指定窗口
        private bool IsNormalizedTimeInWindow(float previousNormalizedTime, float currentNormalizedTime, float openNormalizedTime, float closeNormalizedTime)
        {
            return currentNormalizedTime >= openNormalizedTime && previousNormalizedTime <= closeNormalizedTime;
        }

        private void Finish(PlayerContext context)
        {
            IsRunning = false;
            IsFinished = true;
            m_isStepAdvanceRequested = false;
            m_isStepAdvanceBuffered = false;
            m_stepTimer.Complete();
            context.Action.IsStateFinished = true;
            context.Action.SetRootMotionMoveEnabled(false);
            context.Action.IsWeaponVisible = false;
        }

        private void Reset()
        {
            CurrentSkillType = null;
            m_currentConfig = null;
            m_currentStepIndex = -1;
            CurrentPhase = PlayerSkillStepPhase.Begin;
            IsRunning = false;
            IsFinished = false;
            m_isStepAdvanceRequested = false;
            m_isStepAdvanceBuffered = false;
            m_stepTimer.Reset();
        }
    }
}
