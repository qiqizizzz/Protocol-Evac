/*
 * ┌─────────────────────────────────────────────┐
 * │  描    述: 玩家急停动画标识，区分移动档位与左右落脚动作
 * │  类    名: PlayerStopAnimationId.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────┘
 */

namespace Module.Player.Context.Runtime
{
    public enum PlayerStopAnimationId
    {
        None,
        WalkLeft,
        WalkRight,
        RunLeft,
        RunRight,
        SprintLeft,
        SprintRight
    }
}
