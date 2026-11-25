/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色复合状态基类，子状态入口约定来自角色框架
* │  类    名: ShooterCompositeState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Framework.Character.HFSM;

namespace Module.ShootingPlayer.HFSM
{
    // 复合状态基类，进入父状态时的默认子状态约定由 Character.Core 统一校验
    public abstract class ShooterCompositeState : CharacterCompositeState<ShooterStateId>
    {
    }
}
