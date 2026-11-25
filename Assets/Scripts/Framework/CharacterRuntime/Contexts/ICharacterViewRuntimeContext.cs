/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 角色视角模块所需运行时上下文契约
 * │  类    名: ICharacterViewRuntimeContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Framework.Character.Runtime.Contexts
{
    public interface ICharacterViewRuntimeContext
    {
        Transform Transform { get; }
        CharacterInputContext Input { get; }
        CharacterViewContext View { get; }
        ICharacterDamageContext Damage { get; }
    }
}
