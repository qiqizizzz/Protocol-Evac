/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色动作状态的动画规则
* │  类    名: ShooterActionAnimRules.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.ChaisakiPlayer.Context;
using Module.ChaisakiPlayer.Context.Runtime;
using UnityEngine;

using ShooterAnimRule = Framework.Character.Animation.CharacterAnimRule<Module.ShootingPlayer.HFSM.ShooterStateId, Module.ShootingPlayer.HFSM.Animation.ShooterAnimParams>;

namespace Module.ShootingPlayer.HFSM.Animation.Rules
{
    // 闪避等一次性动作的动画规则
    public static class ShooterActionAnimRules
    {
        // 创建动作动画规则集合
        public static IReadOnlyList<ShooterAnimRule> Create(PlayerContext context)
        {
            return new List<ShooterAnimRule>
            {
                new ShooterAnimRule(ShooterStateId.ActionEvade,
                    (ref ShooterAnimParams animParams) => ResolveEvade(context, ref animParams))
            };
        }

        // 解析闪避动画参数，位移由强制速度驱动，混合树回到中心
        private static void ResolveEvade(PlayerContext context, ref ShooterAnimParams animParams)
        {
            PlayerMovementContext movement = context.Movement;
            Vector3 velocity = movement.Velocity;
            velocity.y = 0f;

            animParams.MoveSpeed = velocity.magnitude;
            animParams.VerticalSpeed = movement.Velocity.y;
            animParams.IsGrounded = movement.IsGrounded;
        }
    }
}
