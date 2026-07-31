/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家普通攻击状态
 * │  类    名: PlayerNormalAttackState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.HFSM.Config.Skill;
using Module.Player.Input.Buffer;
using Utils.Timer;

namespace Module.Player.HFSM.States.Skill
{
    public class PlayerNormalAttackState : BasePlayerState
    {
        private readonly PlayerContext m_context;
        private readonly PlayerNormalAttackConfigSO m_normalAttackConfig;

        private DurationTimer m_normalAttackTimer;
        
        public override PlayerStateId Id => PlayerStateId.SkillNormalAttack;
        public override PlayerStateId ParentId => PlayerStateId.Skill;

        public PlayerNormalAttackState(PlayerContext context, PlayerNormalAttackConfigSO normalAttackConfig)
        {
            m_context = context;
            m_normalAttackConfig = normalAttackConfig;
            m_normalAttackTimer = new DurationTimer();
        }

        public override void Enter()
        {
            m_normalAttackTimer.Reset();
            m_context.InputBuffer.Consume(PlayerBufferedInputType.NormalAttack);
            m_context.IsStateFinished = false;
            m_context.IsMovementLocked = m_normalAttackConfig.LockMovement;
            m_context.RequestAnimReplay(PlayerStateId.SkillNormalAttack);
            
            m_normalAttackTimer.Start(m_normalAttackConfig.NormalAttackDuration);
        }

        public override void Exit()
        {
            m_normalAttackTimer.Reset();
            m_context.IsStateFinished = false;
            m_context.IsMovementLocked = false;
        }

        public override void Tick(float deltaTime)
        {
            m_normalAttackTimer.Tick(deltaTime);

            if (m_normalAttackTimer.IsFinished)
                m_context.IsStateFinished = true;
        }
    }
}