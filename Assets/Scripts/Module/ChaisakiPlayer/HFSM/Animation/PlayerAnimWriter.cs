/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家动画写入器，负责同步 Animator 参数
 * │  类    名: PlayerAnimWriter.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.Character.Animation;
using Framework.Character.HFSM;
using Module.Ability.Data;
using Module.ChaisakiPlayer.Context;
using Module.ChaisakiPlayer.Context.Runtime;
using Module.ChaisakiPlayer.HFSM;
using Module.ChaisakiPlayer.HFSM.Animation.Type;
using UnityEngine;
using Utils.log;

namespace Module.ChaisakiPlayer.HFSM.Animation
{
    // 千咲动画写入器，通用位移参数与写入流程由 Character.Core 提供
    public sealed class PlayerAnimWriter : CharacterAnimWriterBase<PlayerStateId, PlayerAnimParams>
    {
        #region Animator 参数Hash
        // 位移与技能类通用参数已在框架基类中定义
        private static readonly int S_LockOnWeightHash = Animator.StringToHash("lockOnWeight");
        private static readonly int S_IsActionHash = Animator.StringToHash("isAction");
        private static readonly int S_EnterDisabledHash = Animator.StringToHash("enterDisabled");
        private static readonly int S_JumpStateHash = Animator.StringToHash("Base Layer.Air.jump_begin");
        private static readonly int S_GroundedLocomotionStateHash = Animator.StringToHash("Base Layer.Grounded_Common.Grounded_Locomotion");
        private static readonly int S_StopWalkLeftStateHash = Animator.StringToHash("Base Layer.Grounded_Common.Grounded_Stop.stop_walk_l");
        private static readonly int S_StopWalkRightStateHash = Animator.StringToHash("Base Layer.Grounded_Common.Grounded_Stop.stop_walk_r");
        private static readonly int S_StopRunLeftStateHash = Animator.StringToHash("Base Layer.Grounded_Common.Grounded_Stop.stop_run_l");
        private static readonly int S_StopRunRightStateHash = Animator.StringToHash("Base Layer.Grounded_Common.Grounded_Stop.stop_run_r");
        private static readonly int S_StopSprintLeftStateHash = Animator.StringToHash("Base Layer.Grounded_Common.Grounded_Stop.stop_sprint_l");
        private static readonly int S_StopSprintRightStateHash = Animator.StringToHash("Base Layer.Grounded_Common.Grounded_Stop.stop_sprint_r");
        private static readonly int S_ActionDodgeStateHash = Animator.StringToHash("Base Layer.Action.dodge");
        private static readonly int S_SkillNormalAttack01StateHash = Animator.StringToHash("Base Layer.Skill.NormalAttack.attack01");
        private static readonly int S_SkillNormalAttack02StateHash = Animator.StringToHash("Base Layer.Skill.NormalAttack.attack02");
        private static readonly int S_SkillNormalAttack03StateHash = Animator.StringToHash("Base Layer.Skill.NormalAttack.attack03");
        private static readonly int S_SkillNormalAttack01RecoveryStateHash = Animator.StringToHash("Base Layer.Skill.NormalAttack.attack01_end");
        private static readonly int S_SkillNormalAttack02RecoveryStateHash = Animator.StringToHash("Base Layer.Skill.NormalAttack.attack02_end");
        private static readonly int S_SkillNormalAttack03RecoveryStateHash = Animator.StringToHash("Base Layer.Skill.NormalAttack.attack03_end");
        private static readonly int S_DisabledHurtLightLeftStateHash = Animator.StringToHash("Base Layer.Disabled.Hurt_Light.hurt_light_l");
        private static readonly int S_DisabledHurtLightRightStateHash = Animator.StringToHash("Base Layer.Disabled.Hurt_Light.hurt_light_r");
        private static readonly int S_DisabledHurtHeavyLeftStateHash = Animator.StringToHash("Base Layer.Disabled.Hurt_Heavy.hurt_heavy_l");
        private static readonly int S_DisabledHurtHeavyRightStateHash = Animator.StringToHash("Base Layer.Disabled.Hurt_Heavy.hurt_heavy_r");
        private static readonly int S_DisabledHurtKnockUpStartStateHash = Animator.StringToHash("Base Layer.Disabled.Hurt_KnockUp.hurt_knock_up_start");
        private static readonly int S_DisabledHurtKnockUpLoopStateHash = Animator.StringToHash("Base Layer.Disabled.Hurt_KnockUp.hurt_knock_up_loop");
        private static readonly int S_DisabledHurtKnockUpFallStateHash = Animator.StringToHash("Base Layer.Disabled.Hurt_KnockUp.hurt_knock_up_fall");
        private static readonly int S_DisabledDeadStateHash = Animator.StringToHash("Base Layer.Disabled.dead");
        #endregion

