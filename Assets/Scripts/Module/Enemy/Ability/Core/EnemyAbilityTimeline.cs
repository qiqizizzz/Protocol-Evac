/*
 * ┌───────────────────────────────────────────────┐
 * │  描    述: 敌人能力时间轴，负责段落与阶段推进
 * │  类    名: EnemyAbilityTimeline.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────┘
 */

using Module.Ability.Data;
using Module.Ability.Data.Window.StepAdvance;
using Module.Enemy.Context.Runtime;
using Utils.log;
using Utils.Timer;

namespace Module.Enemy.Ability.Core
{
    internal sealed class EnemyAbilityTimeline
    {
        private readonly DurationTimer m_stepTimer;

        private AbilityConfigSO m_currentConfig;
        private int m_currentStepIndex;
        private bool m_isStepAdvanceRequested;
        private bool m_isStepAdvanceBuffered;

        public EnemyAbilityType? CurrentAbilityType { get; private set; }
        public int CurrentStepIndex => m_currentStepIndex;
        public AbilityStepPhase CurrentPhase { get; private set; }
        public float NormalizedTime => m_stepTimer.NormalizedTime;
        public bool IsRunning { get; private set; }
        public bool IsFinished { get; private set; }
        public AbilityStepData CurrentStep => m_currentConfig?.GetStep(m_currentStepIndex);

        // 创建敌人能力时间轴
        public EnemyAbilityTimeline()
        {
            m_stepTimer = new DurationTimer();
            Reset();
        }

        // 打开指定敌人能力时间轴
        public void Open(EnemyAbilityType abilityType, AbilityConfigSO config, EnemyActionContext actionContext)
        {
            Reset();

            CurrentAbilityType = abilityType;
            m_currentConfig = config;
            m_currentStepIndex = 0;
            IsRunning = true;
            EnterCurrentStepBegin(actionContext);
        }

        // 推进当前能力段落与阶段
        public void Tick(float deltaTime, EnemyActionContext actionContext)
        {
            if (!IsRunning)
                return;

            float previousNormalizedTime = m_stepTimer.NormalizedTime;
            m_stepTimer.Tick(deltaTime);
            if (CurrentPhase == AbilityStepPhase.Begin)
                UpdateStepAdvanceBuffer(previousNormalizedTime, m_stepTimer.NormalizedTime);

            if (!m_stepTimer.IsFinished)
                return;

            if (CurrentPhase == AbilityStepPhase.Begin)
            {
                if (TryAdvanceStep(actionContext))
                    return;

                EnterCurrentStepRecovery(actionContext);
                return;
            }

            Finish(actionContext);
        }

        // 关闭并重置当前能力时间轴
        public void Close(EnemyActionContext actionContext)
        {
            Reset();
            actionContext.FinishAbility();
        }

        // 请求时间轴在阶段推进窗口满足后进入下一段
        public void RequestNextStep()
        {
            if (!IsRunning || CurrentPhase != AbilityStepPhase.Begin)
                return;

            m_isStepAdvanceRequested = true;
        }

        // 进入当前能力段落的攻击阶段
        private void EnterCurrentStepBegin(EnemyActionContext actionContext)
        {
            AbilityStepData stepData = CurrentStep;
            if (stepData == null)
            {
                QLog.Error($"进入敌人能力段落失败：{CurrentAbilityType} 第 {m_currentStepIndex} 段配置为空");
                Finish(actionContext);
                return;
            }

            m_isStepAdvanceRequested = false;
            m_isStepAdvanceBuffered = false;
            CurrentPhase = AbilityStepPhase.Begin;
            m_stepTimer.Reset();
            m_stepTimer.Start(stepData.BeginDuration);
            actionContext.EnterAbilityStep(m_currentStepIndex, CurrentPhase, stepData.BeginAnimationClip,
                stepData.ShowWeapon);
        }

        // 进入当前能力段落的收招阶段
        private void EnterCurrentStepRecovery(EnemyActionContext actionContext)
        {
            AbilityStepData stepData = CurrentStep;
            CurrentPhase = AbilityStepPhase.Recovery;
            m_isStepAdvanceRequested = false;
            m_isStepAdvanceBuffered = false;
            m_stepTimer.Reset();
            m_stepTimer.Start(stepData.RecoveryDuration);
            actionContext.EnterAbilityStep(m_currentStepIndex, CurrentPhase, stepData.RecoveryAnimationClip,
                stepData.ShowWeapon);
        }

        // 刷新段落推进缓存
        private void UpdateStepAdvanceBuffer(float previousNormalizedTime, float currentNormalizedTime)
        {
            if (!m_isStepAdvanceRequested || m_isStepAdvanceBuffered)
                return;

            AbilityStepData stepData = CurrentStep;
            if (!stepData.UseStepAdvanceWindow || stepData.StepAdvanceWindowTrack == null
                || stepData.StepAdvanceWindowTrack.WindowCount == 0)
            {
                m_isStepAdvanceBuffered = true;
                m_isStepAdvanceRequested = false;
                return;
            }

            AbilityStepAdvanceWindowTrackSO windowTrack = stepData.StepAdvanceWindowTrack;
            bool isWindowActive = windowTrack.TryGetActiveWindow<AbilityStepAdvanceWindowData>(currentNormalizedTime, out _);
            bool hasCrossedWindow = windowTrack.TryGetCrossedWindow<AbilityStepAdvanceWindowData>(
                previousNormalizedTime, currentNormalizedTime, out _);
            if (!isWindowActive && !hasCrossedWindow)
            {
                if (!windowTrack.HasWindowAtOrAfter(currentNormalizedTime))
                    m_isStepAdvanceRequested = false;

                return;
            }

            m_isStepAdvanceBuffered = true;
            m_isStepAdvanceRequested = false;
        }

        // 尝试推进到下一段能力
        private bool TryAdvanceStep(EnemyActionContext actionContext)
        {
            if (!m_isStepAdvanceBuffered)
                return false;

            int nextStepIndex = m_currentStepIndex + 1;
            if (nextStepIndex >= m_currentConfig.StepCount)
                return false;

            m_currentStepIndex = nextStepIndex;
            EnterCurrentStepBegin(actionContext);
            return IsRunning;
        }

        // 完成当前能力
        private void Finish(EnemyActionContext actionContext)
        {
            IsRunning = false;
            IsFinished = true;
            m_isStepAdvanceRequested = false;
            m_isStepAdvanceBuffered = false;
            m_stepTimer.Complete();
            actionContext.FinishAbility();
        }

        // 重置时间轴内部状态
        private void Reset()
        {
            CurrentAbilityType = null;
            m_currentConfig = null;
            m_currentStepIndex = -1;
            CurrentPhase = AbilityStepPhase.Begin;
            IsRunning = false;
            IsFinished = false;
            m_isStepAdvanceRequested = false;
            m_isStepAdvanceBuffered = false;
            m_stepTimer.Reset();
        }
    }
}
