/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 千咲动作上下文，补充普通攻击段数与阶段
 * │  类    名: PlayerActionContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using Framework.Character.Runtime.Contexts;
using Module.ChaisakiPlayer.HFSM;

namespace Module.ChaisakiPlayer.Context
{
    public sealed class PlayerActionContext : CharacterActionContext<PlayerStateId>
    {
    }
}
