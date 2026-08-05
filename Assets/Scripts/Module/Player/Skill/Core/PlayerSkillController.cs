/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家技能时间轴控制器
 * │  类    名: PlayerSkillController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Player.Context;
using Module.Player.Skill;
using Module.Player.Skill.Data;
using Utils.log;

namespace Module.Player.Skill.Core
{
    public sealed class PlayerSkillController
    {
        private readonly PlayerContext m_context;
        private readonly Dictionary<PlayerSkillType, PlayerSkillConfigSO> m_skillConfigs;
        private readonly PlayerSkillTimeline m_timeline;

        public PlayerSkillType? CurrentSkillType => m_timeline.CurrentSkillType;
        public int CurrentStepIndex => m_timeline.CurrentStepIndex;
        public float NormalizedTime => m_timeline.NormalizedTime;
        public bool IsRunning => m_timeline.IsRunning;
        public bool IsFinished => m_timeline.IsFinished;
        public PlayerSkillStepData CurrentStep => m_timeline.CurrentStep;

        // 创建玩家技能控制器
        public PlayerSkillController(PlayerContext context)
        {
            m_context = context;
            m_skillConfigs = new Dictionary<PlayerSkillType, PlayerSkillConfigSO>();
            m_timeline = new PlayerSkillTimeline();
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

        public void Open(PlayerSkillType skillType)
        {
            if (!m_skillConfigs.TryGetValue(skillType, out PlayerSkillConfigSO config))
            {
                QLog.Error($"打开玩家技能失败：未注册技能配置 {skillType}");
                m_timeline.Close(m_context);
                return;
            }

            if (config.StepCount == 0)
            {
                QLog.Error($"打开玩家技能失败：{skillType} 未配置任何技能段落");
                m_timeline.Close(m_context);
                return;
            }

            m_timeline.Open(skillType, config, m_context);
        }
        
        public void Tick(float deltaTime)
        {
            m_timeline.Tick(deltaTime, m_context);
        }

        public void Close()
        {
            m_timeline.Close(m_context);
        }
    }
}
