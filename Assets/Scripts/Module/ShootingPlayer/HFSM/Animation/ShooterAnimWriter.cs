/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色动画写入器，负责同步专属参数与驱动状态切换
* │  类    名: ShooterAnimWriter.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Framework.Character.Animation;
using Framework.Character.HFSM;
using Module.ShootingPlayer.HFSM.Runtime;
using UnityEngine;

namespace Module.ShootingPlayer.HFSM.Animation
{
    // 射击角色动画写入器，通用位移参数与写入流程来自 Character.Core，不依赖千咲的 AnimReplay 机制
    public sealed class ShooterAnimWriter : CharacterAnimWriterBase<ShooterStateId, ShooterAnimParams>
    {
        #region Animator 专属参数Hash
        // 位移与技能类通用参数已在框架基类中定义
        private static readonly int S_IsAimingHash = Animator.StringToHash("isAiming");
        private static readonly int S_IsCrouchingHash = Animator.StringToHash("isCrouching");
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

        private ShooterRuntimeState m_runtimeState;
        private int m_currentTargetStateHash;

        // 持枪少女的位移由 ShooterMotor 驱动，不应用根运动
        protected override bool ApplyRootMotion => false;

        // 初始化动画写入器依赖
        public void Init(Animator animator, CharacterStateMachine<ShooterStateId> stateMachine,
            CharacterAnimResolver<ShooterStateId, ShooterAnimParams> resolver, ShooterRuntimeState runtimeState)
        {
            m_runtimeState = runtimeState;
            InitBase(animator, stateMachine, resolver);
        }

        // 将本帧动画参数与状态机结果写入 Animator
        protected override void ApplyAnimParams(ShooterAnimParams animParams)
        {
            ApplyLocomotionParams(animParams);

            animator.SetBool(S_IsAimingHash, m_runtimeState.IsAiming);
            animator.SetBool(S_IsCrouchingHash, m_runtimeState.IsCrouching);
            animator.SetBool(isSkillHash, stateMachine.IsStateActive(ShooterStateId.Skill));
            animator.SetBool(isDisabledHash, stateMachine.IsStateActive(ShooterStateId.Disabled));
            // 混合树的一维参数只能是 float，这里把受击动画枚举当阈值索引用
            animator.SetFloat(hurtAnimationIdHash, (float)m_runtimeState.CurrentHurtAnimationId);

            ApplyState();
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
                animator.Play(targetHash, 0, 0f);
            else
                animator.CrossFadeInFixedTime(targetHash, LOCOMOTION_BLEND_DURATION, 0, 0f);
        }

        // 将叶子状态映射到 Animator 目标状态
        private bool TryGetTargetState(out int targetHash, out bool playFromStart)
        {
            targetHash = 0;
            playFromStart = false;

            switch (stateMachine.CurrentLeafStateId)
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
    }
}