        private PlayerContext m_context;
        private bool m_wasSkillActive;
        private bool m_wasDisabledActive;
        private bool m_isDisabledEnterRequested;

        // 千咲的位移由 Animator 根运动驱动
        protected override bool ApplyRootMotion => true;

        // 初始化千咲动画写入器依赖
        public void Init(Animator animator, CharacterStateMachine<PlayerStateId> stateMachine,
            CharacterAnimResolver<PlayerStateId, PlayerAnimParams> resolver, PlayerContext context)
        {
            m_context = context;
            InitBase(animator, stateMachine, resolver);
        }

        // 将本帧动画参数与状态机结果写入 Animator
        protected override void ApplyAnimParams(PlayerAnimParams animParams)
        {
            bool isSkillActive = stateMachine.IsStateActive(PlayerStateId.Skill);
            bool isDisabledActive = stateMachine.IsStateActive(PlayerStateId.Disabled);

            ApplyLocomotionParams(animParams);
            animator.SetFloat(S_LockOnWeightHash, animParams.LockOnWeight);

            ApplyStateMachineParams(isSkillActive, isDisabledActive, !m_wasDisabledActive && isDisabledActive);
            ApplyReplayRequest(isSkillActive, isDisabledActive);

            m_wasSkillActive = isSkillActive;
            m_wasDisabledActive = isDisabledActive;
        }

        // 将 HFSM 当前父状态镜像到 Animator 参数
        private void ApplyStateMachineParams(bool isSkillActive, bool isDisabledActive, bool isEnteringDisabled)
        {
            animator.SetBool(isGroundedHash, m_context.Movement.IsGrounded);
            animator.SetBool(S_IsActionHash, stateMachine.IsStateActive(PlayerStateId.Action));
            animator.SetBool(isSkillHash, isSkillActive);
            animator.SetBool(isDisabledHash, isDisabledActive);
            animator.SetBool(isDeadHash, m_context.Damage.IsDead);
            animator.SetInteger(hurtAnimationIdHash, (int)m_context.Damage.HurtAnimationId);

            if (m_isDisabledEnterRequested && IsDisabledAnimatorState())
            {
                animator.SetBool(S_EnterDisabledHash, false);
                m_isDisabledEnterRequested = false;
            }

            if (!isEnteringDisabled)
                return;

            animator.SetBool(S_EnterDisabledHash, true);
            m_isDisabledEnterRequested = true;
        }

