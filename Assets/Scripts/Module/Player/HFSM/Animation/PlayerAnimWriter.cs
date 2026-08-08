/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家动画写入器，负责同步 Animator 参数
 * │  类    名: PlayerAnimWriter.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.HFSM;
using Module.Player.Skill;
using UnityEngine;
using Utils.log;

namespace Module.Player.HFSM.Animation
{
    public sealed class PlayerAnimWriter
    {
        #region Animator 参数Hash
        private static readonly int S_MoveSpeedHash = Animator.StringToHash("moveSpeed");
        private static readonly int S_LockOnWeightHash = Animator.StringToHash("lockOnWeight");
        private static readonly int S_MoveXHash = Animator.StringToHash("moveX");
        private static readonly int S_MoveYHash = Animator.StringToHash("moveY");
        private static readonly int S_VerticalSpeedHash = Animator.StringToHash("verticalSpeed");
        private static readonly int S_IsGroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int S_JumpStateHash = Animator.StringToHash("Base Layer.Air.jump_begin");
        private static readonly int S_DodgeStateHash = Animator.StringToHash("Base Layer.Action.dodge");
        private static readonly int S_GroundedCommonStateHash = Animator.StringToHash("Base Layer.Grounded_Common");
        private static readonly int S_SkillNormalAttack01StateHash = Animator.StringToHash("Base Layer.Skill.NormalAttack.attack01");
        private static readonly int S_SkillNormalAttack02StateHash = Animator.StringToHash("Base Layer.Skill.NormalAttack.attack02");
        private static readonly int S_SkillNormalAttack03StateHash = Animator.StringToHash("Base Layer.Skill.NormalAttack.attack03");
        private static readonly int S_SkillNormalAttack01RecoveryStateHash = Animator.StringToHash("Base Layer.Skill.NormalAttack.attack01_end");
        private static readonly int S_SkillNormalAttack02RecoveryStateHash = Animator.StringToHash("Base Layer.Skill.NormalAttack.attack02_end");
        private static readonly int S_SkillNormalAttack03RecoveryStateHash = Animator.StringToHash("Base Layer.Skill.NormalAttack.attack03_end");
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
            m_animator.applyRootMotion = true;
            m_isInited = true;
        }

        // 同步玩家 Animator 参数
        public void Tick(float deltaTime)
        {
            if (!m_isInited)
                return;

            PlayerAnimParams animParams = m_resolver.Resolve();
            ApplyParams(animParams);
            ApplyReplayRequest();
        }

        // 将动画参数写入 Animator
        private void ApplyParams(PlayerAnimParams animParams)
        {
            m_animator.SetFloat(S_MoveSpeedHash, animParams.MoveSpeed);
            m_animator.SetFloat(S_LockOnWeightHash, animParams.LockOnWeight);
            m_animator.SetFloat(S_MoveXHash, animParams.MoveX);
            m_animator.SetFloat(S_MoveYHash, animParams.MoveY);
            m_animator.SetFloat(S_VerticalSpeedHash, animParams.VerticalSpeed);
            m_animator.SetBool(S_IsGroundedHash, animParams.IsGrounded);
        }

        // 消费并执行一次性动画重播请求
        private void ApplyReplayRequest()
        {
            PlayerStateId? stateId = m_context.Action.ConsumeAnimReplayRequest();
            if (stateId == PlayerStateId.AirborneJump)
            {
                m_animator.CrossFadeInFixedTime(S_JumpStateHash, 0.03f, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.ActionDodge)
            {
                m_animator.CrossFadeInFixedTime(S_DodgeStateHash, 0f, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.GroundedIdle || stateId == PlayerStateId.GroundedMove)
            {
                float blendDuration = m_context.Action.AnimReplayBlendDuration > 0f ? m_context.Action.AnimReplayBlendDuration : 0.05f;
                m_animator.CrossFadeInFixedTime(S_GroundedCommonStateHash, blendDuration, 0, 0f);
                return;
            }

            if (stateId == PlayerStateId.SkillNormalAttack)
            {
                if (!TryGetNormalAttackStateHash(out int fullStateHash))
                    return;

                m_animator.CrossFadeInFixedTime(fullStateHash, 0.03f, 0, 0f);
            }
        }

        // 根据普攻段数与阶段解析 Animator 状态
        private bool TryGetNormalAttackStateHash(out int fullStateHash)
        {
            fullStateHash = m_context.Action.NormalAttackPhase switch
            {
                PlayerSkillStepPhase.Begin => m_context.Action.NormalAttackIndex switch
                {
                    0 => S_SkillNormalAttack01StateHash,
                    1 => S_SkillNormalAttack02StateHash,
                    2 => S_SkillNormalAttack03StateHash,
                    _ => 0
                },
                PlayerSkillStepPhase.Recovery => m_context.Action.NormalAttackIndex switch
                {
                    0 => S_SkillNormalAttack01RecoveryStateHash,
                    1 => S_SkillNormalAttack02RecoveryStateHash,
                    2 => S_SkillNormalAttack03RecoveryStateHash,
                    _ => 0
                },
                _ => 0
            };

            if (fullStateHash != 0)
                return true;

            QLog.Error($"播放普攻动画失败：段数 {m_context.Action.NormalAttackIndex}，阶段 {m_context.Action.NormalAttackPhase}");
            return false;
        }
    }
}
