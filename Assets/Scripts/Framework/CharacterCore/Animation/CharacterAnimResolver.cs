/*
 * ┌──────────────────────────────────┐
 * │  描    述: 角色动画参数解析器，将状态机结果转换为动画表现参数
 * │  类    名: CharacterAnimResolver.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Framework.Character.HFSM;

namespace Framework.Character.Animation
{
    /// <summary>
    /// 动画参数解析器，按当前叶子状态命中动画规则并填写动画参数
    /// </summary>
    /// <typeparam name="TStateId">角色状态标识枚举，默认值表示空状态</typeparam>
    /// <typeparam name="TAnimParams">角色动画参数结构体</typeparam>
    public sealed class CharacterAnimResolver<TStateId, TAnimParams>
        where TStateId : struct, Enum
        where TAnimParams : struct, ICharacterAnimParams
    {
        private CharacterStateMachine<TStateId> m_stateMachine;
        private IReadOnlyDictionary<TStateId, CharacterAnimRule<TStateId, TAnimParams>.ResolveHandler> m_handlers;
        private bool m_isInited;

        /// <summary>
        /// 初始化解析器依赖
        /// </summary>
        /// <param name="stateMachine">角色层级状态机</param>
        /// <param name="handlers">状态标识到动画解析委托的映射</param>
        public void Init(CharacterStateMachine<TStateId> stateMachine,
            IReadOnlyDictionary<TStateId, CharacterAnimRule<TStateId, TAnimParams>.ResolveHandler> handlers)
        {
            m_stateMachine = stateMachine;
            m_handlers = handlers;
            m_isInited = true;
        }

        /// <summary>
        /// 根据当前叶子状态解析动画参数
        /// </summary>
        /// <returns>本帧动画参数，未初始化或无规则命中时为复位后的默认值</returns>
        public TAnimParams Resolve()
        {
            TAnimParams animParams = default;
            animParams.Reset();

            if (!m_isInited)
                return animParams;

            if (m_handlers.TryGetValue(m_stateMachine.CurrentLeafStateId,
                    out CharacterAnimRule<TStateId, TAnimParams>.ResolveHandler handler))
                handler(ref animParams);

            return animParams;
        }
    }
}
