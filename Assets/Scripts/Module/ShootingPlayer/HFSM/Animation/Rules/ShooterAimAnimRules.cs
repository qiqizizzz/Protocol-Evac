/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色瞄准与蹲伏移动状态的动画规则
* │  类    名: ShooterAimAnimRules.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Player.Context;
using Module.Player.Context.Runtime;
using UnityEngine;

namespace Module.ShootingPlayer.HFSM.Animation.Rules
{
    // 瞄准相关状态的动画规则，moveX/moveY 驱动 2D 自由混合树
    public static class ShooterAimAnimRules
    {
        // 创建瞄准动画规则集合
        public static IReadOnlyList<ShooterAnimRule> Create(PlayerContext context)
        {
            ShooterAnimRule.ResolveHandler idleHandler =
                (ref ShooterAnimParams animParams) => ResolveAimIdle(context, ref animParams);

            ShooterAnimRule.ResolveHandler moveHandler =
                (ref ShooterAnimParams animParams) => ResolveAimMove(context, ref animParams);

            return new List<ShooterAnimRule>
            {
                new ShooterAnimRule(ShooterStateId.GroundedAim, idleHandler),
                new ShooterAnimRule(ShooterStateId.CrouchedAim, idleHandler),
                new ShooterAnimRule(ShooterStateId.GroundedAimMove, moveHandler),
                new ShooterAnimRule(ShooterStateId.CrouchedAimMove, moveHandler)
            };
        }

        // 解析瞄准待机动画参数，待机时混合树回到中心点
        private static void ResolveAimIdle(PlayerContext context, ref ShooterAnimParams animParams)
        {
            ResolveAimLocomotion(context, ref animParams);
            animParams.MoveSpeed = 0f;
        }

        // 解析瞄准移动动画参数
        private static void ResolveAimMove(PlayerContext context, ref ShooterAnimParams animParams)
        {
            ResolveAimLocomotion(context, ref animParams);
        }

        // 解析瞄准位移通用参数
        private static void ResolveAimLocomotion(PlayerContext context, ref ShooterAnimParams animParams)
        {
            PlayerMovementContext movement = context.Movement;

            animParams.VerticalSpeed = movement.Velocity.y;
            animParams.IsGrounded = movement.IsGrounded;
            animParams.MoveSpeed = movement.TargetMoveSpeed;

            Vector3 localVelocity = context.Transform.InverseTransformDirection(movement.Velocity);
            Vector2 localDirection = new Vector2(localVelocity.x, localVelocity.z);

            if (localDirection.sqrMagnitude <= Mathf.Epsilon)
                return;

            localDirection.Normalize();
            animParams.MoveX = localDirection.x;
            animParams.MoveY = localDirection.y;
        }
    }
}
