/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色射击技能的状态转换规则
* │  类    名: ShooterSkillTransitionRules.cs
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
    // 开火段落的进入与退出规则
    public static class ShooterSkillTransitionRules
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        // 创建射击技能状态转换规则集合
        public static IReadOnlyList<ShooterTransitionRule> Create(PlayerContext context,
            ShooterRuntimeState runtimeState, ShooterShootConfigSO shootConfig)
        {
            List<ShooterTransitionRule> rules = new List<ShooterTransitionRule>();

            rules.Add(new ShooterTransitionRule(ShooterStateId.Grounded, ShooterStateId.SkillShoot,
                ShooterTransitionPriority.Skill, () => CanShoot(context, runtimeState, shootConfig), 6));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Crouched, ShooterStateId.SkillShoot,
                ShooterTransitionPriority.Skill, () => CanShoot(context, runtimeState, shootConfig), 5));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Skill, ShooterStateId.GroundedAimMove,
                ShooterTransitionPriority.Skill,
                () => IsShootFinished(context) && IsMoving(context) && runtimeState.IsAiming &&
                     !runtimeState.IsCrouching, 4));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Skill, ShooterStateId.GroundedAim,
                ShooterTransitionPriority.Skill,
                () => IsShootFinished(context) && runtimeState.IsAiming && !runtimeState.IsCrouching, 3));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Skill, ShooterStateId.CrouchedAim,
                ShooterTransitionPriority.Skill,
                () => IsShootFinished(context) && runtimeState.IsCrouching, 2));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Skill, ShooterStateId.GroundedMove,
                ShooterTransitionPriority.Skill,
                () => IsShootFinished(context) && IsMoving(context) && !runtimeState.IsCrouching, 1));

            rules.Add(new ShooterTransitionRule(ShooterStateId.Skill, ShooterStateId.GroundedIdle,
                ShooterTransitionPriority.Skill,
                () => IsShootFinished(context) && !runtimeState.IsCrouching, 0));

            return rules;
        }

        // 判断当前是否可以进入开火段落
        private static bool CanShoot(PlayerContext context, ShooterRuntimeState runtimeState,
            ShooterShootConfigSO shootConfig)
        {
            if (context.Input.IsInputLocked || context.Movement.IsMovementLocked)
                return false;

            if (runtimeState.IsCrouching && !shootConfig.CanFireCrouched)
                return false;

            if (Time.time < runtimeState.NextShootAvailableTime)
                return false;

            return context.Input.Buffer.Has(PlayerBufferedInputType.SpecialSkill, Time.time,
                shootConfig.ShootBufferTime);
        }

        // 判断开火段落是否已经结束
        private static bool IsShootFinished(PlayerContext context)
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
