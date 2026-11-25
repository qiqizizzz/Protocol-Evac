/*
 * ┌──────────────────────────────────┐
 * │  描    述: 角色动画写入器基类，统一解析动画参数并写入通用参数
 * │  类    名: CharacterAnimWriterBase.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using Framework.Character.HFSM;
using UnityEngine;
using Utils.log;

namespace Framework.Character.Animation
{
    /// <summary>
    /// 动画写入器基类，统一走「解析 -> 写入」流程，角色子类只写自己的差异部分
    /// </summary>
    /// <typeparam name="TStateId">角色状态标识枚举，默认值表示空状态</typeparam>
    /// <typeparam name="TAnimParams">角色动画参数结构体</typeparam>
    public abstract class CharacterAnimWriterBase<TStateId, TAnimParams>
        where TStateId : struct, Enum
        where TAnimParams : struct, ICharacterAnimParams
    {
        #region 通用 Animator 参数Hash
        // 各角色共享的参数名约定，角色 Animator 需按此命名
        protected static readonly int moveSpeedHash = Animator.StringToHash("moveSpeed");
        protected static readonly int moveXHash = Animator.StringToHash("moveX");
        protected static readonly int moveYHash = Animator.StringToHash("moveY");
        protected static readonly int verticalSpeedHash = Animator.StringToHash("verticalSpeed");
        protected static readonly int isGroundedHash = Animator.StringToHash("isGrounded");
        protected static readonly int isSkillHash = Animator.StringToHash("isSkill");
        protected static readonly int isDisabledHash = Animator.StringToHash("isDisabled");
        protected static readonly int isDeadHash = Animator.StringToHash("isDead");
        protected static readonly int hurtAnimationIdHash = Animator.StringToHash("hurtAnimationId");
        #endregion

        protected Animator animator;
        protected CharacterStateMachine<TStateId> stateMachine;

        private CharacterAnimResolver<TStateId, TAnimParams> m_resolver;
        private bool m_isInited;

        /// <summary>
        /// 是否交由 Animator 应用根运动
        /// </summary>
        protected abstract bool ApplyRootMotion { get; }

        /// <summary>
        /// 初始化写入器依赖，依赖缺失时直接报错不做静默跳过
        /// </summary>
        /// <param name="animator">角色 Animator</param>
        /// <param name="stateMachine">角色层级状态机</param>
        /// <param name="resolver">动画参数解析器</param>
        protected void InitBase(Animator animator, CharacterStateMachine<TStateId> stateMachine,
            CharacterAnimResolver<TStateId, TAnimParams> resolver)
        {
            if (animator == null)
                QLog.Error("动画写入器初始化失败：animator 为空");

            if (stateMachine == null)
                QLog.Error("动画写入器初始化失败：stateMachine 为空");

            if (resolver == null)
                QLog.Error("动画写入器初始化失败：resolver 为空");

            this.animator = animator;
            this.stateMachine = stateMachine;
            m_resolver = resolver;
            animator.applyRootMotion = ApplyRootMotion;
            m_isInited = true;
        }

        /// <summary>
        /// 解析本帧动画参数并交给角色子类写入
        /// </summary>
        /// <param name="deltaTime">本帧时间增量</param>
        public void Tick(float deltaTime)
        {
            if (!m_isInited)
                return;

            ApplyAnimParams(m_resolver.Resolve());
        }

        /// <summary>
        /// 写入位移类通用参数，供子类在自己的写入流程中复用
        /// </summary>
        /// <param name="animParams">本帧动画参数</param>
        protected void ApplyLocomotionParams(TAnimParams animParams)
        {
            animator.SetFloat(moveSpeedHash, animParams.MoveSpeed);
            animator.SetFloat(moveXHash, animParams.MoveX);
            animator.SetFloat(moveYHash, animParams.MoveY);
            animator.SetFloat(verticalSpeedHash, animParams.VerticalSpeed);
            animator.SetBool(isGroundedHash, animParams.IsGrounded);
        }

        /// <summary>
        /// 把解析出的动画参数写入 Animator，并处理角色专属的状态驱动
        /// </summary>
        /// <param name="animParams">本帧解析出的动画参数</param>
        protected abstract void ApplyAnimParams(TAnimParams animParams);
    }
}
