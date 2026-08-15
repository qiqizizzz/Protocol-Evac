/*
 * ┌─────────────────────────────────────────────────────────┐
 * │  描    述: 敌人动作上下文，保存能力状态与动画播放请求
 * │  类    名: EnemyActionContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────┘
 */

using Module.Ability.Data;
using Module.Enemy.Ability;
using UnityEngine;

namespace Module.Enemy.Context.Runtime
{
    public sealed class EnemyActionContext
    {
        private EnemyAbilityType? m_requestedAbilityType;
        private AnimationClip m_animReplayClip;
        private AbilityStepPhase m_animReplayPhase;
        private bool m_hasAnimReplayRequest;
        private bool m_hasIdleAnimRequest;

        public EnemyAbilityType? CurrentAbilityType { get; private set; }
        public int AbilityStepIndex { get; private set; }
        public AbilityStepPhase AbilityPhase { get; private set; }
        public bool IsMovementLocked { get; private set; }
        public bool CanRotate { get; private set; }
        public bool IsWeaponVisible { get; private set; }
        public bool HasAbilityRequest => m_requestedAbilityType.HasValue;

        // 创建敌人动作上下文
        public EnemyActionContext()
        {
            Reset();
        }

        // 提交待执行的能力请求
        public void RequestAbility(EnemyAbilityType abilityType)
        {
            m_requestedAbilityType = abilityType;
        }

        // 消费一次待执行的能力请求
        public bool TryConsumeAbilityRequest(out EnemyAbilityType abilityType)
        {
            if (!m_requestedAbilityType.HasValue)
            {
                abilityType = default;
                return false;
            }

            abilityType = m_requestedAbilityType.Value;
            m_requestedAbilityType = null;
            return true;
        }

        // 记录当前能力的控制约束
        public void BeginAbility(EnemyAbilityType abilityType, bool lockMovement, bool canRotate)
        {
            CurrentAbilityType = abilityType;
            IsMovementLocked = lockMovement;
            CanRotate = canRotate;
        }

        // 进入能力段落并提交动画重播请求
        public void EnterAbilityStep(int stepIndex, AbilityStepPhase phase, AnimationClip animationClip, bool showWeapon)
        {
            AbilityStepIndex = stepIndex;
            AbilityPhase = phase;
            IsWeaponVisible = showWeapon;
            m_animReplayClip = animationClip;
            m_animReplayPhase = phase;
            m_hasAnimReplayRequest = true;
            m_hasIdleAnimRequest = false;
        }

        // 消费一次能力动画重播请求
        public bool TryConsumeAnimReplayRequest(out AnimationClip animationClip, out AbilityStepPhase phase)
        {
            animationClip = m_animReplayClip;
            phase = m_animReplayPhase;
            if (!m_hasAnimReplayRequest)
                return false;

            m_animReplayClip = null;
            m_hasAnimReplayRequest = false;
            return true;
        }

        // 完成当前能力并请求回到待机动画
        public void FinishAbility()
        {
            CurrentAbilityType = null;
            IsMovementLocked = false;
            CanRotate = false;
            IsWeaponVisible = false;
            m_animReplayClip = null;
            m_hasAnimReplayRequest = false;
            m_hasIdleAnimRequest = true;
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
            m_requestedAbilityType = null;
            m_animReplayClip = null;
            m_animReplayPhase = AbilityStepPhase.Begin;
            m_hasAnimReplayRequest = false;
            m_hasIdleAnimRequest = false;
            CurrentAbilityType = null;
            AbilityStepIndex = -1;
            AbilityPhase = AbilityStepPhase.Begin;
            IsMovementLocked = false;
            CanRotate = false;
            IsWeaponVisible = false;
        }
    }
}

