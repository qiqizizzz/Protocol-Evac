/*
 * ┌─────────────────────────────────────────────────────────┐
 * │  描    述: 敌人动作上下文，保存技能状态与动画播放请求
 * │  类    名: EnemyActionContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────┘
 */

using Module.Ability.Data;
using Module.Enemy.Skill;
using UnityEngine;

namespace Module.Enemy.Context.Runtime
{
    public sealed class EnemyActionContext
    {
        private AnimationClip m_animReplayClip;
        private AbilityStepPhase m_animReplayPhase;
        private bool m_animReplayUseRootMotion;
        private bool m_hasAnimReplayRequest;
        private bool m_hasIdleAnimRequest;

        public EnemySkillType? CurrentSkillType { get; private set; }
        public int SkillStepIndex { get; private set; }
        public AbilityStepPhase SkillPhase { get; private set; }
        public bool IsMovementLocked { get; private set; }
        public bool CanRotate { get; private set; }
        public bool IsWeaponVisible { get; private set; }
        public bool IsHurt { get; private set; }
        public bool IsDead { get; private set; }

        // 创建敌人动作上下文
        public EnemyActionContext()
        {
            Reset();
        }

        // 记录当前技能的控制约束
        public void BeginSkill(EnemySkillType skillType, bool lockMovement, bool canRotate)
        {
            CurrentSkillType = skillType;
            IsMovementLocked = lockMovement;
            CanRotate = canRotate;
        }

        // 进入技能段落并提交动画重播请求
        public void EnterSkillStep(int stepIndex, AbilityStepPhase phase, AnimationClip animationClip,
            bool useRootMotion, bool showWeapon)
        {
            SkillStepIndex = stepIndex;
            SkillPhase = phase;
            IsWeaponVisible = showWeapon;
            m_animReplayClip = animationClip;
            m_animReplayPhase = phase;
            m_animReplayUseRootMotion = useRootMotion;
            m_hasAnimReplayRequest = true;
            m_hasIdleAnimRequest = false;
        }

        // 消费一次技能动画重播请求
        public bool TryConsumeAnimReplayRequest(out AnimationClip animationClip, out AbilityStepPhase phase,
            out bool useRootMotion)
        {
            animationClip = m_animReplayClip;
            phase = m_animReplayPhase;
            useRootMotion = m_animReplayUseRootMotion;
            if (!m_hasAnimReplayRequest)
                return false;

            m_animReplayClip = null;
            m_animReplayUseRootMotion = false;
            m_hasAnimReplayRequest = false;
            return true;
        }

        // 完成当前技能并请求回到待机动画
        public void FinishSkill()
        {
            CurrentSkillType = null;
            IsMovementLocked = false;
            CanRotate = false;
            IsWeaponVisible = false;
            m_animReplayClip = null;
            m_animReplayUseRootMotion = false;
            m_hasAnimReplayRequest = false;
            m_hasIdleAnimRequest = true;
        }

        // 中断当前动作并请求播放受击动画
        public void BeginHurt(AnimationClip animationClip)
        {
            CurrentSkillType = null;
            SkillStepIndex = -1;
            SkillPhase = AbilityStepPhase.Begin;
            IsMovementLocked = true;
            CanRotate = false;
            IsWeaponVisible = false;
            IsHurt = true;
            m_animReplayClip = animationClip;
            m_animReplayPhase = AbilityStepPhase.Begin;
            m_animReplayUseRootMotion = false;
            m_hasAnimReplayRequest = true;
            m_hasIdleAnimRequest = false;
        }

        // 保持受击控制并请求播放起身动画
        public void BeginGetUp(AnimationClip animationClip)
        {
            IsMovementLocked = true;
            CanRotate = false;
            IsWeaponVisible = false;
            IsHurt = true;
            m_animReplayClip = animationClip;
            m_animReplayPhase = AbilityStepPhase.Begin;
            m_animReplayUseRootMotion = false;
            m_hasAnimReplayRequest = true;
            m_hasIdleAnimRequest = false;
        }

        // 结束受击动作并请求切回待机动画
        public void FinishHurt()
        {
            if (IsDead)
                return;

            IsHurt = false;
            IsMovementLocked = false;
            CanRotate = false;
            IsWeaponVisible = false;
            m_hasIdleAnimRequest = true;
        }

        // 锁定敌人全部行为，保留当前受击表现
        public void BeginDead()
        {
            CurrentSkillType = null;
            SkillStepIndex = -1;
            SkillPhase = AbilityStepPhase.Begin;
            IsMovementLocked = true;
            CanRotate = false;
            IsWeaponVisible = false;
            IsHurt = false;
            IsDead = true;
            m_hasIdleAnimRequest = false;
        }

        // 消费一次待机动画请求
        public bool ConsumeIdleAnimRequest()
        {
            if (!m_hasIdleAnimRequest)
                return false;

            m_hasIdleAnimRequest = false;
            return true;
        }

        // 重置全部动作运行时数据
        public void Reset()
        {
            m_animReplayClip = null;
            m_animReplayPhase = AbilityStepPhase.Begin;
            m_animReplayUseRootMotion = false;
            m_hasAnimReplayRequest = false;
            m_hasIdleAnimRequest = false;
            CurrentSkillType = null;
            SkillStepIndex = -1;
            SkillPhase = AbilityStepPhase.Begin;
            IsMovementLocked = false;
            CanRotate = false;
            IsWeaponVisible = false;
            IsHurt = false;
            IsDead = false;
        }
    }
}
