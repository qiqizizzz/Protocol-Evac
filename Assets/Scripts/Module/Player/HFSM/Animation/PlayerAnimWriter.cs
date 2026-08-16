/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家动画写入器，负责同步 Animator 参数
 * │  类    名: PlayerAnimWriter.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Ability.Data;
using Module.Player.Context;
using Module.Player.Context.Runtime;
using Module.Player.HFSM;
using Module.Player.HFSM.Animation.Type;
using UnityEngine;
using Utils.log;

namespace Module.Player.HFSM.Animation
{
    public sealed class PlayerAnimWriter
    {
        #region Animator 参数Hash
        private static readonly int S_MoveSpeedHash = Animator.StringToHash("moveSpeed");
        private static readonly int S_LockOnWeightHash = Animator.StringToHash("lockOnWeight");
        private static readonly int S_MoveXHash = Animator.StringToHash("moveX");
        private static readonly int S_MoveYHash = Animator.StringToHash("moveY");
        private static readonly int S_VerticalSpeedHash = Animator.StringToHash("verticalSpeed");
        private static readonly int S_IsGroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int S_IsActionHash = Animator.StringToHash("isAction");
        private static readonly int S_IsSkillHash = Animator.StringToHash("isSkill");
        private static readonly int S_IsDisabledHash = Animator.StringToHash("isDisabled");
        private static readonly int S_EnterDisabledHash = Animator.StringToHash("enterDisabled");
        private static readonly int S_IsDeadHash = Animator.StringToHash("isDead");
        private static readonly int S_HurtAnimationIdHash = Animator.StringToHash("hurtAnimationId");
        private static readonly int S_JumpStateHash = Animator.StringToHash("Base Layer.Air.jump_begin");
        private static readonly int S_GroundedLocomotionStateHash = Animator.StringToHash("Base Layer.Grounded_Common.Grounded_Locomotion");
        private static readonly int S_StopWalkLeftStateHash = Animator.StringToHash("Base Layer.Grounded_Common.Grounded_Stop.stop_walk_l");
        private static readonly int S_StopWalkRightStateHash = Animator.StringToHash("Base Layer.Grounded_Common.Grounded_Stop.stop_walk_r");
        private static readonly int S_StopRunLeftStateHash = Animator.StringToHash("Base Layer.Grounded_Common.Grounded_Stop.stop_run_l");
        private static readonly int S_StopRunRightStateHash = Animator.StringToHash("Base Layer.Grounded_Common.Grounded_Stop.stop_run_r");
        private static readonly int S_StopSprintLeftStateHash = Animator.StringToHash("Base Layer.Grounded_Common.Grounded_Stop.stop_sprint_l");
        private static readonly int S_StopSprintRightStateHash = Animator.StringToHash("Base Layer.Grounded_Common.Grounded_Stop.stop_sprint_r");
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

        private Animator m_animator;
        private PlayerStateMachine m_stateMachine;
        private PlayerAnimResolver m_resolver;
        private PlayerContext m_context;
        private bool m_isInited;
        private bool m_wasSkillActive;
        private bool m_wasDisabledActive;
        private bool m_isDisabledEnterRequested;

        // 初始化玩家动画写入器依赖
        public void Init(Animator animator, PlayerStateMachine stateMachine, PlayerAnimResolver resolver, PlayerContext context)
        {
            m_animator = animator;
            m_stateMachine = stateMachine;
            m_resolver = resolver;
            m_context = context;
            m_animator.applyRootMotion = true;
            m_isInited = true;
        }

        // 同步玩家 Animator 参数
        public void Tick(float deltaTime)
        {
            if (!m_isInited)
                return;

            bool isSkillActive = IsStateActive(PlayerStateId.Skill);
            bool isDisabledActive = IsStateActive(PlayerStateId.Disabled);
            PlayerAnimParams animParams = m_resolver.Resolve();
            ApplyParams(animParams);
            ApplyStateMachineParams(isSkillActive, isDisabledActive, !m_wasDisabledActive && isDisabledActive);
            ApplyReplayRequest(isSkillActive, isDisabledActive);
            m_wasSkillActive = isSkillActive;
            m_wasDisabledActive = isDisabledActive;
        }

