/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家复合状态类
 * │  类    名: PlayerCompositeState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Player.HFSM
{
    public abstract class PlayerCompositeState : BasePlayerState
    {
        /// <summary>
        /// 返回进入该复合状态时默认激活的直接子状态。
        /// </summary>
        public abstract PlayerStateId GetInitialChildId();
    }
}