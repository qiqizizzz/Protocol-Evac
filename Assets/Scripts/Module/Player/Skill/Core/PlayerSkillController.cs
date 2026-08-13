/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家技能时间轴控制器
 * │  类    名: PlayerSkillController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.Controller;
using System.Collections.Generic;
using Module.Combat.Hitbox;
using Module.Ability.Window.Hit;
using Module.Player.Context;
using Module.Player.Skill;
using Module.Player.Skill.Data;
using UnityEngine;
using Utils.log;

namespace Module.Player.Skill.Core
{
    public sealed class PlayerSkillController : BaseController
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        private readonly PlayerContext m_context;
        private readonly CombatHitbox m_combatHitbox;
        private readonly GameObject m_damageSource;
        private readonly Dictionary<PlayerSkillType, PlayerSkillConfigSO> m_skillConfigs;
        private readonly PlayerSkillTimeline m_timeline;

        private int m_hitWindowStepIndex;
        private string m_activeHitWindowId;
        private Vector2 m_previousMoveInput;
        private bool m_wasSprintActive;

        public PlayerSkillType? CurrentSkillType => m_timeline.CurrentSkillType;
        public int CurrentStepIndex => m_timeline.CurrentStepIndex;
        public PlayerSkillStepPhase CurrentPhase => m_timeline.CurrentPhase;
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
            RecordCancelInputState();
            SyncHitWindow();
        }

        public override void Tick(float deltaTime)
        {
            if (TryFinishEarlyByPlayerInput())
            {
                SyncHitWindow();
                return;
            }

            m_timeline.Tick(deltaTime, m_context);
            SyncHitWindow();
        }

        public void Close()
        {
            CloseHitWindow();
            m_timeline.Close(m_context);
            ResetCancelInputState();
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

        // 根据新的移动或疾跑意图尝试提前结束当前技能
        private bool TryFinishEarlyByPlayerInput()
        {
            if (!m_timeline.IsRunning)
                return false;

            bool hasMoveInput = m_context.Input.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
            bool hadMoveInput = m_previousMoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
            bool hasNewMoveInput = hasMoveInput && !hadMoveInput;
            bool hasNewSprintInput = m_context.Input.IsSprintActive && !m_wasSprintActive;
            RecordCancelInputState();

            if (!hasNewMoveInput && !hasNewSprintInput)
                return false;

            return m_timeline.TryFinishEarly(m_context);
        }

        // 记录本帧取消输入状态
        private void RecordCancelInputState()
        {
            m_previousMoveInput = m_context.Input.MoveInput;
            m_wasSprintActive = m_context.Input.IsSprintActive;
        }

        // 重置取消输入状态
        private void ResetCancelInputState()
        {
            m_previousMoveInput = Vector2.zero;
            m_wasSprintActive = false;
        }

        // 根据当前技能段落与时间轴同步命中窗口
        private void SyncHitWindow()
        {
            if (!m_timeline.IsRunning)
            {
                CloseHitWindow();
                return;
            }

            if (m_timeline.CurrentPhase != PlayerSkillStepPhase.Begin)
            {
                CloseHitWindow();
                return;
            }

            PlayerSkillStepData stepData = m_timeline.CurrentStep;
            if (!stepData.UseHitWindow)
            {
                CloseHitWindow();
                return;
            }

            AbilityHitWindowTrackSO windowTrack = stepData.BeginHitWindowTrack;
            if (windowTrack == null)
            {
                CloseHitWindow();
                return;
            }

            float normalizedTime = m_timeline.NormalizedTime;
            if (!windowTrack.TryGetActiveWindow(normalizedTime, out AbilityHitWindowData activeWindow))
            {
                CloseHitWindow();
                return;
            }

            if (m_hitWindowStepIndex == m_timeline.CurrentStepIndex && m_activeHitWindowId == activeWindow.Id)
                return;

            CloseHitWindow();
            m_combatHitbox.Open(activeWindow.Damage, m_damageSource);
            m_hitWindowStepIndex = m_timeline.CurrentStepIndex;
            m_activeHitWindowId = activeWindow.Id;
        }

        // 关闭当前命中窗口并重置段落记录
        private void CloseHitWindow()
        {
            if (m_hitWindowStepIndex < 0)
                return;

            m_combatHitbox.Close();
            m_hitWindowStepIndex = -1;
            m_activeHitWindowId = null;
        }
    }
}
