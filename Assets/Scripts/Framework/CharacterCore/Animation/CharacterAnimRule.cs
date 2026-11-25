/*
 * ┌──────────────────────────────────┐
 * │  描    述: 角色动画规则，描述状态与动画参数解析函数的映射
 * │  类    名: CharacterAnimRule.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;

namespace Framework.Character.Animation
{
    /// <summary>
    /// 单条动画规则，把一个叶子状态绑定到动画参数解析委托
    /// </summary>
    /// <typeparam name="TStateId">角色状态标识枚举，默认值表示空状态</typeparam>
    /// <typeparam name="TAnimParams">角色动画参数结构体</typeparam>
    public readonly struct CharacterAnimRule<TStateId, TAnimParams>
        where TStateId : struct, Enum
        where TAnimParams : struct, ICharacterAnimParams
    {
        // 动画参数解析委托，由具体角色的动画规则提供实现
        public delegate void ResolveHandler(ref TAnimParams animParams);

        // 规则生效的叶子状态
        public TStateId StateId { get; }

        // 命中状态时要执行的动画参数解析委托
        public ResolveHandler Handler { get; }

        /// <summary>
        /// 创建动画规则
        /// </summary>
        /// <param name="stateId">规则生效的叶子状态</param>
        /// <param name="handler">动画参数解析委托</param>
        public CharacterAnimRule(TStateId stateId, ResolveHandler handler)
        {
            StateId = stateId;
            Handler = handler;
        }
    }
}
