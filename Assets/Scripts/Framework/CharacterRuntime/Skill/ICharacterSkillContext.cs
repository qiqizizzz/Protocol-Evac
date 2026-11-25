/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 角色技能时间轴所需上下文契约
 * │  类    名: ICharacterSkillContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using System;
using Framework.Character.Runtime.Contexts;

namespace Framework.Character.Runtime.Skill
{
    public interface ICharacterSkillContext<TStateId> where TStateId : struct, Enum
    {
        CharacterActionContext<TStateId> Action { get; }
        CharacterInputContext Input { get; }
    }
}
