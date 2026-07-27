/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家动作动画规则集合
 * │  类    名: PlayerActionAnimRules.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Player.Context;
using Module.Player.HFSM;
using Module.Player.HFSM.Animation;
using UnityEngine;

namespace Module.Player.HFSM.Animation.Rules
{
    public static class PlayerActionAnimRules
    {
        /// <summary>
        /// 创建动作动画规则
        /// </summary>
        /// <param name="context">玩家运行时上下文</param>
        /// <returns>动作动画规则集合</returns>
        public static IReadOnlyList<PlayerAnimRule> Create(PlayerContext context)
        {
            return new PlayerAnimRule[]
            {
                new PlayerAnimRule(
                    PlayerStateId.ActionDodge,
                    (ref PlayerAnimParams animParams) => resolveDodge(context, ref animParams))
            };
        }

        // 解析闪避动画参数
        private static void resolveDodge(PlayerContext context, ref PlayerAnimParams animParams)
        {
            animParams.MoveSpeed = getHorizontalSpeed(context);
            animParams.VerticalSpeed = context.Velocity.y;
            animParams.IsGrounded = context.IsGrounded;
        }

        // 获取玩家水平速度
        private static float getHorizontalSpeed(PlayerContext context)
        {
            Vector3 velocity = context.Velocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }
    }
}
