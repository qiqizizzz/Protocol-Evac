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
        private Transform m_animatorTransform;
        private Transform m_rootMotionNode;
        private PlayerContext m_context;
        private Vector3 m_animatorAnchorLocalPosition;
        private Quaternion m_animatorAnchorLocalRotation;
        private Vector3 m_rootMotionNodeAnchorLocalPosition;
        private bool m_isInited;

        // 初始化玩家根运动接收器依赖
        public void Init(Animator animator, Transform rootMotionNode, PlayerContext context)
        {
            if (animator == null || rootMotionNode == null || context == null)
            {
                QLog.Error("初始化根运动接收器失败：Animator、Root Motion Node 或 PlayerContext 为空");
                return;
            }

            m_animator = animator;
            m_animatorTransform = animator.transform;
            m_rootMotionNode = rootMotionNode;
            m_context = context;
            m_animatorAnchorLocalPosition = m_animatorTransform.localPosition;
            m_animatorAnchorLocalRotation = m_animatorTransform.localRotation;
            m_rootMotionNodeAnchorLocalPosition = m_rootMotionNode.localPosition;
            m_isInited = true;
        }

        private void OnAnimatorMove()
        {
            if (!m_isInited)
                return;

            Vector3 deltaPosition = m_animator.deltaPosition;
            m_animator.ApplyBuiltinRootMotion();
            RestoreAnimatorAnchor();
            RestoreRootMotionNodeAnchor();

            if (!m_context.Action.IsRootMotionMoveEnabled)
                return;

            deltaPosition.y = 0f;
            if (deltaPosition.sqrMagnitude <= 0f)
                return;

            m_context.Action.AddRootMotionDeltaPosition(deltaPosition);
        }

        private void LateUpdate()
        {
            if (!m_isInited)
                return;

            RestoreAnimatorAnchor();
            RestoreRootMotionNodeAnchor();
        }

        // 将 Animator 子节点固定在玩家根节点挂点，避免 Generic 骨架重复表现根位移
        private void RestoreAnimatorAnchor()
        {
            m_animatorTransform.localPosition = m_animatorAnchorLocalPosition;
            m_animatorTransform.localRotation = m_animatorAnchorLocalRotation;
        }

        // 清除 Generic 骨架重复写入的水平根位移，保留动画的高度与旋转姿势
        private void RestoreRootMotionNodeAnchor()
        {
            Vector3 localPosition = m_rootMotionNode.localPosition;
            localPosition.x = m_rootMotionNodeAnchorLocalPosition.x;
            localPosition.z = m_rootMotionNodeAnchorLocalPosition.z;
            m_rootMotionNode.localPosition = localPosition;
        }
    }
}
