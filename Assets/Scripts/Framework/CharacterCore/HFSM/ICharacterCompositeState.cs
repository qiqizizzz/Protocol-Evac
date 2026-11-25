/*
 * ┌──────────────────────────────────┐
 * │  描    述: 复合状态接口，供状态机展开默认子状态路径
 * │  类    名: ICharacterCompositeState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;

namespace Framework.Character.HFSM
{
    /// <summary>
    /// 复合状态契约，状态机只依赖该接口完成父子校验与路径展开
    /// </summary>
    /// <typeparam name="TStateId">角色状态标识枚举，默认值表示空状态</typeparam>
    public interface ICharacterCompositeState<TStateId> where TStateId : struct, Enum
    {
        /// <summary>
        /// 返回进入该复合状态时默认激活的子状态标识
        /// </summary>
        /// <returns>默认子状态标识</returns>
        TStateId GetInitialChildId();
    }
}
