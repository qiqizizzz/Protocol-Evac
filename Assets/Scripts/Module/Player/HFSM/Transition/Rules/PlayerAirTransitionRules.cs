/*
 * ┌──────────────────────────────────────┐
 * │  描    述: 玩家空中状态转换规则集合
 * │  类    名: PlayerAirTransitionRules.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Player.Config.Air;
using Module.Player.Context;
using Module.Player.HFSM;
using Module.Player.HFSM.Transition;
using Module.Player.Input.Buffer;
using UnityEngine;

namespace Module.Player.HFSM.Transition.Rules
{
    public static class PlayerAirTransitionRules
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        /// <summary>
        /// 创建空中状态转换规则
        /// </summary>
        /// <param name="context">玩家运行时上下文</param>
        /// <param name="airConfig">玩家空中配置</param>
        /// <returns>空中状态转换规则集合</returns>
        public static IReadOnlyList<PlayerTransitionRule> Create(PlayerContext context, PlayerAirConfigSO airConfig)
        {
            return new PlayerTransitionRule[]
            {
                new PlayerTransitionRule(PlayerStateId.Grounded, PlayerStateId.AirborneJump,
                    PlayerTransitionPriority.Air, () => canGroundedJump(context, airConfig), 10),

                new PlayerTransitionRule(PlayerStateId.Grounded, PlayerStateId.AirborneFall,
                    PlayerTransitionPriority.Air, () => context.HasGroundedChecked && !context.IsGrounded),

                new PlayerTransitionRule(PlayerStateId.AirborneFall, PlayerStateId.AirborneJump,
                    PlayerTransitionPriority.Air, () => canCoyoteJump(context, airConfig), 20),

                new PlayerTransitionRule(PlayerStateId.AirborneJump, PlayerStateId.AirborneFall,
                    PlayerTransitionPriority.Air, () => context.Velocity.y <= 0f),

                new PlayerTransitionRule(PlayerStateId.AirborneFall, PlayerStateId.GroundedMove,
                    PlayerTransitionPriority.Air, () => context.IsGrounded && canMove(context), 10),

                new PlayerTransitionRule(PlayerStateId.AirborneFall, PlayerStateId.GroundedIdle,
                    PlayerTransitionPriority.Air, () => context.IsGrounded)
            };
        }

        // 判断玩家在地面或落地缓存窗口内是否可以起跳
        private static bool canGroundedJump(PlayerContext context, PlayerAirConfigSO airConfig)
        {
            return
                context.IsGrounded &&
                hasBufferedJump(context, airConfig);
        }

        // 判断玩家离开地面后是否仍可通过土狼时间起跳
        private static bool canCoyoteJump(PlayerContext context, PlayerAirConfigSO airConfig)
        {
            float nowTime = Time.time;
            return
                !context.IsGrounded &&
                hasBufferedJump(context, airConfig) &&
                nowTime - context.LastGroundedTime <= airConfig.CoyoteTime;
        }

        // 判断玩家是否存在有效跳跃缓存
        private static bool hasBufferedJump(PlayerContext context, PlayerAirConfigSO airConfig)
        {
            float nowTime = Time.time;
            return
                !context.IsInputLocked &&
                context.InputBuffer.Has(PlayerBufferedInputType.Jump, nowTime, airConfig.JumpBufferTime);
        }

        // 判断玩家落地后是否应直接进入移动状态
        private static bool canMove(PlayerContext context)
        {
            return
                !context.IsInputLocked &&
                !context.IsMovementLocked &&
                context.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
        }
    }
}
