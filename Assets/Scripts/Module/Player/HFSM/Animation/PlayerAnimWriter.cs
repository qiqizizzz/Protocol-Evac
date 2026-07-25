/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家动画写入器，负责同步 Animator 参数
 * │  类    名: PlayerAnimWriter.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;
using Utils.log;

namespace Module.Player.HFSM.Animation
{
    public sealed class PlayerAnimWriter
    {
        #region Animator 参数Hash
        private static readonly int S_MoveSpeedHash = Animator.StringToHash("moveSpeed");
        private static readonly int S_IsMovingHash = Animator.StringToHash("isMoving");
        private static readonly int S_IsSprintingHash = Animator.StringToHash("isSprinting");
        #endregion

        private Animator m_animator;
        private PlayerAnimResolver m_resolver;
        private bool m_isInited;

        // 初始化玩家动画写入器依赖
        public void Init(Animator animator, PlayerAnimResolver resolver)
        {
            if (animator == null)
            {
                QLog.Error("初始化玩家动画写入器失败：Animator 为空");
                return;
            }

            if (resolver == null)
            {
                QLog.Error("初始化玩家动画写入器失败：PlayerAnimResolver 为空");
                return;
            }

            m_animator = animator;
            m_resolver = resolver;
            m_animator.applyRootMotion = false;
            m_isInited = true;
        }

        // 同步玩家 Animator 参数
        public void Tick(float deltaTime)
        {
            if (!m_isInited)
                return;

            PlayerAnimParams animParams = m_resolver.Resolve();
            applyParams(animParams);
        }

        // 将动画参数写入 Animator
        private void applyParams(PlayerAnimParams animParams)
        {
            m_animator.SetBool(S_IsMovingHash, animParams.IsMoving);
            m_animator.SetBool(S_IsSprintingHash, animParams.IsSprinting);
            m_animator.SetFloat(S_MoveSpeedHash, animParams.MoveSpeed);
        }
    }
}
