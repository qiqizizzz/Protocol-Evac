/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色受击与死亡的状态转换规则
* │  类    名: ShooterDamageTransitionRules.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.ChaisakiPlayer.Context;
using Module.ShootingPlayer.HFSM.Runtime;
using UnityEngine;

using ShooterTransitionRule = Framework.Character.Transition.CharacterTransitionRule<Module.ShootingPlayer.HFSM.ShooterStateId, Module.ShootingPlayer.HFSM.Transition.ShooterTransitionPriority>;

namespace Module.ShootingPlayer.HFSM.Transition.Rules
{
    // 受击与死亡状态拥有最高优先级，可从任意状态切入
    public static class ShooterDamageTransitionRules
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        // 创建受控状态转换规则集合
        public static IReadOnlyList<ShooterTransitionRule> Create(PlayerContext context,
            ShooterRuntimeState runtimeState)
        {
            List<ShooterTransitionRule> rules = new List<ShooterTransitionRule>();

            rules.Add(new ShooterTransitionRule(ShooterStateId.None, ShooterStateId.DisabledDead,
                ShooterTransitionPriority.Status, () => context.Damage.IsDead, 20, true));

            rules.Add(new ShooterTransitionRule(ShooterStateId.None, ShooterStateId.DisabledHurt,
                ShooterTransitionPriority.Status, () => CanEnterHurt(context), 10, true));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Disabled, ShooterStateId.GroundedAimMove,
                ShooterTransitionPriority.Status, () => IsHurtFinished(context) && IsMoving(context) &&
                                                        runtimeState.IsAiming && !runtimeState.IsCrouching, 8));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Disabled, ShooterStateId.GroundedAim,
                ShooterTransitionPriority.Status, () => IsHurtFinished(context) && runtimeState.IsAiming &&
                                                        !runtimeState.IsCrouching, 7));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Disabled, ShooterStateId.CrouchedAim,
                ShooterTransitionPriority.Status, () => IsHurtFinished(context) && runtimeState.IsCrouching, 6));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Disabled, ShooterStateId.GroundedMove,
                ShooterTransitionPriority.Status, () => IsHurtFinished(context) && IsMoving(context) &&
                                                        !runtimeState.IsCrouching, 5));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Disabled, ShooterStateId.GroundedIdle,
                ShooterTransitionPriority.Status, () => IsHurtFinished(context) && !runtimeState.IsCrouching, 4));

            return rules;
        }

        // 判断当前是否可以切入受击状态
        private static bool CanEnterHurt(PlayerContext context)
        {
            if (context.Damage.IsDead)
                return false;

            return context.Damage.HasPendingHurt;
        }

        // 判断受击段落是否结束且输入已恢复
        private static bool IsHurtFinished(PlayerContext context)
        {
            return context.Action.IsStateFinished && !context.Input.IsInputLocked &&
                   !context.Movement.IsMovementLocked;
        }

        // 判断当前是否存在移动输入
        private static bool IsMoving(PlayerContext context)
        {
            return context.Input.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
        }
    }
}
