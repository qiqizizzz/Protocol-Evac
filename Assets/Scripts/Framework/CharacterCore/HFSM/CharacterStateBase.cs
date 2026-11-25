/*
 * ┌──────────────────────────────────┐
 * │  描    述: 角色状态基类，定义层级状态机的最小状态契约
 * │  类    名: CharacterStateBase.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;

namespace Framework.Character.HFSM
{
    /// <summary>
    /// 角色所有状态的抽象基类，状态标识由具体角色的枚举决定
    /// </summary>
    /// <typeparam name="TStateId">角色状态标识枚举，默认值表示空状态</typeparam>
    public abstract class CharacterStateBase<TStateId> where TStateId : struct, Enum
    {
        // 当前状态标识
        public abstract TStateId Id { get; }

        // 父状态标识，顶层状态返回默认值
        public virtual TStateId ParentId => default;

        // 进入状态
        public virtual void Enter()
        {
        }

        // 退出状态
        public virtual void Exit()
        {
        }

        // 每帧逻辑
        public virtual void Tick(float deltaTime)
        {
        }

        // 每物理帧逻辑
        public virtual void FixedTick(float fixedDeltaTime)
        {
        }
    }
}
