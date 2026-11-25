/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 千咲移动上下文，保存急停动画选择
 * │  类    名: PlayerMovementContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using Framework.Character.Runtime.Contexts;
using Module.ChaisakiPlayer.HFSM.Animation.Type;

namespace Module.ChaisakiPlayer.Context
{
    public sealed class PlayerMovementContext : CharacterMovementContext
    {
        public PlayerStopAnimationId StopAnimationId { get; set; }

        // 重置移动数据与急停动画标识
        public override void Reset()
        {
            base.Reset();
            StopAnimationId = PlayerStopAnimationId.None;
        }
    }
}
