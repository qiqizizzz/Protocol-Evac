/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色受击与死亡动画标识
* │  类    名: ShooterHurtAnimationId.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.ShootingPlayer.HFSM.Animation
{
    // 射击角色受击动画标识，与千咲受击动画解耦
    public enum ShooterHurtAnimationId
    {
        None = 0,
        Hit1 = 1,   // 轻受击
        Hit2 = 2,   // 重受击
        Stun = 3,   // 硬直
        Die1 = 4,   // 死亡一
        Die2 = 5    // 死亡二
    }
}
