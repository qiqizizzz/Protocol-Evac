/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色位移与姿态切换规则，覆盖站立、瞄准、蹲伏三组状态
* │  类    名: ShooterMoveTransitionRules.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Player.Context;
using Module.ShootingPlayer.Config;
using Module.ShootingPlayer.HFSM.Runtime;
using UnityEngine;

namespace Module.ShootingPlayer.HFSM.Transition.Rules
{
    // 站立、瞄准、蹲伏之间的姿态与移动切换规则
    public static class ShooterMoveTransitionRules
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        // 创建位移相关状态转换规则集合
        public static IReadOnlyList<ShooterTransitionRule> Create(PlayerContext context,
            ShooterRuntimeState runtimeState, ShooterAimConfigSO aimConfig)
        {
            List<ShooterTransitionRule> rules = new List<ShooterTransitionRule>();

            AddCrouchRules(rules, context, runtimeState);
            AddGroundedRules(rules, context, runtimeState, aimConfig);
            AddCrouchedRules(rules, context, runtimeState);

            return rules;
        }

        // 站立与蹲伏的整体切换
        private static void AddCrouchRules(List<ShooterTransitionRule> rules, PlayerContext context,
            ShooterRuntimeState runtimeState)
        {
            rules.Add(new ShooterTransitionRule(ShooterStateId.Grounded, ShooterStateId.CrouchedAim,
                ShooterTransitionPriority.Move, () => runtimeState.IsCrouching && runtimeState.IsAiming, 20));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Grounded, ShooterStateId.CrouchedIdle,
                ShooterTransitionPriority.Move, () => runtimeState.IsCrouching && !runtimeState.IsAiming, 19));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Crouched, ShooterStateId.GroundedIdle,
                ShooterTransitionPriority.Move, () => !runtimeState.IsCrouching && !runtimeState.IsAiming, 18));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Crouched, ShooterStateId.GroundedAim,
                ShooterTransitionPriority.Move, () => !runtimeState.IsCrouching && runtimeState.IsAiming, 17));
        }

        // 站立分支的空闲、移动与瞄准切换
        private static void AddGroundedRules(List<ShooterTransitionRule> rules, PlayerContext context,
            ShooterRuntimeState runtimeState, ShooterAimConfigSO aimConfig)
        {
            rules.Add(new ShooterTransitionRule(ShooterStateId.GroundedIdle, ShooterStateId.GroundedAimMove,
                ShooterTransitionPriority.Move, () => runtimeState.IsAiming && IsMoving(context), 16));

            rules.Add(new ShooterTransitionRule(ShooterStateId.GroundedMove, ShooterStateId.GroundedAimMove,
                ShooterTransitionPriority.Move, () => runtimeState.IsAiming && IsMoving(context), 15));

            rules.Add(new ShooterTransitionRule(ShooterStateId.GroundedIdle, ShooterStateId.GroundedAim,
                ShooterTransitionPriority.Move, () => runtimeState.IsAiming && !IsMoving(context), 14));

            rules.Add(new ShooterTransitionRule(ShooterStateId.GroundedMove, ShooterStateId.GroundedAim,
                ShooterTransitionPriority.Move, () => runtimeState.IsAiming && !IsMoving(context) &&
                                                     aimConfig.CanMoveWhileAiming, 13));

            rules.Add(new ShooterTransitionRule(ShooterStateId.GroundedAimMove, ShooterStateId.GroundedAim,
                ShooterTransitionPriority.Move, () => runtimeState.IsAiming && !IsMoving(context), 12));

            rules.Add(new ShooterTransitionRule(ShooterStateId.GroundedAim, ShooterStateId.GroundedAimMove,
                ShooterTransitionPriority.Move, () => runtimeState.IsAiming && IsMoving(context) &&
                                                     aimConfig.CanMoveWhileAiming, 11));

            rules.Add(new ShooterTransitionRule(ShooterStateId.GroundedAimMove, ShooterStateId.GroundedMove,
                ShooterTransitionPriority.Move, () => !runtimeState.IsAiming && IsMoving(context), 10));

            rules.Add(new ShooterTransitionRule(ShooterStateId.GroundedAim, ShooterStateId.GroundedMove,
                ShooterTransitionPriority.Move, () => !runtimeState.IsAiming && IsMoving(context), 9));

            rules.Add(new ShooterTransitionRule(ShooterStateId.GroundedAimMove, ShooterStateId.GroundedIdle,
                ShooterTransitionPriority.Move, () => !runtimeState.IsAiming && !IsMoving(context), 8));

            rules.Add(new ShooterTransitionRule(ShooterStateId.GroundedAim, ShooterStateId.GroundedIdle,
                ShooterTransitionPriority.Move, () => !runtimeState.IsAiming && !IsMoving(context), 7));

            rules.Add(new ShooterTransitionRule(ShooterStateId.GroundedIdle, ShooterStateId.GroundedMove,
                ShooterTransitionPriority.Move, () => IsMoving(context), 6));

            rules.Add(new ShooterTransitionRule(ShooterStateId.GroundedMove, ShooterStateId.GroundedIdle,
                ShooterTransitionPriority.Move, () => !IsMoving(context), 5));
        }

        // 蹲伏分支的空闲与瞄准切换，蹲伏位移由 2D 混合树内部表达
        private static void AddCrouchedRules(List<ShooterTransitionRule> rules, PlayerContext context,
            ShooterRuntimeState runtimeState)
        {
            rules.Add(new ShooterTransitionRule(ShooterStateId.CrouchedIdle, ShooterStateId.CrouchedAim,
                ShooterTransitionPriority.Move, () => runtimeState.IsAiming, 4));

            rules.Add(new ShooterTransitionRule(ShooterStateId.CrouchedAim, ShooterStateId.CrouchedAimMove,
                ShooterTransitionPriority.Move, () => runtimeState.IsAiming && IsMoving(context), 3));

            rules.Add(new ShooterTransitionRule(ShooterStateId.CrouchedAimMove, ShooterStateId.CrouchedAim,
                ShooterTransitionPriority.Move, () => runtimeState.IsAiming && !IsMoving(context), 2));

            rules.Add(new ShooterTransitionRule(ShooterStateId.CrouchedAim, ShooterStateId.CrouchedIdle,
                ShooterTransitionPriority.Move, () => !runtimeState.IsAiming, 1));

            rules.Add(new ShooterTransitionRule(ShooterStateId.CrouchedAimMove, ShooterStateId.CrouchedIdle,
                ShooterTransitionPriority.Move, () => !runtimeState.IsAiming, 0));
        }

        // 判断当前是否存在有效移动输入
        private static bool IsMoving(PlayerContext context)
        {
            return context.Input.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
        }
    }
}
