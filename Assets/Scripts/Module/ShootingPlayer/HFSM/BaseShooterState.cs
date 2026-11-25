/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色状态基类，继承角色框架状态契约并绑定射击状态枚举
* │  类    名: BaseShooterState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Framework.Character.HFSM;

namespace Module.ShootingPlayer.HFSM
{
    // 射击角色所有状态的抽象基类，生命周期实现全部来自 Character.Core
    public abstract class BaseShooterState : CharacterStateBase<ShooterStateId>
    {
    }
}
