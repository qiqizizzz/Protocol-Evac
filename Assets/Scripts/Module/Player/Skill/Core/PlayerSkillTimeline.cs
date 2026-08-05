/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家技能时间轴
 * │  类    名: PlayerSkillTimeline.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
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
        public float NormalizedTime => m_stepTimer.NormalizedTime;
        public bool IsRunning { get; private set; }
        public bool IsFinished { get; private set; }
        public PlayerSkillStepData CurrentStep => m_currentConfig?.GetStep(m_currentStepIndex);

        public PlayerSkillTimeline()
        {
            m_stepTimer = new DurationTimer();
            reset();
        }

        #region 生命周期
        public void Open(PlayerSkillType skillType, PlayerSkillConfigSO config, PlayerContext context)
        {
            reset();

            CurrentSkillType = skillType;
            m_currentConfig = config;
            m_currentStepIndex = 0;
            IsRunning = true;
            context.IsStateFinished = false;

            enterCurrentStep(context);
        }

        public void Tick(float deltaTime, PlayerContext context)
        {
            if (!IsRunning)
                return;

            float previousNormalizedTime = m_stepTimer.NormalizedTime;
            m_stepTimer.Tick(deltaTime);
            updateStepAdvanceBuffer(previousNormalizedTime, m_stepTimer.NormalizedTime);

            if (!m_stepTimer.IsFinished)
                return;

            if (tryAdvanceStep(context))
                return;

            finish(context);
        }

        public void Close(PlayerContext context)
        {
            reset();
            context.IsStateFinished = false;
            context.SetRootMotionMoveEnabled(false);
        }
        #endregion
        
        public void RequestStepAdvance()
        {
            if (!IsRunning)
                return;

            m_isStepAdvanceRequested = true;
        }

        // 进入当前技能段落
        private void enterCurrentStep(PlayerContext context)
        {
            PlayerSkillStepData stepData = CurrentStep;
            if (stepData == null)
            {
                QLog.Error($"进入玩家技能段落失败：{CurrentSkillType} 第 {m_currentStepIndex} 段配置为空");
                finish(context);
                return;
            }

            m_isStepAdvanceRequested = false;
            m_isStepAdvanceBuffered = false;
            m_stepTimer.Reset();
            m_stepTimer.Start(stepData.Duration);
            context.SetRootMotionMoveEnabled(stepData.UseRootMotion);

            if (CurrentSkillType == PlayerSkillType.NormalAttack)
                context.NormalAttackIndex = m_currentStepIndex;
        }

        // 刷新段落推进缓存
        private void updateStepAdvanceBuffer(float previousNormalizedTime, float currentNormalizedTime)
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

            if (!isNormalizedTimeInWindow(previousNormalizedTime, currentNormalizedTime, openNormalizedTime, closeNormalizedTime))
                return;

            m_isStepAdvanceBuffered = true;
            m_isStepAdvanceRequested = false;
        }

        // 尝试推进到下一段技能
        private bool tryAdvanceStep(PlayerContext context)
        {
            if (!m_isStepAdvanceBuffered)
                return false;

            int nextStepIndex = m_currentStepIndex + 1;
            if (m_currentConfig == null || nextStepIndex >= m_currentConfig.StepCount)
                return false;

            m_currentStepIndex = nextStepIndex;
            enterCurrentStep(context);
            return IsRunning;
        }

        // 判断归一化时间段是否覆盖指定窗口
        private bool isNormalizedTimeInWindow(float previousNormalizedTime, float currentNormalizedTime, float openNormalizedTime, float closeNormalizedTime)
        {
            return currentNormalizedTime >= openNormalizedTime && previousNormalizedTime <= closeNormalizedTime;
        }

        private void finish(PlayerContext context)
        {
            IsRunning = false;
            IsFinished = true;
            m_isStepAdvanceRequested = false;
            m_isStepAdvanceBuffered = false;
            m_stepTimer.Complete();
            context.IsStateFinished = true;
            context.SetRootMotionMoveEnabled(false);
        }

        private void reset()
        {
            CurrentSkillType = null;
            m_currentConfig = null;
            m_currentStepIndex = -1;
            IsRunning = false;
            IsFinished = false;
            m_isStepAdvanceRequested = false;
            m_isStepAdvanceBuffered = false;
            m_stepTimer.Reset();
        }
    }
}
