/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家地面移动动画规则集合
 * │  类    名: PlayerMoveAnimRules.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Player.Context;
using Module.Player.HFSM;
using Module.Player.HFSM.Animation;
using UnityEngine;

namespace Module.Player.HFSM.Animation.Rules
{
    public static class PlayerMoveAnimRules
    {
        /// <summary>
        /// 创建地面移动动画规则
        /// </summary>
        /// <param name="context">玩家运行时上下文</param>
        /// <returns>地面移动动画规则集合</returns>
        public static IReadOnlyList<PlayerAnimRule> Create(PlayerContext context)
        {
            return new PlayerAnimRule[]
            {
                new PlayerAnimRule(
                    PlayerStateId.GroundedIdle,
                    (ref PlayerAnimParams animParams) => ResolveGrounded(context, ref animParams)),

                new PlayerAnimRule(
                    PlayerStateId.GroundedMove,
                    (ref PlayerAnimParams animParams) => ResolveGrounded(context, ref animParams)),

                new PlayerAnimRule(
                    PlayerStateId.GroundedStop,
                    (ref PlayerAnimParams animParams) => ResolveGrounded(context, ref animParams))
            };
        }

        #region 解析并绑定动画参数
        // 解析地面动画参数
        private static void ResolveGrounded(PlayerContext context, ref PlayerAnimParams animParams)
        {
            animParams.MoveSpeed = GetHorizontalSpeed(context);
            animParams.LockOnWeight = context.View.IsLockOn ? 1f : 0f;
            animParams.VerticalSpeed = context.Movement.Velocity.y;
            animParams.IsGrounded = context.Movement.IsGrounded;

            Vector2 lockOnMoveDirection = GetLockOnMoveDirection(context);
            animParams.MoveX = lockOnMoveDirection.x;
            animParams.MoveY = lockOnMoveDirection.y;
        }
        #endregion

        // 获取玩家水平速度
        private static float GetHorizontalSpeed(PlayerContext context)
        {
            Vector3 velocity = context.Movement.Velocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }

        // 获取相对玩家朝向的锁定移动方向
        private static Vector2 GetLockOnMoveDirection(PlayerContext context)
        {
            Vector3 localVelocity = context.Transform.InverseTransformDirection(context.Movement.Velocity);
            Vector2 horizontalDirection = new Vector2(localVelocity.x, localVelocity.z);
            if (horizontalDirection.sqrMagnitude <= Mathf.Epsilon)
                return Vector2.zero;

            return horizontalDirection.normalized;
        }
    }
}
