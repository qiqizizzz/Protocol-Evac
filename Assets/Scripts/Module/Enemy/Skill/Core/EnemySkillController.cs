/*
 * ┌───────────────────────────────────────────────────────┐
 * │  描    述: 敌人技能控制器，负责配置注册、冷却、命中与特效窗口
 * │  类    名: EnemySkillController.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Ability.Audio;
using Module.Ability.Data;
using Module.Ability.Hit;
using Module.Ability.Vfx;
using Module.Combat.Damage;
using Module.Combat.Hitbox;
using Module.Enemy.Context.Runtime;
using Module.Enemy.Skill.Data;
using UnityEngine;
using Utils.log;

namespace Module.Enemy.Skill.Core
{
    public sealed class EnemySkillController
    {
        private readonly EnemyActionContext m_actionContext;
        private readonly AbilityHitWindowController m_hitWindowController;
        private readonly AbilityVfxWindowController m_vfxWindowController;
        private readonly AbilityAudioWindowController m_audioWindowController;
        private readonly CombatHitbox m_combatHitbox;
        private readonly Dictionary<EnemySkillType, AbilityConfigSO> m_skillConfigs;
        private readonly Dictionary<EnemySkillType, float> m_cooldownRemainingTimes;
        private readonly List<EnemySkillType> m_registeredSkillTypes;
        private readonly EnemySkillTimeline m_timeline;

        public EnemySkillType? CurrentSkillType => m_timeline.CurrentSkillType;
        public int CurrentStepIndex => m_timeline.CurrentStepIndex;
        public AbilityStepPhase CurrentPhase => m_timeline.CurrentPhase;
        public float NormalizedTime => m_timeline.NormalizedTime;
        public bool IsRunning => m_timeline.IsRunning;
        public bool IsFinished => m_timeline.IsFinished;
        public AbilityStepData CurrentStep => m_timeline.CurrentStep;
        public bool IsMovementLocked => m_actionContext.IsMovementLocked;
        public bool CanRotate => m_actionContext.CanRotate;

        // 创建敌人技能控制器
        public EnemySkillController(EnemyActionContext actionContext, CombatHitbox combatHitbox, GameObject damageSource)
        {
            m_actionContext = actionContext;
            m_combatHitbox = combatHitbox;
            m_hitWindowController = new AbilityHitWindowController(combatHitbox, damageSource);
            m_vfxWindowController = new AbilityVfxWindowController(damageSource);
            m_audioWindowController = new AbilityAudioWindowController(damageSource);
            m_skillConfigs = new Dictionary<EnemySkillType, AbilityConfigSO>();
            m_cooldownRemainingTimes = new Dictionary<EnemySkillType, float>();
            m_registeredSkillTypes = new List<EnemySkillType>();
            m_timeline = new EnemySkillTimeline();
            m_combatHitbox.OnHitConfirmed += HandleHitConfirmed;
        }

        // 推进技能时间轴、冷却与命中窗口
        public void Tick(float deltaTime)
        {
            TickCooldowns(deltaTime);

            bool wasRunning = m_timeline.IsRunning;
            EnemySkillType? skillType = m_timeline.CurrentSkillType;
            m_timeline.Tick(deltaTime, m_actionContext);
            if (wasRunning && m_timeline.IsFinished && skillType.HasValue)
                StartCooldown(skillType.Value);

            SyncHitWindow();
            SyncVfxWindow();
            SyncAudioWindow();
        }

        // 注册敌人技能配置
        public void RegisterConfig(EnemySkillType skillType, AbilityConfigSO config)
        {
            if (config == null)
            {
                QLog.Error($"注册敌人技能配置失败：{skillType} 的配置为空");
                return;
            }

            if (!m_skillConfigs.ContainsKey(skillType))
                m_registeredSkillTypes.Add(skillType);

            m_skillConfigs[skillType] = config;
            m_cooldownRemainingTimes[skillType] = 0f;
        }

        // 判断指定技能当前是否可以打开
        public bool CanOpen(EnemySkillType skillType)
        {
            if (m_timeline.IsRunning || !m_skillConfigs.ContainsKey(skillType))
                return false;

            return m_cooldownRemainingTimes[skillType] <= 0f;
        }

        // 尝试打开指定敌人技能
        public bool TryOpen(EnemySkillType skillType)
        {
            if (!CanOpen(skillType))
                return false;

            AbilityConfigSO config = m_skillConfigs[skillType];
            if (config.StepCount == 0)
            {
                QLog.Error($"打开敌人技能失败：{skillType} 未配置任何状态动画段落");
                return false;
            }

            ApplyControlSettings(skillType, config);
            m_timeline.Open(skillType, config, m_actionContext);
            SyncHitWindow();
            SyncVfxWindow();
            SyncAudioWindow();
            return m_timeline.IsRunning;
        }

        // 请求当前技能推进到下一段
        public void RequestNextStep()
        {
            m_timeline.RequestNextStep();
        }

        // 关闭当前技能并清理命中窗口
        public void Close()
        {
            EnemySkillType? skillType = m_timeline.CurrentSkillType;
            bool wasRunning = m_timeline.IsRunning;
            m_hitWindowController.Close();
            m_vfxWindowController.Close();
            m_audioWindowController.Close();
            m_timeline.Close(m_actionContext);
            if (wasRunning && skillType.HasValue)
                StartCooldown(skillType.Value);
        }

        // 释放敌人技能控制器持有的运行时订阅
        public void UnInit()
        {
            m_combatHitbox.OnHitConfirmed -= HandleHitConfirmed;
            Close();
        }

        // 查询指定技能是否处于冷却
        public bool IsCoolingDown(EnemySkillType skillType)
        {
            return m_cooldownRemainingTimes.TryGetValue(skillType, out float remainingTime)
                && remainingTime > 0f;
        }

        // 推进全部已注册技能的冷却
        private void TickCooldowns(float deltaTime)
        {
            for (int i = 0; i < m_registeredSkillTypes.Count; i++)
            {
                EnemySkillType skillType = m_registeredSkillTypes[i];
                float remainingTime = m_cooldownRemainingTimes[skillType];
                if (remainingTime > 0f)
                    m_cooldownRemainingTimes[skillType] = Mathf.Max(0f, remainingTime - deltaTime);
            }
        }

        // 根据技能配置写入动作控制约束
        private void ApplyControlSettings(EnemySkillType skillType, AbilityConfigSO config)
        {
            if (skillType == EnemySkillType.NormalAttack)
            {
                EnemyNormalAttackConfigSO normalAttackConfig = (EnemyNormalAttackConfigSO)config;
                m_actionContext.BeginSkill(skillType, normalAttackConfig.LockMovement, normalAttackConfig.CanRotate);
            }
        }

        // 根据技能配置开始冷却
        public void StartCooldown(EnemySkillType skillType)
        {
            if (skillType == EnemySkillType.NormalAttack)
            {
                EnemyNormalAttackConfigSO normalAttackConfig =
                    (EnemyNormalAttackConfigSO)m_skillConfigs[skillType];
                m_cooldownRemainingTimes[skillType] = normalAttackConfig.Cooldown;
            }
        }

        // 根据当前技能阶段同步命中窗口
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

        // 根据当前技能阶段同步特效窗口
        private void SyncVfxWindow()
        {
            if (!m_timeline.IsRunning || m_timeline.CurrentPhase != AbilityStepPhase.Begin)
            {
                m_vfxWindowController.Close();
                return;
            }

            AbilityStepData stepData = m_timeline.CurrentStep;
            if (!stepData.UseVfxWindow)
            {
                m_vfxWindowController.Close();
                return;
            }

            m_vfxWindowController.Sync(stepData.VfxWindowTrack, m_timeline.NormalizedTime,
                m_timeline.CurrentStepIndex);
        }

        // 根据当前技能阶段同步音效窗口
        private void SyncAudioWindow()
        {
            if (!m_timeline.IsRunning || m_timeline.CurrentPhase != AbilityStepPhase.Begin)
            {
                m_audioWindowController.Close();
                return;
            }

            AbilityStepData stepData = m_timeline.CurrentStep;
            if (!stepData.UseAudioWindow)
            {
                m_audioWindowController.Close();
                return;
            }

            m_audioWindowController.Sync(stepData.AudioWindowTrack, m_timeline.NormalizedTime,
                m_timeline.CurrentStepIndex);
        }

        // 转发真实命中事件给当前技能表现窗口
        private void HandleHitConfirmed(DamageData damageData, Component hitTarget)
        {
            m_vfxWindowController.PlayHitVfx(damageData, hitTarget);
            m_audioWindowController.PlayHitAudio(damageData, hitTarget);
        }
    }
}
