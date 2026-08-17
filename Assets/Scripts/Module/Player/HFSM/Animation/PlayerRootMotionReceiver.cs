/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家根运动接收器，负责接收指定动画位移
 * │  类    名: PlayerRootMotionReceiver.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Ability.Data;
using Framework.QTower.Common.Sound;
using Module.Player.Audio;
using Module.Player.Context;
using UnityEngine;
using Utils.log;

namespace Module.Player.HFSM.Animation
{
    [AnimationEventReceiver]
    public sealed class PlayerRootMotionReceiver : MonoBehaviour
    {
        private const float ROOT_MOTION_DELTA_THRESHOLD_SQR = 0.00000001f;
        private const float MAX_ROOT_MOTION_SPEED = 8f;

        private Animator m_animator;
        private Transform m_animatorTransform;
        private Transform m_rootMotionNode;
        private PlayerContext m_context;
        private PlayerAudioConfigSO m_audioConfig;
        private Vector3 m_animatorAnchorLocalPosition;
        private Quaternion m_animatorAnchorLocalRotation;
        private Vector3 m_rootMotionNodeAnchorLocalPosition;
        private Vector3 m_previousRootMotionNodeLocalPosition;
        private bool m_hasAnimatorRootMotionDelta;
        private bool m_wasRootMotionMoveEnabled;
        private bool m_isInited;
        private bool m_hasFootstepAudio;
        private int m_nextFootstepClipIndex;

        // 初始化玩家根运动接收器依赖
        public void Init(Animator animator, Transform rootMotionNode, PlayerContext context, PlayerAudioConfigSO audioConfig)
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
            m_audioConfig = audioConfig;
            m_hasFootstepAudio = audioConfig != null && audioConfig.FootstepClipCount > 0;
            m_animatorAnchorLocalPosition = m_animatorTransform.localPosition;
            m_animatorAnchorLocalRotation = m_animatorTransform.localRotation;
            m_rootMotionNodeAnchorLocalPosition = m_rootMotionNode.localPosition;
            m_previousRootMotionNodeLocalPosition = m_rootMotionNode.localPosition;
            m_isInited = true;
        }

        private void OnAnimatorMove()
        {
            if (!m_isInited)
                return;

            Vector3 deltaPosition = m_animator.deltaPosition;
            m_animator.ApplyBuiltinRootMotion();
            RestoreAnimatorAnchor();

            m_hasAnimatorRootMotionDelta = deltaPosition.sqrMagnitude > ROOT_MOTION_DELTA_THRESHOLD_SQR;

            if (!m_context.Action.IsRootMotionMoveEnabled || !m_hasAnimatorRootMotionDelta)
                return;

            deltaPosition.y = 0f;
            m_context.Action.AddRootMotionDeltaPosition(ClampRootMotionDelta(deltaPosition));
        }

        private void LateUpdate()
        {
            if (!m_isInited)
                return;

            CaptureGenericRootMotion();
            RestoreAnimatorAnchor();
            RestoreRootMotionNodeAnchor();
            m_hasAnimatorRootMotionDelta = false;
            m_wasRootMotionMoveEnabled = m_context.Action.IsRootMotionMoveEnabled;
        }

        // 接收移动动画左脚落地事件
        public void OnLeftFootPlant()
        {
            if (!m_isInited)
                return;

            m_context.Movement.RecordPlantedFoot(true);
            PlayFootstep();
        }

        // 接收移动动画右脚落地事件
        public void OnRightFootPlant()
        {
            if (!m_isInited)
                return;

            m_context.Movement.RecordPlantedFoot(false);
            PlayFootstep();
        }

        // 播放当前动画落脚对应的脚步音效
        private void PlayFootstep()
        {
            if (!m_hasFootstepAudio)
                return;

            AudioClip footstepClip = SelectFootstepClip();
            if (footstepClip == null)
                return;

            float pitch = m_audioConfig.FootstepPitch;
            if (m_audioConfig.FootstepRandomPitchRange > 0f)
                pitch += Random.Range(-m_audioConfig.FootstepRandomPitchRange, m_audioConfig.FootstepRandomPitchRange);

            SoundManager.PlayEffect(footstepClip, m_animatorTransform.position, m_audioConfig.FootstepVolume,
                pitch, m_audioConfig.FootstepSpatial);
        }

        // 根据配置选择本次脚步音效
        private AudioClip SelectFootstepClip()
        {
            if (m_audioConfig.RandomFootstep)
                return m_audioConfig.GetFootstepClip(Random.Range(0, m_audioConfig.FootstepClipCount));

            AudioClip footstepClip = m_audioConfig.GetFootstepClip(m_nextFootstepClipIndex);
            m_nextFootstepClipIndex = (m_nextFootstepClipIndex + 1) % m_audioConfig.FootstepClipCount;
            return footstepClip;
        }

        // 在动画姿势完成求值后读取未被Unity提取的Generic Root节点位移
        private void CaptureGenericRootMotion()
        {
            Vector3 rootMotionNodeLocalPosition = m_rootMotionNode.localPosition;
            bool isRootMotionMoveEnabled = m_context.Action.IsRootMotionMoveEnabled;

            if (isRootMotionMoveEnabled && m_wasRootMotionMoveEnabled && !m_hasAnimatorRootMotionDelta)
            {
                Vector3 localDeltaPosition = rootMotionNodeLocalPosition - m_previousRootMotionNodeLocalPosition;
                Vector3 deltaPosition = m_animatorTransform.TransformVector(localDeltaPosition);
                deltaPosition.y = 0f;

                if (deltaPosition.sqrMagnitude > ROOT_MOTION_DELTA_THRESHOLD_SQR)
                    m_context.Action.AddRootMotionDeltaPosition(ClampRootMotionDelta(deltaPosition));
            }

            m_previousRootMotionNodeLocalPosition = rootMotionNodeLocalPosition;
        }

        // 限制异常动画根运动曲线的单帧位移，避免动画数据突变造成闪现
        private Vector3 ClampRootMotionDelta(Vector3 deltaPosition)
        {
            float maxDistance = MAX_ROOT_MOTION_SPEED * Time.deltaTime;
            if (deltaPosition.sqrMagnitude <= maxDistance * maxDistance)
                return deltaPosition;

            return deltaPosition.normalized * maxDistance;
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
