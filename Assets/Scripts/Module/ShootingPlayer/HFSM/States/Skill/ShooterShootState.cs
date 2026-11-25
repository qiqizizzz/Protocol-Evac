/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色开火状态，负责驱动射击技能段落并写入命中窗口
* │  类    名: ShooterShootState.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Player.Context;
using Module.Player.Input.Buffer;
using Module.Player.Skill;
using Module.Player.Skill.Core;
using Module.ShootingPlayer.Config;
using Module.ShootingPlayer.HFSM.Runtime;
using UnityEngine;

namespace Module.ShootingPlayer.HFSM.States.Skill
{
    // 开火状态，单段技能，不做连段与身体转向
    public sealed class ShooterShootState : BaseShooterState
    {
        private readonly PlayerContext m_context;
        private readonly ShooterRuntimeState m_runtimeState;
        private readonly ShooterShootConfigSO m_shootConfig;
        private readonly PlayerSkillController m_skillController;

        public override ShooterStateId Id => ShooterStateId.SkillShoot;
        public override ShooterStateId ParentId => ShooterStateId.Skill;

        public ShooterShootState(PlayerContext context, ShooterRuntimeState runtimeState,
            ShooterShootConfigSO shootConfig, PlayerSkillController skillController)
        {
            m_context = context;
            m_runtimeState = runtimeState;
            m_shootConfig = shootConfig;
            m_skillController = skillController;
        }

        // 进入开火段落并打开射击技能时间轴
        public override void Enter()
        {
            m_context.Input.Buffer.Consume(PlayerBufferedInputType.SpecialSkill);
            m_context.Action.IsStateFinished = false;
            m_context.Movement.IsMovementLocked = m_shootConfig.LockMovement;
            m_runtimeState.ForceAnimatorRestart = true;

            m_skillController.Open(PlayerSkillType.SpecialSkill);

            if (!m_skillController.IsRunning)
                m_context.Action.IsStateFinished = true;
        }

        public override void Exit()
        {
            m_skillController.Close();
            m_context.Action.IsStateFinished = false;
            m_context.Movement.IsMovementLocked = false;
            m_runtimeState.NextShootAvailableTime = Time.time + m_shootConfig.FireInterval;
        }

        public override void Tick(float deltaTime)
        {
            m_context.Action.IsStateFinished = !m_skillController.IsRunning || m_skillController.IsFinished;
        }
    }
}
