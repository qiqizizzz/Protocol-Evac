/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家根运动接收器，负责接收指定动画位移
 * │  类    名: PlayerRootMotionReceiver.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.Context;
using UnityEngine;
using Utils.log;

namespace Module.Player.HFSM.Animation
{
    public sealed class PlayerRootMotionReceiver : MonoBehaviour
    {
        private Animator m_animator;
        private PlayerContext m_context;
        private bool m_isInited;

        // 初始化玩家根运动接收器依赖
        public void Init(Animator animator, PlayerContext context)
        {
            if (animator == null || context == null)
            {
                QLog.Error("初始化根运动接收器失败：Animator 或 PlayerContext 为空");
                return;
            }

            m_animator = animator;
            m_context = context;
            m_isInited = true;
        }

        private void OnAnimatorMove()
        {
            if (!m_isInited)
                return;

            if (!m_context.IsRootMotionMoveEnabled)
                return;

            Vector3 deltaPosition = m_animator.deltaPosition;
            deltaPosition.y = 0f;
            if (deltaPosition.sqrMagnitude <= 0f)
                return;

            m_context.AddRootMotionDeltaPosition(deltaPosition);
        }
    }
}
