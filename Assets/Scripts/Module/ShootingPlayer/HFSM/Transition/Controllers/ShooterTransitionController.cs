/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色状态转换控制器，负责装配状态转换规则
* │  类    名: ShooterTransitionController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Framework.QTower.Controller;
using Module.ChaisakiPlayer.Context;
using Module.ShootingPlayer.Config;
using Module.ShootingPlayer.HFSM.Runtime;
using Module.ShootingPlayer.HFSM.Transition.Rules;
using Utils.log;

using ShooterTransitionRule = Framework.Character.Transition.CharacterTransitionRule<Module.ShootingPlayer.HFSM.ShooterStateId, Module.ShootingPlayer.HFSM.Transition.ShooterTransitionPriority>;

namespace Module.ShootingPlayer.HFSM.Transition.Controllers
{
    // 射击角色状态转换控制器，只负责收集规则
    public sealed class ShooterTransitionController : BaseController
    {
        private readonly List<ShooterTransitionRule> m_rules = new List<ShooterTransitionRule>();
        private readonly PlayerContext m_context;
        private readonly ShooterRuntimeState m_runtimeState;
        private readonly ShooterAimConfigSO m_aimConfig;
        private readonly ShooterEvadeConfigSO m_evadeConfig;
        private readonly ShooterShootConfigSO m_shootConfig;

        public IReadOnlyList<ShooterTransitionRule> Rules => m_rules;

        // 创建射击角色状态转换控制器
        public ShooterTransitionController(PlayerContext context, ShooterRuntimeState runtimeState,
            ShooterAimConfigSO aimConfig, ShooterEvadeConfigSO evadeConfig, ShooterShootConfigSO shootConfig)
        {
            m_context = context;
            m_runtimeState = runtimeState;
            m_aimConfig = aimConfig;
            m_evadeConfig = evadeConfig;
            m_shootConfig = shootConfig;
        }

        // 初始化射击角色状态转换规则
        protected override void OnInit()
        {
            Register(ShooterMoveTransitionRules.Create(m_context, m_runtimeState, m_aimConfig));
            Register(ShooterSkillTransitionRules.Create(m_context, m_runtimeState, m_shootConfig));
            Register(ShooterActionTransitionRules.Create(m_context, m_runtimeState, m_evadeConfig));
            Register(ShooterDamageTransitionRules.Create(m_context, m_runtimeState));
        }

        // 注册一组射击角色状态转换规则
        private void Register(IReadOnlyList<ShooterTransitionRule> rules)
        {
            if (rules == null)
            {
                QLog.Error("注册射击角色状态转换规则失败：rules 为空");
                return;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i] == null)
                {
                    QLog.Error("注册射击角色状态转换规则失败：存在空规则");
                    continue;
                }

                m_rules.Add(rules[i]);
            }
        }
    }
}
