/*
* ┌──────────────────────────────────┐
* │  描    述: 玩家状态转换规则优先级
* │  类    名: RuleLevel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.Player.Transition
{
    public enum RuleLevel
    {
        Move = 100,
        Ability = 200,
        Air = 300,
        Status = 400
    }
}
