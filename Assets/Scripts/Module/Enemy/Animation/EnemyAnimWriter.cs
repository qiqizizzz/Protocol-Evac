/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人动画写入器，通过 Playables 输出基础与技能动画
 * │  类    名: EnemyAnimWriter.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using Module.Enemy.Animation.Config;
using Module.Enemy.Context;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Module.Enemy.Animation
{
    public sealed class EnemyAnimWriter
    {
        private const float ANIMATION_BLEND_DURATION = 0.05f;
        private const int PLAYABLE_INPUT_COUNT = 2;

        private Animator m_animator;
        private EnemyContext m_context;
        private RuntimeAnimatorController m_originalController;
        private AnimationClip m_idleAnimationClip;
        private AnimationClip m_moveAnimationClip;
        private PlayableGraph m_playableGraph;
        private AnimationMixerPlayable m_animationMixer;
        private AnimationClipPlayable[] m_clipPlayables;
        private int m_currentInputIndex;
        private int m_previousInputIndex;
        private float m_blendElapsedTime;
        private float m_blendDuration;
        private float m_currentClipDuration;
        private bool m_originalApplyRootMotion;
        private bool m_isCurrentClipLooping;
        private bool m_isLocomotionMoving;

        // 初始化敌人 Playables 动画输出
        public void Init(Animator animator, EnemyContext context, EnemyAnimationConfigSO animationConfig)
        {
            m_animator = animator;
            m_context = context;
            m_originalController = animator.runtimeAnimatorController;
            m_originalApplyRootMotion = animator.applyRootMotion;
            m_idleAnimationClip = animationConfig.IdleAnimationClip;
            m_moveAnimationClip = animationConfig.MoveAnimationClip;
            m_clipPlayables = new AnimationClipPlayable[PLAYABLE_INPUT_COUNT];
            m_currentInputIndex = -1;
            m_previousInputIndex = -1;

            m_animator.runtimeAnimatorController = null;
            m_animator.applyRootMotion = false;
            m_playableGraph = PlayableGraph.Create($"{animator.gameObject.name} Enemy Animation");
            m_playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            m_animationMixer = AnimationMixerPlayable.Create(m_playableGraph, PLAYABLE_INPUT_COUNT);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(m_playableGraph, "Enemy Animation", animator);
            output.SetSourcePlayable(m_animationMixer);
            m_playableGraph.Play();
            m_isLocomotionMoving = false;
            PlayClip(m_idleAnimationClip, true, false, 0f);
        }

        // 消费动画请求并推进动画混合
        public void Tick(float deltaTime)
        {
            if (m_context.Action.TryConsumeAnimReplayRequest(out AnimationClip animationClip, out _,
                    out bool useRootMotion))
            {
                PlayClip(animationClip, false, useRootMotion, ANIMATION_BLEND_DURATION);
            }
            else if (m_context.Action.ConsumeIdleAnimRequest())
            {
                PlayLocomotion();
            }
            else if (!m_context.Action.CurrentSkillType.HasValue
                     && m_isLocomotionMoving != m_context.Movement.IsMoving)
            {
                PlayLocomotion();
            }

            UpdateCurrentClipLoop();
            UpdateBlend(deltaTime);
        }

        // 立即请求切回待机动画
        public void Close()
        {
            m_isLocomotionMoving = false;
            PlayClip(m_idleAnimationClip, true, false, ANIMATION_BLEND_DURATION);
        }

        // 销毁 PlayableGraph 并还原 Animator 原始设置
        public void UnInit()
        {
            if (m_playableGraph.IsValid())
                m_playableGraph.Destroy();

            if (m_animator != null)
            {
                m_animator.runtimeAnimatorController = m_originalController;
                m_animator.applyRootMotion = m_originalApplyRootMotion;
            }

            m_animator = null;
            m_context = null;
            m_originalController = null;
            m_idleAnimationClip = null;
            m_moveAnimationClip = null;
            m_clipPlayables = null;
            m_currentInputIndex = -1;
            m_previousInputIndex = -1;
            m_isLocomotionMoving = false;
        }

        // 根据当前移动事实切换待机或移动循环动画
        private void PlayLocomotion()
        {
            m_isLocomotionMoving = m_context.Movement.IsMoving;
            AnimationClip animationClip = m_isLocomotionMoving ? m_moveAnimationClip : m_idleAnimationClip;
            PlayClip(animationClip, true, false, ANIMATION_BLEND_DURATION);
        }

        // 创建动画输入并与当前输入交叉混合
        private void PlayClip(AnimationClip animationClip, bool isLooping, bool useRootMotion, float blendDuration)
        {
            int nextInputIndex = m_currentInputIndex == 0 ? 1 : 0;
            DestroyInput(nextInputIndex);

            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(m_playableGraph, animationClip);
            clipPlayable.SetApplyFootIK(true);
            clipPlayable.SetTime(0d);
            clipPlayable.SetSpeed(1d);
            m_playableGraph.Connect(clipPlayable, 0, m_animationMixer, nextInputIndex);
            m_clipPlayables[nextInputIndex] = clipPlayable;

            m_previousInputIndex = m_currentInputIndex;
            m_currentInputIndex = nextInputIndex;
            m_currentClipDuration = animationClip.length;
            m_isCurrentClipLooping = isLooping;
            m_blendElapsedTime = 0f;
            m_blendDuration = blendDuration;
            m_animator.applyRootMotion = useRootMotion;

            m_animationMixer.SetInputWeight(m_currentInputIndex, m_previousInputIndex < 0 ? 1f : 0f);
            if (m_previousInputIndex >= 0)
                m_animationMixer.SetInputWeight(m_previousInputIndex, 1f);

            if (m_previousInputIndex >= 0 && m_blendDuration <= 0f)
                CompleteBlend();
        }

        // 推进当前动画输入的循环时间
        private void UpdateCurrentClipLoop()
        {
            if (!m_isCurrentClipLooping || m_currentInputIndex < 0)
                return;

            AnimationClipPlayable currentPlayable = m_clipPlayables[m_currentInputIndex];
            if (currentPlayable.GetTime() < m_currentClipDuration)
                return;

            currentPlayable.SetTime(currentPlayable.GetTime() % m_currentClipDuration);
            currentPlayable.SetDone(false);
        }

        // 推进当前两段动画之间的混合权重
        private void UpdateBlend(float deltaTime)
        {
            if (m_previousInputIndex < 0)
                return;

            m_blendElapsedTime += deltaTime;
            float blendWeight = Mathf.Clamp01(m_blendElapsedTime / m_blendDuration);
            m_animationMixer.SetInputWeight(m_currentInputIndex, blendWeight);
            m_animationMixer.SetInputWeight(m_previousInputIndex, 1f - blendWeight);
            if (blendWeight >= 1f)
                CompleteBlend();
        }

        // 完成混合并销毁不再使用的旧输入
        private void CompleteBlend()
        {
            m_animationMixer.SetInputWeight(m_currentInputIndex, 1f);
            DestroyInput(m_previousInputIndex);
            m_previousInputIndex = -1;
        }

        // 断开并销毁指定动画输入
        private void DestroyInput(int inputIndex)
        {
            if (inputIndex < 0 || !m_clipPlayables[inputIndex].IsValid())
                return;

            m_playableGraph.Disconnect(m_animationMixer, inputIndex);
            m_clipPlayables[inputIndex].Destroy();
        }
    }
}
