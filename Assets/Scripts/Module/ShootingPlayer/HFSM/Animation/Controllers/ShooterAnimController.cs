/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色动画控制器，负责装配动画规则处理函数
* │  类    名: ShooterAnimController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Framework.QTower.Controller;
using Module.Player.Context;
using Module.ShootingPlayer.HFSM.Animation.Rules;
using Utils.log;

namespace Module.ShootingPlayer.HFSM.Animation.Controllers
{
    // 射击角色动画控制器，只负责收集动画规则
    public sealed class ShooterAnimController : BaseController
    {
        private readonly Dictionary<ShooterStateId, ShooterAnimRule.ResolveHandler> m_handlers =
            new Dictionary<ShooterStateId, ShooterAnimRule.ResolveHandler>();
        private readonly PlayerContext m_context;

        public IReadOnlyDictionary<ShooterStateId, ShooterAnimRule.ResolveHandler> Handlers => m_handlers;

        // 创建射击角色动画控制器
        public ShooterAnimController(PlayerContext context)
        {
            m_context = context;
        }

        // 初始化射击角色动画规则
        protected override void OnInit()
        {
            Register(ShooterMoveAnimRules.Create(m_context));
            Register(ShooterAimAnimRules.Create(m_context));
            Register(ShooterActionAnimRules.Create(m_context));
            Register(ShooterSkillAnimRules.Create(m_context));
            Register(ShooterDisabledAnimRules.Create(m_context));
        }

        // 注册一组射击角色动画规则
        private void Register(IReadOnlyList<ShooterAnimRule> rules)
        {
            if (rules == null)
            {
                QLog.Error("注册射击角色动画规则失败：rules 为空");
                return;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                ShooterAnimRule rule = rules[i];
                if (rule.StateId == ShooterStateId.None || rule.Handler == null)
                {
                    QLog.Error("注册射击角色动画规则失败：存在无效规则");
                    continue;
                }

                m_handlers[rule.StateId] = rule.Handler;
            }
        }
    }
}
