/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家动作状态转换规则集合
 * │  类    名: PlayerActionTransitionRules.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Player.Context;
using Module.Player.HFSM;
using Module.Player.HFSM.Config.Action;
using Module.Player.HFSM.Transition;
using Module.Player.Input.Buffer;
using UnityEngine;

namespace Module.Player.HFSM.Transition.Rules
{
    public static class PlayerActionTransitionRules
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;
        private const float DEFAULT_DODGE_BUFFER_TIME = 0.18f;

        /// <summary>
        /// 创建动作状态转换规则
        /// </summary>
        /// <param name="context">玩家运行时上下文</param>
        /// <param name="dodgeConfig">玩家闪避配置</param>
        /// <returns>动作状态转换规则集合</returns>
        public static IReadOnlyList<PlayerTransitionRule> Create(PlayerContext context, PlayerDodgeConfigSO dodgeConfig)
        {
            return new PlayerTransitionRule[]
            {
                new PlayerTransitionRule(PlayerStateId.Grounded, PlayerStateId.ActionDodge,
                    PlayerTransitionPriority.Action, () => canDodge(context, dodgeConfig), 30),

                new PlayerTransitionRule(PlayerStateId.ActionDodge, PlayerStateId.AirborneFall,
                    PlayerTransitionPriority.Action, () => context.IsStateFinished && !context.IsGrounded, 20),

                new PlayerTransitionRule(PlayerStateId.ActionDodge, PlayerStateId.GroundedMove,
                    PlayerTransitionPriority.Action, () => context.IsStateFinished && context.IsGrounded && canMove(context), 10),

                new PlayerTransitionRule(PlayerStateId.ActionDodge, PlayerStateId.GroundedIdle,
                    PlayerTransitionPriority.Action, () => context.IsStateFinished && context.IsGrounded)
            };
        }

        // 判断当前是否可以触发地面闪避
        private static bool canDodge(PlayerContext context, PlayerDodgeConfigSO dodgeConfig)
        {
            return
                !context.IsInputLocked &&
                !context.IsMovementLocked &&
                context.IsGrounded &&
                context.InputBuffer.Has(PlayerBufferedInputType.Dodge, Time.time, getDodgeBufferTime(dodgeConfig));
        }

        // 获取闪避输入缓存时间
        private static float getDodgeBufferTime(PlayerDodgeConfigSO dodgeConfig)
        {
            return dodgeConfig != null ? dodgeConfig.DodgeBufferTime : DEFAULT_DODGE_BUFFER_TIME;
        }

        // 判断动作结束后是否应回到地面移动
        private static bool canMove(PlayerContext context)
        {
            return
                !context.IsInputLocked &&
                context.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
        }
    }
}
