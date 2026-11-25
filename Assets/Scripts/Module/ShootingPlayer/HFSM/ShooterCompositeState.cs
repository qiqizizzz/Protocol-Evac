/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色复合状态基类，承载子状态默认入口
* │  类    名: ShooterCompositeState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.ShootingPlayer.HFSM
{
    // 复合状态基类，进入父状态时需要指定一个初始子状态
    public abstract class ShooterCompositeState : BaseShooterState
    {
        // 返回该复合状态的默认子状态标识
        public abstract ShooterStateId GetInitialChildId();
    }
}
