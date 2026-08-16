/*
 * ┌────────────────────────────────────────────────────┐
 * │  描    述: 玩家伤害状态转换规则集合
 * │  类    名: PlayerDamageTransitionRules.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Player.Context;
using Module.Player.HFSM;
using Module.Player.HFSM.Transition;
using UnityEngine;

namespace Module.Player.HFSM.Transition.Rules
{
    public static class PlayerDamageTransitionRules
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        /// <summary>
        /// 创建伤害状态转换规则
        /// </summary>
        /// <param name="context">玩家运行时上下文</param>
        /// <returns>伤害状态转换规则集合</returns>
        public static IReadOnlyList<PlayerTransitionRule> Create(PlayerContext context)
        {
            return new PlayerTransitionRule[]
            {
                new PlayerTransitionRule(PlayerStateId.None, PlayerStateId.DisabledDead,
                    PlayerTransitionPriority.Status, () => context.Damage.IsDead, 20),

                new PlayerTransitionRule(PlayerStateId.None, PlayerStateId.DisabledHurt,
                    PlayerTransitionPriority.Status, () => context.Damage.HasPendingHurt, 10),

                new PlayerTransitionRule(PlayerStateId.DisabledHurt, PlayerStateId.AirborneFall,
                    PlayerTransitionPriority.Status, () => context.Action.IsStateFinished && !context.Movement.IsGrounded, 20),

                new PlayerTransitionRule(PlayerStateId.DisabledHurt, PlayerStateId.GroundedMove,
                    PlayerTransitionPriority.Status, () => context.Action.IsStateFinished && context.Movement.IsGrounded && CanMove(context), 10),

                new PlayerTransitionRule(PlayerStateId.DisabledHurt, PlayerStateId.GroundedIdle,
                    PlayerTransitionPriority.Status, () => context.Action.IsStateFinished && context.Movement.IsGrounded)
            };
        }

        // 判断受击结束后是否应直接恢复地面移动
        private static bool CanMove(PlayerContext context)
        {
            return context.Input.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
        }
    }
}
