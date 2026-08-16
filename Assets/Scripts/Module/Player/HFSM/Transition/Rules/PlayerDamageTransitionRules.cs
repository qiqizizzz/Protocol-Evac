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
using Module.Player.HFSM.Config.Action;
using Module.Player.HFSM.Config.Air;
using Module.Player.HFSM.Transition;
using Module.Player.Input.Buffer;
using Module.Player.Skill.Data;
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
        /// <param name="airConfig">玩家空中配置</param>
        /// <param name="dodgeConfig">玩家闪避配置</param>
        /// <param name="normalAttackConfig">玩家普通攻击配置</param>
        /// <returns>伤害状态转换规则集合</returns>
        public static IReadOnlyList<PlayerTransitionRule> Create(PlayerContext context, PlayerAirConfigSO airConfig,
            PlayerDodgeConfigSO dodgeConfig, PlayerNormalAttackConfigSO normalAttackConfig)
        {
            return new PlayerTransitionRule[]
            {
                new PlayerTransitionRule(PlayerStateId.None, PlayerStateId.DisabledDead,
                    PlayerTransitionPriority.Status, () => context.Damage.IsDead, 20),

                new PlayerTransitionRule(PlayerStateId.None, PlayerStateId.DisabledHurt,
                    PlayerTransitionPriority.Status, () => context.Damage.HasPendingHurt, 10, true),

                new PlayerTransitionRule(PlayerStateId.DisabledDead, PlayerStateId.AirborneFall,
                    PlayerTransitionPriority.Air, () => !context.Damage.IsDead && !context.Movement.IsGrounded, 20),

                new PlayerTransitionRule(PlayerStateId.DisabledDead, PlayerStateId.GroundedIdle,
                    PlayerTransitionPriority.Move, () => !context.Damage.IsDead && context.Movement.IsGrounded, 20),

                new PlayerTransitionRule(PlayerStateId.DisabledHurt, PlayerStateId.AirborneJump,
                    PlayerTransitionPriority.Air, () => CanCancelHurt(context) && CanJump(context, airConfig), 30),

                new PlayerTransitionRule(PlayerStateId.DisabledHurt, PlayerStateId.AirborneFall,
                    PlayerTransitionPriority.Air, () => CanExitHurt(context) && !context.Movement.IsGrounded, 20),

                new PlayerTransitionRule(PlayerStateId.DisabledHurt, PlayerStateId.ActionDodge,
                    PlayerTransitionPriority.Action, () => CanCancelHurt(context) && CanDodge(context, dodgeConfig), 30),

                new PlayerTransitionRule(PlayerStateId.DisabledHurt, PlayerStateId.Skill,
                    PlayerTransitionPriority.Skill,
                    () => CanCancelHurt(context) && CanNormalAttack(context, normalAttackConfig), 30),

                new PlayerTransitionRule(PlayerStateId.DisabledHurt, PlayerStateId.GroundedMove,
                    PlayerTransitionPriority.Move,
                    () => CanCancelHurt(context) && context.Movement.IsGrounded && CanCancelHurtWithMove(context), 10),

                new PlayerTransitionRule(PlayerStateId.DisabledHurt, PlayerStateId.GroundedIdle,
                    PlayerTransitionPriority.Move, () => CanExitHurt(context) && context.Movement.IsGrounded)
            };
        }

        // 判断受击状态是否已结束移动锁定并允许响应输入
        private static bool CanExitHurt(PlayerContext context)
        {
            return context.Action.IsStateFinished
                && !context.Input.IsInputLocked
                && !context.Movement.IsMovementLocked;
        }

        // 判断受击锁定窗口结束后是否允许由实际输入打断动画
        private static bool CanCancelHurt(PlayerContext context)
        {
            return context.Damage.IsHurtCancellationEnabled
                && !context.Input.IsInputLocked
                && !context.Movement.IsMovementLocked;
        }

        // 判断受击结束后是否应直接起跳
        private static bool CanJump(PlayerContext context, PlayerAirConfigSO airConfig)
        {
            return context.Movement.IsGrounded
                && context.Input.Buffer.Has(PlayerBufferedInputType.Jump, Time.time, airConfig.JumpBufferTime);
        }

        // 判断受击结束后是否应直接闪避
        private static bool CanDodge(PlayerContext context, PlayerDodgeConfigSO dodgeConfig)
        {
            return context.Movement.IsGrounded
                && context.Input.Buffer.Has(PlayerBufferedInputType.Dodge, Time.time, dodgeConfig.DodgeBufferTime);
        }

        // 判断受击结束后是否应直接进入普通攻击
        private static bool CanNormalAttack(PlayerContext context, PlayerNormalAttackConfigSO normalAttackConfig)
        {
            return context.Movement.IsGrounded
                && context.Input.Buffer.Has(PlayerBufferedInputType.NormalAttack, Time.time,
                    normalAttackConfig.NormalAttackBufferTime);
        }

        // 判断锁定窗口结束后是否由新移动输入取消受击动画
        private static bool CanCancelHurtWithMove(PlayerContext context)
        {
            return context.Damage.IsHurtMoveCancellationRequested
                && context.Input.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
        }
    }
}
