/*
 * ┌──────────────────────────────────┐
 * │  描    述: 千咲状态基类，继承角色框架状态契约并绑定千咲状态枚举
 * │  类    名: BasePlayerState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.Character.HFSM;

namespace Module.ChaisakiPlayer.HFSM
{
    // 千咲所有状态的抽象基类，生命周期实现全部来自 Character.Core
    public abstract class BasePlayerState : CharacterStateBase<PlayerStateId>
    {
    }
}
