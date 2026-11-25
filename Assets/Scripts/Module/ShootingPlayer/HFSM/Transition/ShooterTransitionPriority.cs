/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色状态转换优先级
* │  类    名: ShooterTransitionPriority.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.ShootingPlayer.HFSM.Transition
{
    // 数值越大优先级越高，受控状态最优先
    public enum ShooterTransitionPriority
    {
        Move = 100,
        Skill = 150,
        Action = 200,
        Status = 400
    }
}
