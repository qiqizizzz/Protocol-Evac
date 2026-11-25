/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色闪避动作的状态转换规则
* │  类    名: ShooterActionTransitionRules.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Player.Context;
using Module.Player.Input.Buffer;
using Module.ShootingPlayer.Config;
using Module.ShootingPlayer.HFSM.Runtime;
using UnityEngine;

namespace Module.ShootingPlayer.HFSM.Transition.Rules
{
    // 闪避动作的进入与退出规则
    public static class ShooterActionTransitionRules
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        // 创建闪避状态转换规则集合
        public static IReadOnlyList<ShooterTransitionRule> Create(PlayerContext context,
            ShooterRuntimeState runtimeState, ShooterEvadeConfigSO evadeConfig)
        {
            List<ShooterTransitionRule> rules = new List<ShooterTransitionRule>();

            rules.Add(new ShooterTransitionRule(ShooterStateId.Grounded, ShooterStateId.ActionEvade,
                ShooterTransitionPriority.Action, () => CanStartEvade(context, evadeConfig), 4));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Crouched, ShooterStateId.ActionEvade,
                ShooterTransitionPriority.Action, () => CanStartEvade(context, evadeConfig), 3));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Action, ShooterStateId.GroundedMove,
                ShooterTransitionPriority.Action,
                () => IsEvadeFinished(context) && IsMoving(context) && !runtimeState.IsCrouching, 2));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Action, ShooterStateId.CrouchedIdle,
                ShooterTransitionPriority.Action,
                () => IsEvadeFinished(context) && runtimeState.IsCrouching, 1));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Action, ShooterStateId.GroundedIdle,
                ShooterTransitionPriority.Action,
                () => IsEvadeFinished(context) && !IsMoving(context) && !runtimeState.IsCrouching, 0));

            return rules;
        }

        // 判断当前是否可以进入闪避
        private static bool CanStartEvade(PlayerContext context, ShooterEvadeConfigSO evadeConfig)
        {
            if (context.Input.IsInputLocked || context.Movement.IsMovementLocked)
                return false;

            return context.Input.Buffer.Has(PlayerBufferedInputType.Dodge, Time.time, evadeConfig.EvadeBufferTime);
        }

        // 判断闪避段落是否已经结束
        private static bool IsEvadeFinished(PlayerContext context)
        {
            return context.Action.IsStateFinished;
        }

        // 判断当前是否存在移动输入
        private static bool IsMoving(PlayerContext context)
        {
            return context.Input.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
        }
    }
}