        // 消费并执行一次性动画重播请求
        private void ApplyReplayRequest(bool isSkillActive, bool isDisabledActive)
        {
            PlayerStateId? stateId = m_context.Action.ConsumeAnimReplayRequest();
            if (stateId == PlayerStateId.AirborneJump)
            {
                animator.CrossFadeInFixedTime(S_JumpStateHash, 0.03f, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.ActionDodge)
            {
                animator.Play(S_ActionDodgeStateHash, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.GroundedIdle || stateId == PlayerStateId.GroundedMove)
            {
                float blendDuration = m_context.Action.AnimReplayBlendDuration;
                animator.CrossFadeInFixedTime(S_GroundedLocomotionStateHash, blendDuration, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.GroundedStop)
            {
                if (!TryGetStopStateHash(out int fullStateHash))
                    return;

                animator.CrossFadeInFixedTime(fullStateHash, m_context.Action.AnimReplayBlendDuration, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.SkillNormalAttack)
            {
                if (!TryGetNormalAttackStateHash(out int fullStateHash))
                    return;

                animator.Play(fullStateHash, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.DisabledHurt)
            {
                if (!m_wasDisabledActive && isDisabledActive)
                    return;

                if (!TryGetHurtStateHash(out int fullStateHash))
                    return;

                animator.CrossFadeInFixedTime(fullStateHash, 0.03f, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.DisabledDead)
                animator.CrossFadeInFixedTime(S_DisabledDeadStateHash, 0.05f, 0, 0f);
        }
        // 判断 Animator 是否已实际进入受控状态机的任一叶子状态
        private bool IsDisabledAnimatorState()
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            int fullPathHash = stateInfo.fullPathHash;
            return fullPathHash == S_DisabledHurtLightLeftStateHash ||
                   fullPathHash == S_DisabledHurtLightRightStateHash ||
                   fullPathHash == S_DisabledHurtHeavyLeftStateHash ||
                   fullPathHash == S_DisabledHurtHeavyRightStateHash ||
                   fullPathHash == S_DisabledHurtKnockUpStartStateHash ||
                   fullPathHash == S_DisabledHurtKnockUpLoopStateHash ||
                   fullPathHash == S_DisabledHurtKnockUpFallStateHash ||
                   fullPathHash == S_DisabledDeadStateHash;
        }

        // 根据当前受击动画标识解析 Animator 状态
        private bool TryGetHurtStateHash(out int fullStateHash)
        {
            fullStateHash = m_context.Damage.HurtAnimationId switch
            {
                PlayerHurtAnimationId.LightLeft => S_DisabledHurtLightLeftStateHash,
                PlayerHurtAnimationId.LightRight => S_DisabledHurtLightRightStateHash,
                PlayerHurtAnimationId.HeavyLeft => S_DisabledHurtHeavyLeftStateHash,
                PlayerHurtAnimationId.HeavyRight => S_DisabledHurtHeavyRightStateHash,
                PlayerHurtAnimationId.KnockUpStart => S_DisabledHurtKnockUpStartStateHash,
                PlayerHurtAnimationId.KnockUpLoop => S_DisabledHurtKnockUpLoopStateHash,
                PlayerHurtAnimationId.KnockUpFall => S_DisabledHurtKnockUpFallStateHash,
                _ => 0
            };

            if (fullStateHash != 0)
                return true;

            QLog.Error($"播放受击动画失败：动画标识 {m_context.Damage.HurtAnimationId}");
            return false;
        }

        // 根据普攻段数与阶段解析 Animator 状态
        private bool TryGetNormalAttackStateHash(out int fullStateHash)
        {
            fullStateHash = m_context.Action.NormalAttackPhase switch
            {
                AbilityStepPhase.Begin => m_context.Action.NormalAttackIndex switch
                {
                    0 => S_SkillNormalAttack01StateHash,
                    1 => S_SkillNormalAttack02StateHash,
                    2 => S_SkillNormalAttack03StateHash,
                    _ => 0
                },
                AbilityStepPhase.Recovery => m_context.Action.NormalAttackIndex switch
                {
                    0 => S_SkillNormalAttack01RecoveryStateHash,
                    1 => S_SkillNormalAttack02RecoveryStateHash,
                    2 => S_SkillNormalAttack03RecoveryStateHash,
                    _ => 0
                },
                _ => 0
            };

            if (fullStateHash != 0)
                return true;

            QLog.Error($"播放普攻动画失败：段数 {m_context.Action.NormalAttackIndex}，阶段 {m_context.Action.NormalAttackPhase}");
            return false;
        }

        // 根据当前急停动画标识解析 Animator 状态
        private bool TryGetStopStateHash(out int fullStateHash)
        {
            fullStateHash = m_context.Movement.StopAnimationId switch
            {
                PlayerStopAnimationId.WalkLeft => S_StopWalkLeftStateHash,
                PlayerStopAnimationId.WalkRight => S_StopWalkRightStateHash,
                PlayerStopAnimationId.RunLeft => S_StopRunLeftStateHash,
                PlayerStopAnimationId.RunRight => S_StopRunRightStateHash,
                PlayerStopAnimationId.SprintLeft => S_StopSprintLeftStateHash,
                PlayerStopAnimationId.SprintRight => S_StopSprintRightStateHash,
                _ => 0
            };

            if (fullStateHash != 0)
                return true;

            QLog.Error($"播放急停动画失败：动画标识 {m_context.Movement.StopAnimationId}");
            return false;
        }
    }
}
