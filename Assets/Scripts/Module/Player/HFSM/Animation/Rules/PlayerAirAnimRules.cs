/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家空中动画规则集合
 * │  类    名: PlayerAirAnimRules.cs
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
    public static class PlayerAirAnimRules
    {
        /// <summary>
        /// 创建空中动画规则
        /// </summary>
        /// <param name="context">玩家运行时上下文</param>
        /// <returns>空中动画规则集合</returns>
        public static IReadOnlyList<PlayerAnimRule> Create(PlayerContext context)
        {
            return new PlayerAnimRule[]
            {
                new PlayerAnimRule(
                    PlayerStateId.AirborneJump,
                    (ref PlayerAnimParams animParams) => ResolveAirborne(context, ref animParams)),

                new PlayerAnimRule(
                    PlayerStateId.AirborneFall,
                    (ref PlayerAnimParams animParams) => ResolveAirborne(context, ref animParams))
            };
        }

        // 解析空中动画参数
        private static void ResolveAirborne(PlayerContext context, ref PlayerAnimParams animParams)
        {
            animParams.MoveSpeed = GetHorizontalSpeed(context);
            animParams.VerticalSpeed = context.Velocity.y;
            animParams.IsGrounded = context.IsGrounded;
        }

        // 获取玩家水平速度
        private static float GetHorizontalSpeed(PlayerContext context)
        {
            Vector3 velocity = context.Velocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }
    }
}
