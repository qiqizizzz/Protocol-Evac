/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 射击角色动作上下文
 * │  类    名: ShooterActionContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using Framework.Character.Runtime.Contexts;
using Module.ShootingPlayer.HFSM;

namespace Module.ShootingPlayer.Context
{
    public sealed class ShooterActionContext : CharacterActionContext<ShooterStateId>
    {
    }
}
