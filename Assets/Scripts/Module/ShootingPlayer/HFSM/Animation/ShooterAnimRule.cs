/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色动画规则，描述状态与动画参数处理函数的映射
* │  类    名: ShooterAnimRule.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.ShootingPlayer.HFSM.Animation
{
    public readonly struct ShooterAnimRule
    {
        // 动画参数解析委托
        public delegate void ResolveHandler(ref ShooterAnimParams animParams);

        public ShooterStateId StateId { get; }
        public ResolveHandler Handler { get; }

        // 创建射击角色动画规则
        public ShooterAnimRule(ShooterStateId stateId, ResolveHandler handler)
        {
            StateId = stateId;
            Handler = handler;
        }
    }
}
