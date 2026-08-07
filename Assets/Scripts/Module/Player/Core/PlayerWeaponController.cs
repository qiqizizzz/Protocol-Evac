/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家武器表现控制器，负责武器显隐、握持姿态与装饰动画
 * │  类    名: PlayerWeaponController.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.Skill;
using UnityEngine;
using Utils.log;

namespace Module.Player.Core
{
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        private static readonly int SWeaponOpenedStateHash = Animator.StringToHash("Base Layer.WeaponOpened");
        private static readonly int SWeaponClosing01StateHash = Animator.StringToHash("Base Layer.WeaponClosing01");
        private static readonly int SWeaponClosing02StateHash = Animator.StringToHash("Base Layer.WeaponClosing02");
        private static readonly int SWeaponClosing03StateHash = Animator.StringToHash("Base Layer.WeaponClosing03");

        private Animator m_animator;
        private PlayerContext m_context;
        private PlayerSkillStepPhase m_lastAttackPhase;
        private bool m_wasWeaponVisible;

        private void Awake()
        {
            m_animator = GetComponent<Animator>();
        }

        // 初始化武器表现控制器
        public void Init(PlayerContext context)
        {
            m_context = context;
            m_lastAttackPhase = context.NormalAttackPhase;
            RefreshVisibility();
        }

        // 刷新武器表现状态
        public void Tick()
        {
            RefreshVisibility();
            RefreshAnimation();
        }

        // 按玩家运行时意图切换武器根节点
        private void RefreshVisibility()
        {
            if (gameObject.activeSelf == m_context.IsWeaponVisible)
                return;

            gameObject.SetActive(m_context.IsWeaponVisible);
        }

        // 根据普攻阶段驱动武器内部装饰动画
        private void RefreshAnimation()
        {
            if (!m_context.IsWeaponVisible)
            {
                m_wasWeaponVisible = false;
                return;
            }

            if (!m_wasWeaponVisible)
            {
                PlayOpenedAnimation();
                m_lastAttackPhase = m_context.NormalAttackPhase;
                m_wasWeaponVisible = true;
                return;
            }

            if (m_lastAttackPhase == m_context.NormalAttackPhase)
                return;

            m_lastAttackPhase = m_context.NormalAttackPhase;
            if (m_lastAttackPhase == PlayerSkillStepPhase.Recovery)
            {
                PlayRecoveryAnimation();
                return;
            }

            PlayOpenedAnimation();
        }

        // 播放武器展开循环并恢复攻击握持姿态
        private void PlayOpenedAnimation()
        {
            m_animator.CrossFadeInFixedTime(SWeaponOpenedStateHash, 0f, 0, 0f);
        }

        // 按普攻段数播放对应的武器收招姿态动画
        private void PlayRecoveryAnimation()
        {
            if (!TryGetRecoveryStateHash(out int stateHash))
                return;

            m_animator.CrossFadeInFixedTime(stateHash, 0f, 0, 0f);
        }

        // 根据当前普攻段数解析武器收招状态
        private bool TryGetRecoveryStateHash(out int stateHash)
        {
            stateHash = m_context.NormalAttackIndex switch
            {
                0 => SWeaponClosing01StateHash,
                1 => SWeaponClosing02StateHash,
                2 => SWeaponClosing03StateHash,
                _ => 0
            };

            if (stateHash != 0)
                return true;

            QLog.Error($"播放武器收招动画失败：普攻段数 {m_context.NormalAttackIndex}");
            return false;
        }
    }
}
