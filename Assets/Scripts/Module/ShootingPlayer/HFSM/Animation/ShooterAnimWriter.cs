/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色动画写入器，负责同步 Animator 参数与驱动状态切换
* │  类    名: ShooterAnimWriter.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ShootingPlayer.HFSM.Runtime;
using UnityEngine;

namespace Module.ShootingPlayer.HFSM.Animation
{
    // 射击角色动画写入器，不依赖千咲的 AnimReplay 机制
    public sealed class ShooterAnimWriter
    {
        #region Animator 参数Hash
        private static readonly int S_MoveSpeedHash = Animator.StringToHash("moveSpeed");
        private static readonly int S_MoveXHash = Animator.StringToHash("moveX");
        private static readonly int S_MoveYHash = Animator.StringToHash("moveY");
        private static readonly int S_VerticalSpeedHash = Animator.StringToHash("verticalSpeed");
        private static readonly int S_IsGroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int S_IsAimingHash = Animator.StringToHash("isAiming");
        private static readonly int S_IsCrouchingHash = Animator.StringToHash("isCrouching");
        private static readonly int S_IsSkillHash = Animator.StringToHash("isSkill");
        private static readonly int S_IsDisabledHash = Animator.StringToHash("isDisabled");
        private static readonly int S_IsDeadHash = Animator.StringToHash("isDead");
        private static readonly int S_HurtAnimationIdHash = Animator.StringToHash("hurtAnimationId");
        #endregion

        #region Animator 状态路径Hash
        private static readonly int S_GroundedLocomotionHash = Animator.StringToHash("Base Layer.Grounded.Grounded_Locomotion");
        private static readonly int S_GroundedAimLocomotionHash = Animator.StringToHash("Base Layer.Grounded.Grounded_AimLocomotion");
        private static readonly int S_CrouchedLocomotionHash = Animator.StringToHash("Base Layer.Crouched.Crouched_Locomotion");
        private static readonly int S_CrouchedAimLocomotionHash = Animator.StringToHash("Base Layer.Crouched.Crouched_AimLocomotion");
        private static readonly int S_ActionEvadeHash = Animator.StringToHash("Base Layer.Action.Action_Evade");
        private static readonly int S_SkillShootHash = Animator.StringToHash("Base Layer.Skill.Skill_Shoot");
        private static readonly int S_DisabledHurtHash = Animator.StringToHash("Base Layer.Disabled.Disabled_Hurt");
        private static readonly int S_DisabledDeadHash = Animator.StringToHash("Base Layer.Disabled.Disabled_Dead");
        #endregion

        // 循环类状态淡入时长
        private const float LOCOMOTION_BLEND_DURATION = 0.2f;

        private Animator m_animator;
        private ShooterStateMachine m_stateMachine;
        private ShooterAnimResolver m_resolver;
        private ShooterRuntimeState m_runtimeState;
        private bool m_isInited;
        private int m_currentTargetStateHash;

        // 初始化动画写入器依赖
        public void Init(Animator animator, ShooterStateMachine stateMachine, ShooterAnimResolver resolver,
            ShooterRuntimeState runtimeState)
        {
            m_animator = animator;
            m_stateMachine = stateMachine;
            m_resolver = resolver;
            m_runtimeState = runtimeState;
            m_animator.applyRootMotion = false;
            m_isInited = true;
        }

        // 同步 Animator 参数与状态
        public void Tick(float deltaTime)
        {
            if (!m_isInited)
                return;

            ShooterAnimParams animParams = m_resolver.Resolve();
            ApplyParams(animParams);
            ApplyState();
        }

        // 将动画参数写入 Animator
        private void ApplyParams(ShooterAnimParams animParams)
        {
            m_animator.SetFloat(S_MoveSpeedHash, animParams.MoveSpeed);
            m_animator.SetFloat(S_MoveXHash, animParams.MoveX);
            m_animator.SetFloat(S_MoveYHash, animParams.MoveY);
            m_animator.SetFloat(S_VerticalSpeedHash, animParams.VerticalSpeed);
            m_animator.SetBool(S_IsGroundedHash, animParams.IsGrounded);
            m_animator.SetBool(S_IsAimingHash, m_runtimeState.IsAiming);
            m_animator.SetBool(S_IsCrouchingHash, m_runtimeState.IsCrouching);
            m_animator.SetBool(S_IsSkillHash, IsStateActive(ShooterStateId.Skill));
            m_animator.SetBool(S_IsDisabledHash, IsStateActive(ShooterStateId.Disabled));
            // 混合树的一维参数只能是 float，这里把受击动画枚举当阈值索引用
            m_animator.SetFloat(S_HurtAnimationIdHash, (float)m_runtimeState.CurrentHurtAnimationId);
        }

        // 按当前叶子状态驱动 Animator 状态切换
        private void ApplyState()
        {
            if (!TryGetTargetState(out int targetHash, out bool playFromStart))
                return;

            bool isSameTarget = targetHash == m_currentTargetStateHash;
            bool isRestartRequested = m_runtimeState.ForceAnimatorRestart;

            if (isSameTarget && !isRestartRequested)
                return;

            m_currentTargetStateHash = targetHash;
            m_runtimeState.ForceAnimatorRestart = false;

            // 一次性动作从头播放，循环位移状态使用淡入
            if (playFromStart)
                m_animator.Play(targetHash, 0, 0f);
            else
                m_animator.CrossFadeInFixedTime(targetHash, LOCOMOTION_BLEND_DURATION, 0, 0f);
        }

        // 将叶子状态映射到 Animator 目标状态
        private bool TryGetTargetState(out int targetHash, out bool playFromStart)
        {
            targetHash = 0;
            playFromStart = false;

            switch (m_stateMachine.CurrentLeafStateId)
            {
                case ShooterStateId.GroundedIdle:
                case ShooterStateId.GroundedMove:
                    targetHash = S_GroundedLocomotionHash;
                    return true;
                case ShooterStateId.GroundedAim:
                case ShooterStateId.GroundedAimMove:
                    targetHash = S_GroundedAimLocomotionHash;
                    return true;
                case ShooterStateId.CrouchedIdle:
                    targetHash = S_CrouchedLocomotionHash;
                    return true;
                case ShooterStateId.CrouchedAim:
                case ShooterStateId.CrouchedAimMove:
                    targetHash = S_CrouchedAimLocomotionHash;
                    return true;
                case ShooterStateId.ActionEvade:
                    targetHash = S_ActionEvadeHash;
                    playFromStart = true;
                    return true;
                case ShooterStateId.SkillShoot:
                    targetHash = S_SkillShootHash;
                    playFromStart = true;
                    return true;
                case ShooterStateId.DisabledHurt:
                    targetHash = S_DisabledHurtHash;
                    playFromStart = true;
                    return true;
                case ShooterStateId.DisabledDead:
                    targetHash = S_DisabledDeadHash;
                    playFromStart = true;
                    return true;
                default:
                    return false;
            }
        }

        // 判断指定状态是否位于当前活动路径中
        private bool IsStateActive(ShooterStateId stateId)
        {
            System.Collections.Generic.IReadOnlyList<ShooterStateId> activeStatePath = m_stateMachine.ActiveStatePath;

            for (int i = 0; i < activeStatePath.Count; i++)
            {
                if (activeStatePath[i] == stateId)
                    return true;
            }

            return false;
        }
    }
}
