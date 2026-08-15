/*
 * ┌───────────────────────────────────────────────────────┐
 * │  描    述: 敌人动画写入器，负责消费能力动画播放请求
 * │  类    名: EnemyAnimWriter.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────┘
 */

using Module.Ability.Data;
using Module.Enemy.Ability.Config;
using Module.Enemy.Context;
using UnityEngine;

namespace Module.Enemy.Animation
{
    public sealed class EnemyAnimWriter
    {
        private const float ANIMATION_BLEND_DURATION = 0.05f;

        private static readonly int S_NormalAttackStateHash = Animator.StringToHash("Base Layer.Attack");
        private static readonly int S_NormalAttackRecoveryStateHash = Animator.StringToHash("Base Layer.AttackRecovery");
        private static readonly int S_IdleStateHash = Animator.StringToHash("Base Layer.Idle");

        private Animator m_animator;
        private EnemyContext m_context;
        private RuntimeAnimatorController m_originalController;
        private AnimatorOverrideController m_overrideController;
        private AnimationClip m_beginBaseClip;
        private AnimationClip m_recoveryBaseClip;

        // 初始化敌人动画写入器依赖
        public void Init(Animator animator, EnemyContext context, EnemyNormalAttackConfigSO normalAttackConfig)
        {
            m_animator = animator;
            m_context = context;
            m_originalController = animator.runtimeAnimatorController;
            m_overrideController = new AnimatorOverrideController(m_originalController);
            m_animator.runtimeAnimatorController = m_overrideController;

            AbilityStepData firstStep = normalAttackConfig.GetStep(0);
            m_beginBaseClip = firstStep.BeginAnimationClip;
            m_recoveryBaseClip = firstStep.RecoveryAnimationClip;
        }

        // 消费并执行当前帧动画请求
        public void Tick()
        {
            if (m_context.Action.TryConsumeAnimReplayRequest(out AnimationClip animationClip,
                    out AbilityStepPhase phase))
            {
                ApplyAbilityAnimation(animationClip, phase);
                return;
            }

            if (m_context.Action.ConsumeIdleAnimRequest())
                m_animator.CrossFadeInFixedTime(S_IdleStateHash, ANIMATION_BLEND_DURATION, 0, 0f);
        }

        // 立即切回敌人待机动画
        public void Close()
        {
            m_animator.CrossFadeInFixedTime(S_IdleStateHash, ANIMATION_BLEND_DURATION, 0, 0f);
        }

        // 解除运行时 Animator 覆写控制器
        public void UnInit()
        {
            if (m_animator != null)
                m_animator.runtimeAnimatorController = m_originalController;

            if (m_overrideController != null)
                Object.Destroy(m_overrideController);

            m_animator = null;
            m_context = null;
            m_originalController = null;
            m_overrideController = null;
            m_beginBaseClip = null;
            m_recoveryBaseClip = null;
        }

        // 覆写当前阶段动画并从状态起点播放
        private void ApplyAbilityAnimation(AnimationClip animationClip, AbilityStepPhase phase)
        {
            if (phase == AbilityStepPhase.Begin)
            {
                m_overrideController[m_beginBaseClip] = animationClip;
                m_animator.CrossFadeInFixedTime(S_NormalAttackStateHash, ANIMATION_BLEND_DURATION, 0, 0f);
                return;
            }

            m_overrideController[m_recoveryBaseClip] = animationClip;
            m_animator.CrossFadeInFixedTime(S_NormalAttackRecoveryStateHash, ANIMATION_BLEND_DURATION, 0, 0f);
        }
    }
}

