/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色地面移动相关状态的动画规则
* │  类    名: ShooterMoveAnimRules.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.ChaisakiPlayer.Context;
using Module.ChaisakiPlayer.Context.Runtime;
using UnityEngine;

namespace Module.ShootingPlayer.HFSM.Animation.Rules
{
    // 站立与蹲伏基础位移的动画规则
    public static class ShooterMoveAnimRules
    {
        // 创建地面移动动画规则集合
        public static IReadOnlyList<ShooterAnimRule> Create(PlayerContext context)
        {
            ShooterAnimRule.ResolveHandler handler =
                (ref ShooterAnimParams animParams) => ResolveLocomotion(context, ref animParams);

            return new List<ShooterAnimRule>
            {
                new ShooterAnimRule(ShooterStateId.GroundedIdle, handler),
                new ShooterAnimRule(ShooterStateId.GroundedMove, handler),
                new ShooterAnimRule(ShooterStateId.CrouchedIdle, handler)
            };
        }

        // 解析位移相关的通用动画参数
        private static void ResolveLocomotion(PlayerContext context, ref ShooterAnimParams animParams)
        {
            PlayerMovementContext movement = context.Movement;

            // 已有移动意图时使用目标档位，避免状态切换首帧落入待机动画
            animParams.MoveSpeed = !movement.IsMovementLocked &&
                                   movement.MoveDir.sqrMagnitude > Mathf.Epsilon &&
                                   movement.TargetMoveSpeed > 0f
                ? movement.TargetMoveSpeed
                : GetHorizontalSpeed(movement);

            animParams.VerticalSpeed = movement.Velocity.y;
            animParams.IsGrounded = movement.IsGrounded;

            Vector3 localVelocity = context.Transform.InverseTransformDirection(movement.Velocity);
            Vector2 localDirection = new Vector2(localVelocity.x, localVelocity.z);

            if (localDirection.sqrMagnitude <= Mathf.Epsilon)
                return;

            localDirection.Normalize();
            animParams.MoveX = localDirection.x;
            animParams.MoveY = localDirection.y;
        }

        // 获取水平速度大小
        private static float GetHorizontalSpeed(PlayerMovementContext movement)
        {
            Vector3 velocity = movement.Velocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }
    }
}
