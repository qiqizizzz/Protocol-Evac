/*
* ┌──────────────────────────────────┐
* │  描    述: 玩家状态转换优先级
* │  类    名: PlayerTransitionPriority.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Player.HFSM.Transition
{
    public enum PlayerTransitionPriority
    {
        Move = 100,
        Skill = 150,
        Action = 200,
        Air = 300,
        Status = 400
    }
}
