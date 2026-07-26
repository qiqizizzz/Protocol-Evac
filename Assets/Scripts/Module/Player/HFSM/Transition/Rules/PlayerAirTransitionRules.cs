/*
 * ┌──────────────────────────────────────┐
 * │  描    述: 玩家空中状态转换规则集合
 * │  类    名: PlayerAirTransitionRules.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Player.Context;
using Module.Player.HFSM;
using Module.Player.HFSM.Transition;

namespace Module.Player.HFSM.Transition.Rules
{
    public static class PlayerAirTransitionRules
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        /// <summary>
        /// 创建空中状态转换规则
        /// </summary>
        /// <param name="context">玩家运行时上下文</param>
        /// <returns>空中状态转换规则集合</returns>
        public static IReadOnlyList<PlayerTransitionRule> Create(PlayerContext context)
        {
            return new PlayerTransitionRule[]
            {
                new PlayerTransitionRule(PlayerStateId.Grounded, PlayerStateId.AirborneJump,
                    PlayerTransitionPriority.Air, () => canJump(context), 10),

                new PlayerTransitionRule(PlayerStateId.Grounded, PlayerStateId.AirborneFall,
                    PlayerTransitionPriority.Air, () => context.HasGroundedChecked && !context.IsGrounded),

                new PlayerTransitionRule(PlayerStateId.AirborneJump, PlayerStateId.AirborneFall,
                    PlayerTransitionPriority.Air, () => context.Velocity.y <= 0f),

                new PlayerTransitionRule(PlayerStateId.AirborneFall, PlayerStateId.GroundedMove,
                    PlayerTransitionPriority.Air, () => context.IsGrounded && canMove(context), 10),

                new PlayerTransitionRule(PlayerStateId.AirborneFall, PlayerStateId.GroundedIdle,
                    PlayerTransitionPriority.Air, () => context.IsGrounded)
            };
        }

        // 判断玩家当前是否可以起跳
        private static bool canJump(PlayerContext context)
        {
            return
                context.IsJumpPressed &&
                context.IsGrounded &&
                !context.IsInputLocked;
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
