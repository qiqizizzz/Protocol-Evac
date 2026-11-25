/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 千咲运行时上下文入口，组合各领域子上下文与角色根节点
 * │  类    名: PlayerContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using Framework.Character.Runtime.Contexts;
using Module.ChaisakiPlayer.HFSM;
using Module.ChaisakiPlayer.HFSM.Animation.Type;
using UnityEngine;

namespace Module.ChaisakiPlayer.Context
{
    public sealed class PlayerContext :
        CharacterContext<PlayerStateId, PlayerHurtAnimationId, PlayerActionContext, PlayerMovementContext>
    {
        // 创建玩家运行时上下文入口
        public PlayerContext(Transform transform) : base(transform)
        {
        }
    }
}
