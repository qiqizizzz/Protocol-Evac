/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家技能时间轴控制器
 * │  类    名: PlayerSkillController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.Controller;
using Module.Combat.Hitbox;
using Module.Player.Context;
using Module.Player.Skill;
using Module.Player.Skill.Data;
using UnityEngine;
using Utils.log;

namespace Module.Player.Skill.Core
{
    public sealed class PlayerSkillController : BaseController
    {
        private readonly PlayerContext m_context;
        private readonly CombatHitbox m_combatHitbox;
        private readonly GameObject m_damageSource;
        private readonly Dictionary<PlayerSkillType, PlayerSkillConfigSO> m_skillConfigs;
        private readonly PlayerSkillTimeline m_timeline;

        private int m_hitWindowStepIndex;
        private bool m_isHitWindowOpen;

        public PlayerSkillType? CurrentSkillType => m_timeline.CurrentSkillType;
        public int CurrentStepIndex => m_timeline.CurrentStepIndex;
        public float NormalizedTime => m_timeline.NormalizedTime;
        public bool IsRunning => m_timeline.IsRunning;
        public bool IsFinished => m_timeline.IsFinished;
        public PlayerSkillStepData CurrentStep => m_timeline.CurrentStep;

        // 创建玩家技能控制器
        public PlayerSkillController(PlayerContext context, CombatHitbox combatHitbox, GameObject damageSource)
        {
            m_context = context;
            m_combatHitbox = combatHitbox;
            m_damageSource = damageSource;
            m_skillConfigs = new Dictionary<PlayerSkillType, PlayerSkillConfigSO>();
            m_timeline = new PlayerSkillTimeline();
            m_hitWindowStepIndex = -1;
        }

        #region 生命周期
        public void Open(PlayerSkillType skillType)
        {
            Close();

            if (!m_skillConfigs.TryGetValue(skillType, out PlayerSkillConfigSO config))
            {
                QLog.Error($"打开玩家技能失败：未注册技能配置 {skillType}");
                return;
            }

            if (config.StepCount == 0)
            {
                QLog.Error($"打开玩家技能失败：{skillType} 未配置任何技能段落");
                return;
            }

            m_timeline.Open(skillType, config, m_context);
            SyncHitWindow();
        }

        public override void Tick(float deltaTime)
        {
            m_timeline.Tick(deltaTime, m_context);
            SyncHitWindow();
        }

        public void Close()
        {
            CloseHitWindow();
            m_timeline.Close(m_context);
        }

        // 销毁时关闭当前技能
        protected override void OnDestroy()
        {
            Close();
        }
        #endregion

        // 请求时间轴在满足推进窗口后进入下一段
        public void RequestNextStep()
        {
            m_timeline.RequestNextStep();
        }

        // 注册玩家技能配置
        public void RegisterConfig(PlayerSkillType skillType, PlayerSkillConfigSO config)
        {
            if (config == null)
            {
                QLog.Error($"注册玩家技能配置失败：{skillType} 的配置为空");
                return;
            }

            m_skillConfigs[skillType] = config;
        }

        // 根据当前技能段落与时间轴同步命中窗口
        private void SyncHitWindow()
        {
            if (!m_timeline.IsRunning)
            {
                CloseHitWindow();
                return;
            }

            PlayerSkillStepData stepData = m_timeline.CurrentStep;
            if (!stepData.TryGetHitWindow(out float openNormalizedTime, out float closeNormalizedTime))
            {
                CloseHitWindow();
                return;
            }

            float normalizedTime = m_timeline.NormalizedTime;
            bool isInHitWindow = normalizedTime >= openNormalizedTime && normalizedTime <= closeNormalizedTime;
            if (!isInHitWindow)
            {
                CloseHitWindow();
                return;
            }

            if (m_isHitWindowOpen && m_hitWindowStepIndex == m_timeline.CurrentStepIndex)
                return;

            CloseHitWindow();
            m_combatHitbox.Open(stepData.Damage, m_damageSource);
            m_isHitWindowOpen = true;
            m_hitWindowStepIndex = m_timeline.CurrentStepIndex;
        }

        // 关闭当前命中窗口并重置段落记录
        private void CloseHitWindow()
        {
            if (!m_isHitWindowOpen)
                return;

            m_combatHitbox.Close();
            m_isHitWindowOpen = false;
            m_hitWindowStepIndex = -1;
        }
    }
}
