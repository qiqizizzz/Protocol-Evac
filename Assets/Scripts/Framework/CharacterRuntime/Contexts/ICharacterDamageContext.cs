/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 角色伤害上下文只读契约
 * │  类    名: ICharacterDamageContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

namespace Framework.Character.Runtime.Contexts
{
    public interface ICharacterDamageContext
    {
        bool IsDead { get; }
    }
}
