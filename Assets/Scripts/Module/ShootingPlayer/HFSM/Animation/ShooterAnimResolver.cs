/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色动画参数解析器，将状态机结果转换为动画表现参数
* │  类    名: ShooterAnimResolver.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;

namespace Module.ShootingPlayer.HFSM.Animation
{
    public sealed class ShooterAnimResolver
    {
        private ShooterStateMachine m_stateMachine;
        private IReadOnlyDictionary<ShooterStateId, ShooterAnimRule.ResolveHandler> m_handlers;
        private bool m_isInited;

        // 初始化解析器依赖
        public void Init(ShooterStateMachine stateMachine,
            IReadOnlyDictionary<ShooterStateId, ShooterAnimRule.ResolveHandler> handlers)
        {
            m_stateMachine = stateMachine;
            m_handlers = handlers;
            m_isInited = true;
        }

        // 根据当前叶子状态解析动画参数
        public ShooterAnimParams Resolve()
        {
            ShooterAnimParams animParams = new ShooterAnimParams();
            animParams.Reset();

            if (!m_isInited)
                return animParams;

            if (m_handlers.TryGetValue(m_stateMachine.CurrentLeafStateId,
                    out ShooterAnimRule.ResolveHandler handler))
                handler(ref animParams);

            return animParams;
        }
    }
}
