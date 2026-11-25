/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家复合状态类
 * │  类    名: PlayerCompositeState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.Character.HFSM;

namespace Module.ChaisakiPlayer.HFSM
{
    // 千咲复合状态基类，子状态入口约定来自 Character.Core
    public abstract class PlayerCompositeState : CharacterCompositeState<PlayerStateId>
    {
    }
}
