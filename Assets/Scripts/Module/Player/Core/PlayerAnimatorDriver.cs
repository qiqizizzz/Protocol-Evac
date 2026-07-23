/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家动画驱动器，负责同步移动动画参数
 * │  类    名: PlayerAnimatorDriver.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using UnityEngine;
using Utils.log;

namespace Module.Player.Core
{
    public sealed class PlayerAnimatorDriver
    {
        private static readonly int S_MoveSpeedHash = Animator.StringToHash("moveSpeed");
        private static readonly int S_IsMovingHash = Animator.StringToHash("isMoving");
        private static readonly int S_IsSprintingHash = Animator.StringToHash("isSprinting");

        private Animator m_animator;
        private PlayerContext m_context;
        private bool m_isInited;

        public void Init(Animator animator, PlayerContext context)
        {
            if (animator == null)
            {
                QLog.Error("初始化玩家动画驱动失败：Animator 为空");
                return;
            }

            if (context == null)
            {
                QLog.Error("初始化玩家动画驱动失败：PlayerContext 为空");
                return;
            }

            m_animator = animator;
            m_context = context;
            m_animator.applyRootMotion = false;
            m_isInited = true;
        }

        // 根据当前输入与速度同步 Animator 参数
        public void Tick(float deltaTime)
        {
            if (!m_isInited)
                return;

            bool isMoving = !m_context.IsInputLocked
                && !m_context.IsMovementLocked
                && m_context.MoveInput.sqrMagnitude > 0.01f;
            Vector3 horizontalVelocity = new Vector3(m_context.Velocity.x, 0f, m_context.Velocity.z);

            m_animator.SetBool(S_IsMovingHash, isMoving);
            m_animator.SetBool(S_IsSprintingHash, isMoving && m_context.IsSprintPressed);
            m_animator.SetFloat(S_MoveSpeedHash, horizontalVelocity.magnitude);
        }
    }
}
