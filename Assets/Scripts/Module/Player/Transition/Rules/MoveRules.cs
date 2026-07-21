/*
* ┌────────────────────────────────────────────────────┐
* │  描    述: 玩家地面移动状态转换规则集合
* │  类    名: MoveRules.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Player.Context;
using Module.Player.HFSM;

namespace Module.Player.Transition.Rules
{
    public static class MoveRules
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        /// <summary>
        /// 创建地面移动状态转换规则
        /// </summary>
        /// <param name="context">玩家运行时上下文</param>
        /// <returns>地面移动状态转换规则集合</returns>
        public static IReadOnlyList<StateRule> Create(PlayerContext context)
        {
            return new StateRule[]
            {
                new StateRule(
                    PlayerStateId.GroundedIdle,
                    PlayerStateId.GroundedMove,
                    RuleLevel.Move,
                    () => canMove(context)),

                new StateRule(
                    PlayerStateId.GroundedMove,
                    PlayerStateId.GroundedIdle,
                    RuleLevel.Move,
                    () => !canMove(context))
            };
        }

        // 判断玩家当前是否具备地面移动条件
        private static bool canMove(PlayerContext context)
        {
            return
                !context.IsInputLocked &&
                !context.IsMovementLocked &&
                context.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
        }
    }
}
