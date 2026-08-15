/*
 * ┌───────────────────────────────────────────────────────┐
 * │  描    述: 敌人能力控制器，负责配置注册、冷却与命中窗口
 * │  类    名: EnemyAbilityController.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Ability.Data;
using Module.Ability.Hit;
using Module.Combat.Hitbox;
using Module.Enemy.Ability.Config;
using Module.Enemy.Context.Runtime;
using UnityEngine;
using Utils.log;

namespace Module.Enemy.Ability.Core
{
    public sealed class EnemyAbilityController
    {
        private readonly EnemyActionContext m_actionContext;
        private readonly AbilityHitWindowController m_hitWindowController;
        private readonly Dictionary<EnemyAbilityType, AbilityConfigSO> m_abilityConfigs;
        private readonly Dictionary<EnemyAbilityType, float> m_cooldownRemainingTimes;
        private readonly List<EnemyAbilityType> m_registeredAbilityTypes;
        private readonly EnemyAbilityTimeline m_timeline;

        public EnemyAbilityType? CurrentAbilityType => m_timeline.CurrentAbilityType;
        public int CurrentStepIndex => m_timeline.CurrentStepIndex;
        public AbilityStepPhase CurrentPhase => m_timeline.CurrentPhase;
        public float NormalizedTime => m_timeline.NormalizedTime;
        public bool IsRunning => m_timeline.IsRunning;
        public bool IsFinished => m_timeline.IsFinished;
        public AbilityStepData CurrentStep => m_timeline.CurrentStep;
        public bool IsMovementLocked => m_actionContext.IsMovementLocked;
        public bool CanRotate => m_actionContext.CanRotate;

        // 创建敌人能力控制器
        public EnemyAbilityController(EnemyActionContext actionContext, CombatHitbox combatHitbox, GameObject damageSource)
        {
            m_actionContext = actionContext;
            m_hitWindowController = new AbilityHitWindowController(combatHitbox, damageSource);
            m_abilityConfigs = new Dictionary<EnemyAbilityType, AbilityConfigSO>();
            m_cooldownRemainingTimes = new Dictionary<EnemyAbilityType, float>();
            m_registeredAbilityTypes = new List<EnemyAbilityType>();
            m_timeline = new EnemyAbilityTimeline();
        }

        // 推进能力时间轴、冷却与命中窗口
        public void Tick(float deltaTime)
        {
            TickCooldowns(deltaTime);

            bool wasRunning = m_timeline.IsRunning;
            EnemyAbilityType? abilityType = m_timeline.CurrentAbilityType;
            m_timeline.Tick(deltaTime, m_actionContext);
            if (wasRunning && m_timeline.IsFinished && abilityType.HasValue)
                StartCooldown(abilityType.Value);

            SyncHitWindow();
        }

        // 注册敌人能力配置
        public void RegisterConfig(EnemyAbilityType abilityType, AbilityConfigSO config)
        {
            if (config == null)
            {
                QLog.Error($"注册敌人能力配置失败：{abilityType} 的配置为空");
                return;
            }

            if (!m_abilityConfigs.ContainsKey(abilityType))
                m_registeredAbilityTypes.Add(abilityType);

            m_abilityConfigs[abilityType] = config;
            m_cooldownRemainingTimes[abilityType] = 0f;
        }

        // 判断指定能力当前是否可以打开
        public bool CanOpen(EnemyAbilityType abilityType)
        {
            if (m_timeline.IsRunning || !m_abilityConfigs.ContainsKey(abilityType))
                return false;

            return m_cooldownRemainingTimes[abilityType] <= 0f;
        }

        // 尝试打开指定敌人能力
        public bool TryOpen(EnemyAbilityType abilityType)
        {
            if (!CanOpen(abilityType))
                return false;

            AbilityConfigSO config = m_abilityConfigs[abilityType];
            if (config.StepCount == 0)
            {
                QLog.Error($"打开敌人能力失败：{abilityType} 未配置任何状态动画段落");
                return false;
            }

            ApplyControlSettings(abilityType, config);
            m_timeline.Open(abilityType, config, m_actionContext);
            SyncHitWindow();
            return m_timeline.IsRunning;
        }

        // 请求当前能力推进到下一段
        public void RequestNextStep()
        {
            m_timeline.RequestNextStep();
        }

        // 关闭当前能力并清理命中窗口
        public void Close()
        {
            m_hitWindowController.Close();
            m_timeline.Close(m_actionContext);
        }

        // 查询指定能力是否处于冷却
        public bool IsCoolingDown(EnemyAbilityType abilityType)
        {
            return m_cooldownRemainingTimes.TryGetValue(abilityType, out float remainingTime)
                && remainingTime > 0f;
        }

        // 推进全部已注册能力的冷却
        private void TickCooldowns(float deltaTime)
        {
            for (int i = 0; i < m_registeredAbilityTypes.Count; i++)
            {
                EnemyAbilityType abilityType = m_registeredAbilityTypes[i];
                float remainingTime = m_cooldownRemainingTimes[abilityType];
                if (remainingTime > 0f)
                    m_cooldownRemainingTimes[abilityType] = Mathf.Max(0f, remainingTime - deltaTime);
            }
        }

        // 根据能力配置写入动作控制约束
        private void ApplyControlSettings(EnemyAbilityType abilityType, AbilityConfigSO config)
        {
            if (abilityType == EnemyAbilityType.NormalAttack)
            {
                EnemyNormalAttackConfigSO normalAttackConfig = (EnemyNormalAttackConfigSO)config;
                m_actionContext.BeginAbility(abilityType, normalAttackConfig.LockMovement, normalAttackConfig.CanRotate);
            }
        }

        // 根据能力配置开始冷却
        private void StartCooldown(EnemyAbilityType abilityType)
        {
            if (abilityType == EnemyAbilityType.NormalAttack)
            {
                EnemyNormalAttackConfigSO normalAttackConfig =
                    (EnemyNormalAttackConfigSO)m_abilityConfigs[abilityType];
                m_cooldownRemainingTimes[abilityType] = normalAttackConfig.Cooldown;
            }
        }

        // 根据当前能力阶段同步命中窗口
        private void SyncHitWindow()
        {
            if (!m_timeline.IsRunning || m_timeline.CurrentPhase != AbilityStepPhase.Begin)
            {
                m_hitWindowController.Close();
                return;
            }

            AbilityStepData stepData = m_timeline.CurrentStep;
            if (!stepData.UseHitWindow)
            {
                m_hitWindowController.Close();
                return;
            }

            m_hitWindowController.Sync(stepData.BeginHitWindowTrack, m_timeline.NormalizedTime,
                m_timeline.CurrentStepIndex);
        }
    }
}

