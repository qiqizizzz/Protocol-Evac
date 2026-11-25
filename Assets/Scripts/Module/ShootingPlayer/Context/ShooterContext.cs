/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 射击角色运行时上下文入口
 * │  类    名: ShooterContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using Framework.Character.Runtime.Contexts;
using Module.ShootingPlayer.HFSM;
using Module.ShootingPlayer.HFSM.Animation;
using UnityEngine;

namespace Module.ShootingPlayer.Context
{
    public sealed class ShooterContext :
        CharacterContext<ShooterStateId, ShooterHurtAnimationId, ShooterActionContext, ShooterMovementContext>
    {
        // 创建射击角色运行时上下文
        public ShooterContext(Transform transform) : base(transform)
        {
        }
    }
}
