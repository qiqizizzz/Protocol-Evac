/*
* ┌────────────────────────────────────────────────────┐
* │  描    述: 玩家技能状态转换规则集合
* │  类    名: PlayerSkillTransitionRules.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Player.Context;
using Module.Player.HFSM;
using Module.Player.HFSM.Transition;
using Module.Player.Input.Buffer;
using Module.Player.Skill.Data;
using UnityEngine;

namespace Module.Player.HFSM.Transition.Rules
{
    public static class PlayerSkillTransitionRules
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        /// <summary>
        /// 创建技能状态转换规则
        /// </summary>
        /// <param name="context">玩家运行时上下文</param>
        /// <param name="normalAttackConfig">玩家普通攻击配置</param>
        /// <returns>技能状态转换规则集合</returns>
        public static IReadOnlyList<PlayerTransitionRule> Create(PlayerContext context, PlayerNormalAttackConfigSO normalAttackConfig)
        {
            return new PlayerTransitionRule[]
            {
                new PlayerTransitionRule(PlayerStateId.Grounded, PlayerStateId.Skill,
                    PlayerTransitionPriority.Skill, () => canTriggerNormalAttack(context, normalAttackConfig), 30),

                new PlayerTransitionRule(PlayerStateId.SkillNormalAttack, PlayerStateId.AirborneFall,
                    PlayerTransitionPriority.Skill, () => context.IsStateFinished && !context.IsGrounded, 20),

                new PlayerTransitionRule(PlayerStateId.SkillNormalAttack, PlayerStateId.GroundedMove,
                    PlayerTransitionPriority.Skill, () => context.IsStateFinished && context.IsGrounded && canMove(context), 10),

                new PlayerTransitionRule(PlayerStateId.SkillNormalAttack, PlayerStateId.GroundedIdle,
                    PlayerTransitionPriority.Skill, () => context.IsStateFinished && context.IsGrounded)
            };
        }

        // 判断当前是否满足普攻触发条件
        private static bool canTriggerNormalAttack(PlayerContext context, PlayerNormalAttackConfigSO normalAttackConfig)
        {
            return
                !context.IsInputLocked &&
                !context.IsMovementLocked &&
                context.IsGrounded &&
                context.InputBuffer.Has(PlayerBufferedInputType.NormalAttack, Time.time, normalAttackConfig.NormalAttackBufferTime);
        }

        // 判断技能结束后是否应回到地面移动
        private static bool canMove(PlayerContext context)
        {
            return
                !context.IsInputLocked &&
                context.MoveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
        }
    }
}
