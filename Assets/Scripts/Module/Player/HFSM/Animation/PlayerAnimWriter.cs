/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家动画写入器，负责同步 Animator 参数
 * │  类    名: PlayerAnimWriter.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.HFSM;
using UnityEngine;
using Utils.log;

namespace Module.Player.HFSM.Animation
{
    public sealed class PlayerAnimWriter
    {
        #region Animator 参数Hash
        private static readonly int S_MoveSpeedHash = Animator.StringToHash("moveSpeed");
        private static readonly int S_VerticalSpeedHash = Animator.StringToHash("verticalSpeed");
        private static readonly int S_IsGroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int S_JumpStateHash = Animator.StringToHash("Base Layer.jump");
        private static readonly int S_DodgeStateHash = Animator.StringToHash("Base Layer.Action.dodge");
        #endregion

        private Animator m_animator;
        private PlayerAnimResolver m_resolver;
        private PlayerContext m_context;
        private bool m_isInited;

        // 初始化玩家动画写入器依赖
        public void Init(Animator animator, PlayerAnimResolver resolver, PlayerContext context)
        {
            m_animator = animator;
            m_resolver = resolver;
            m_context = context;
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
            applyReplayRequest();
        }

        // 将动画参数写入 Animator
        private void applyParams(PlayerAnimParams animParams)
        {
            m_animator.SetFloat(S_MoveSpeedHash, animParams.MoveSpeed);
            m_animator.SetFloat(S_VerticalSpeedHash, animParams.VerticalSpeed);
            m_animator.SetBool(S_IsGroundedHash, animParams.IsGrounded);
        }

        // 消费并执行一次性动画重播请求
        private void applyReplayRequest()
        {
            PlayerStateId? stateId = m_context.ConsumeAnimReplayRequest();
            if (stateId == PlayerStateId.AirborneJump)
            {
                m_animator.CrossFadeInFixedTime(S_JumpStateHash, 0.03f, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.ActionDodge)
                m_animator.CrossFadeInFixedTime(S_DodgeStateHash, 0.03f, 0, 0f);
        }
    }
}
