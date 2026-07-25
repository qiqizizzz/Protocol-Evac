/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家动画规则，描述状态与动画参数处理函数的映射
 * │  类    名: PlayerAnimRule.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Player.HFSM.Animation
{
    public readonly struct PlayerAnimRule
    {
        //动画参数解析委托
        public delegate void ResolveHandler(ref PlayerAnimParams animParams);

        public PlayerStateId StateId { get; }
        public ResolveHandler Handler { get; }

        // 创建玩家动画规则
        public PlayerAnimRule(PlayerStateId stateId, ResolveHandler handler)
        {
            StateId = stateId;
            Handler = handler;
        }
    }
}
