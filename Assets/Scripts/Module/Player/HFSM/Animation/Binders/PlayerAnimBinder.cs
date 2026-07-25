/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家动画绑定器，负责收集动画规则处理函数
 * │  类    名: PlayerAnimBinder.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Player.HFSM.Animation;
using Module.Player.HFSM;
using Utils.log;

namespace Module.Player.HFSM.Animation.Binders
{
    public sealed class PlayerAnimBinder
    {
        private readonly Dictionary<PlayerStateId, PlayerAnimRule.ResolveHandler> m_handlers =
            new Dictionary<PlayerStateId, PlayerAnimRule.ResolveHandler>();

        public IReadOnlyDictionary<PlayerStateId, PlayerAnimRule.ResolveHandler> Handlers => m_handlers;

        // 绑定一组玩家动画规则
        public void Bind(IReadOnlyList<PlayerAnimRule> rules)
        {
            if (rules == null)
            {
                QLog.Error("绑定玩家动画规则失败：rules 为空");
                return;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                PlayerAnimRule rule = rules[i];
                if (rule.StateId == PlayerStateId.None || rule.Handler == null)
                {
                    QLog.Error("绑定玩家动画规则失败：存在无效规则");
                    continue;
                }

                m_handlers[rule.StateId] = rule.Handler;
            }
        }
    }
}
