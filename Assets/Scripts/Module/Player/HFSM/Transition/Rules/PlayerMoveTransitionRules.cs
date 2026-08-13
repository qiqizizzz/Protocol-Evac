/*
* ┌────────────────────────────────────────────────────┐
* │  描    述: 玩家地面移动状态转换规则集合
* │  类    名: PlayerMoveTransitionRules.cs
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
    public static class PlayerMoveTransitionRules
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        /// <summary>
        /// 创建地面移动状态转换规则
        /// </summary>
        /// <param name="context">玩家运行时上下文</param>
        /// <returns>地面移动状态转换规则集合</returns>
        public static IReadOnlyList<PlayerTransitionRule> Create(PlayerContext context)
        {
            return new PlayerTransitionRule[]
            {
                new PlayerTransitionRule(PlayerStateId.GroundedIdle, PlayerStateId.GroundedMove,
                    PlayerTransitionPriority.Move, () => CanMove(context)),

                new PlayerTransitionRule(PlayerStateId.GroundedMove, PlayerStateId.GroundedStop,
                    PlayerTransitionPriority.Move, () => CanStop(context), 10),

                new PlayerTransitionRule(PlayerStateId.GroundedMove, PlayerStateId.GroundedIdle,
                    PlayerTransitionPriority.Move, () => !CanMove(context)),

                new PlayerTransitionRule(PlayerStateId.GroundedStop, PlayerStateId.GroundedMove,
                    PlayerTransitionPriority.Move, () => CanMove(context), 10),

                new PlayerTransitionRule(PlayerStateId.GroundedStop, PlayerStateId.GroundedIdle,
                    PlayerTransitionPriority.Move, () => context.Action.IsStateFinished)
            };
        }

        // 判断玩家当前是否具备地面移动条件
        private static bool CanMove(PlayerContext context)
        {
            return
                !context.Input.IsInputLocked &&
                !context.Movement.IsMovementLocked &&
                context.Input.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
        }

        // 判断当前是否应播放急停动作
        private static bool CanStop(PlayerContext context)
        {
            Vector3 velocity = context.Movement.Velocity;
            velocity.y = 0f;

            return
                !context.Input.IsInputLocked &&
                !context.Movement.IsMovementLocked &&
                context.Input.MoveInput.sqrMagnitude <= MOVE_INPUT_THRESHOLD_SQR &&
                velocity.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
        }
    }
}
