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

                new PlayerTransitionRule(PlayerStateId.GroundedMove, PlayerStateId.GroundedIdle,
                    PlayerTransitionPriority.Move, () => !CanMove(context))
            };
        }

        // 判断玩家当前是否具备地面移动条件
        private static bool CanMove(PlayerContext context)
        {
            return
                !context.IsInputLocked &&
                !context.IsMovementLocked &&
                context.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
        }
    }
}