        // 将动画参数写入 Animator
        private void ApplyParams(PlayerAnimParams animParams)
        {
            m_animator.SetFloat(S_MoveSpeedHash, animParams.MoveSpeed);
            m_animator.SetFloat(S_LockOnWeightHash, animParams.LockOnWeight);
            m_animator.SetFloat(S_MoveXHash, animParams.MoveX);
            m_animator.SetFloat(S_MoveYHash, animParams.MoveY);
            m_animator.SetFloat(S_VerticalSpeedHash, animParams.VerticalSpeed);
            m_animator.SetBool(S_IsGroundedHash, animParams.IsGrounded);
        }

        // 将 HFSM 当前父状态镜像到 Animator 参数
        private void ApplyStateMachineParams(bool isSkillActive, bool isDisabledActive, bool isEnteringDisabled)
        {
            m_animator.SetBool(S_IsGroundedHash, m_context.Movement.IsGrounded);
            m_animator.SetBool(S_IsActionHash, IsStateActive(PlayerStateId.Action));
            m_animator.SetBool(S_IsSkillHash, isSkillActive);
            m_animator.SetBool(S_IsDisabledHash, isDisabledActive);
            m_animator.SetBool(S_IsDeadHash, m_context.Damage.IsDead);
            m_animator.SetInteger(S_HurtAnimationIdHash, (int)m_context.Damage.HurtAnimationId);

            if (m_isDisabledEnterRequested && IsDisabledAnimatorState())
            {
                m_animator.SetBool(S_EnterDisabledHash, false);
                m_isDisabledEnterRequested = false;
            }

            if (!isEnteringDisabled)
                return;

            m_animator.SetBool(S_EnterDisabledHash, true);
            m_isDisabledEnterRequested = true;
        }

        // 消费并执行一次性动画重播请求
        private void ApplyReplayRequest(bool isSkillActive, bool isDisabledActive)
        {
            PlayerStateId? stateId = m_context.Action.ConsumeAnimReplayRequest();
            if (stateId == PlayerStateId.AirborneJump)
            {
                m_animator.CrossFadeInFixedTime(S_JumpStateHash, 0.03f, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.ActionDodge)
                return;

            if (stateId == PlayerStateId.GroundedIdle || stateId == PlayerStateId.GroundedMove)
            {
                float blendDuration = m_context.Action.AnimReplayBlendDuration;
                m_animator.CrossFadeInFixedTime(S_GroundedLocomotionStateHash, blendDuration, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.GroundedStop)
            {
                if (!TryGetStopStateHash(out int fullStateHash))
                    return;

                m_animator.CrossFadeInFixedTime(fullStateHash, m_context.Action.AnimReplayBlendDuration, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.SkillNormalAttack)
            {
                if (!TryGetNormalAttackStateHash(out int fullStateHash))
                    return;

                m_animator.Play(fullStateHash, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.DisabledHurt)
            {
                if (!m_wasDisabledActive && isDisabledActive)
                    return;

                if (!TryGetHurtStateHash(out int fullStateHash))
                    return;

                m_animator.CrossFadeInFixedTime(fullStateHash, 0.03f, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.DisabledDead)
                m_animator.CrossFadeInFixedTime(S_DisabledDeadStateHash, 0.05f, 0, 0f);
        }

        // 判断指定状态是否位于当前 HFSM 活动路径中
        private bool IsStateActive(PlayerStateId stateId)
        {
            System.Collections.Generic.IReadOnlyList<PlayerStateId> activeStatePath = m_stateMachine.ActiveStatePath;
            for (int i = 0; i < activeStatePath.Count; i++)
            {
                if (activeStatePath[i] == stateId)
                    return true;
            }

            return false;
        }

        // 判断 Animator 是否已实际进入受控状态机的任一叶子状态
        private bool IsDisabledAnimatorState()
        {
            AnimatorStateInfo stateInfo = m_animator.GetCurrentAnimatorStateInfo(0);
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
