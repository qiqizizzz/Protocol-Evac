/*
 * ┌──────────────────────────────────┐
 * │  描    述: 角色复合状态基类，承载子状态默认入口
 * │  类    名: CharacterCompositeState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;

namespace Framework.Character.HFSM
{
    /// <summary>
    /// 复合状态基类，进入该状态时状态机会自动展开到默认子状态
    /// </summary>
    /// <typeparam name="TStateId">角色状态标识枚举，默认值表示空状态</typeparam>
    public abstract class CharacterCompositeState<TStateId> : CharacterStateBase<TStateId>,
        ICharacterCompositeState<TStateId> where TStateId : struct, Enum
    {
        /// <summary>
        /// 返回进入该复合状态时默认激活的直接子状态
        /// </summary>
        /// <returns>默认子状态标识</returns>
        public abstract TStateId GetInitialChildId();
    }
}
