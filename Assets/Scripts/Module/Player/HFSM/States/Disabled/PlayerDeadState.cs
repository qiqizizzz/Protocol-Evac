/*
 * ┌──────────────────────────────────────────┐
 * │  描    述: 玩家死亡状态，持续锁定玩家操作与位移
 * │  类    名: PlayerDeadState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.HFSM.Animation.Type;

namespace Module.Player.HFSM.States.Disabled
{
    public sealed class PlayerDeadState : BasePlayerState
    {
        private readonly PlayerContext m_context;

        public override PlayerStateId Id => PlayerStateId.DisabledDead;
        public override PlayerStateId ParentId => PlayerStateId.Disabled;

        public PlayerDeadState(PlayerContext context)
        {
            m_context = context;
        }

        // 进入死亡状态并持续禁止角色操作
        public override void Enter()
        {
            m_context.Damage.SetHurtAnimationId(PlayerHurtAnimationId.None);
            m_context.Input.IsInputLocked = true;
            m_context.Movement.IsMovementLocked = true;
            m_context.Movement.ClearForcedMoveVelocity();
            m_context.Movement.ClearHorizontalVelocity();
            m_context.Movement.ClearHorizontalMoveIntent();
            m_context.Action.SetRootMotionMoveEnabled(false);
            m_context.Action.ClearRootMotionDeltaPosition();
        }

        // 退出死亡状态并恢复玩家控制
        public override void Exit()
        {
            m_context.Input.IsInputLocked = false;
            m_context.Movement.IsMovementLocked = false;
            m_context.Movement.ClearForcedMoveVelocity();
            m_context.Movement.ClearHorizontalVelocity();
            m_context.Movement.ClearHorizontalMoveIntent();
            m_context.Action.SetRootMotionMoveEnabled(false);
            m_context.Action.ClearRootMotionDeltaPosition();
        }
    }
}
