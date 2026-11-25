/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色状态机状态基类
* │  类    名: BaseShooterState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.ShootingPlayer.HFSM
{
    // 射击角色所有状态的抽象基类
    public abstract class BaseShooterState
    {
        // 当前状态标识
        public abstract ShooterStateId Id { get; }

        // 父状态标识，顶层状态为 None
        public virtual ShooterStateId ParentId => ShooterStateId.None;

        // 进入状态
        public virtual void Enter()
        {
        }

        // 退出状态
        public virtual void Exit()
        {
        }

        // 每帧逻辑
        public virtual void Tick(float deltaTime)
        {
        }

        // 每物理帧逻辑
        public virtual void FixedTick(float fixedDeltaTime)
        {
        }
    }
}
