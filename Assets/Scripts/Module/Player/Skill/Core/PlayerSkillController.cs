/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家技能时间轴控制器
 * │  类    名: PlayerSkillController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.Controller;
using Module.Player.Context;
using Module.Player.HFSM;
using Module.Player.HFSM.States.Skill;
using Module.Player.Skill;
using Module.Player.Skill.Data;
using Utils.log;

namespace Module.Player.Skill.Core
{
    public sealed class PlayerSkillController : BaseController
    {
        private readonly PlayerContext m_context;
        private readonly PlayerStateMachine m_stateMachine;
        private readonly Dictionary<PlayerSkillType, PlayerSkillConfigSO> m_skillConfigs;
        private readonly PlayerSkillTimeline m_timeline;

        public PlayerSkillType? CurrentSkillType => m_timeline.CurrentSkillType;
        public int CurrentStepIndex => m_timeline.CurrentStepIndex;
        public float NormalizedTime => m_timeline.NormalizedTime;
        public bool IsRunning => m_timeline.IsRunning;
        public bool IsFinished => m_timeline.IsFinished;
        public PlayerSkillStepData CurrentStep => m_timeline.CurrentStep;

        // 创建玩家技能控制器
        public PlayerSkillController(PlayerContext context, PlayerStateMachine stateMachine)
        {
            m_context = context;
            m_stateMachine = stateMachine;
            m_skillConfigs = new Dictionary<PlayerSkillType, PlayerSkillConfigSO>();
            m_timeline = new PlayerSkillTimeline();
        }

        #region 生命周期
        // 初始化技能运行生命周期
        protected override void OnInit()
        {
            if (!m_skillConfigs.TryGetValue(PlayerSkillType.NormalAttack, out PlayerSkillConfigSO config))
            {
                QLog.Error("初始化玩家技能生命周期失败：未注册普通攻击配置");
                return;
            }

            if (config is not PlayerNormalAttackConfigSO normalAttackConfig)
            {
                QLog.Error("初始化玩家技能生命周期失败：普通攻击配置类型不匹配");
                return;
            }

            m_stateMachine.RegisterState(new PlayerNormalAttackState(m_context, this, normalAttackConfig));
        }

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
        }

        public override void Tick(float deltaTime)
        {
            m_timeline.Tick(deltaTime, m_context);
        }

        public void Close()
        {
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
    }
}
