/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 角色运行时测试上下文，提供最小独立状态标识
 * │  类    名: CharacterRuntimeTestContext.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System;
using Framework.Character.Runtime.Contexts;
using UnityEngine;

namespace Tests.CharacterRuntime
{
    public enum TestStateId
    {
        Idle,
        Attack
    }

    public enum TestHurtAnimationId
    {
        None,
        LightHit
    }

    internal sealed class CharacterRuntimeTestContext :
        CharacterContext<TestStateId, TestHurtAnimationId, CharacterActionContext<TestStateId>, CharacterMovementContext>
    {
        // 以场景 Transform 创建独立测试上下文
        public CharacterRuntimeTestContext(Transform transform) : base(transform)
        {
        }
    }
}
