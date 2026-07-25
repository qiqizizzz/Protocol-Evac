/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家动画参数解析器，负责将状态机结果转换为动画表现参数
 * │  类    名: PlayerAnimResolver.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Player.HFSM;
using Utils.log;

namespace Module.Player.HFSM.Animation
{
    public sealed class PlayerAnimResolver
    {
        private PlayerStateMachine m_stateMachine;
        private IReadOnlyDictionary<PlayerStateId, PlayerAnimRule.ResolveHandler> m_handlers;//id->委托
        private bool m_isInited;

        // 初始化玩家动画参数解析器依赖
        public void Init(
            PlayerStateMachine stateMachine,
            IReadOnlyDictionary<PlayerStateId, PlayerAnimRule.ResolveHandler> handlers)
        {
            if (stateMachine == null)
            {
                QLog.Error("初始化玩家动画参数解析器失败：PlayerStateMachine 为空");
                return;
            }

            if (handlers == null)
            {
                QLog.Error("初始化玩家动画参数解析器失败：动画规则集合为空");
                return;
            }

            m_stateMachine = stateMachine;
            m_handlers = handlers;
            m_isInited = true;
        }

        // 根据当前玩法状态解析动画参数
        public PlayerAnimParams Resolve()
        {
            PlayerAnimParams animParams = new PlayerAnimParams();
            animParams.Reset();

            if (!m_isInited)
                return animParams;

            if (m_handlers.TryGetValue(m_stateMachine.CurrentLeafStateId, out PlayerAnimRule.ResolveHandler handler))
                handler(ref animParams);

            return animParams;
        }
    }
}
