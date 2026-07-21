/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家地面移动状态，负责写入移动方向与目标速度                      
 * │  类    名: PlayerMoveState.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Config.Move;
using Module.Player.Context;
using UnityEngine;

namespace Module.Player.HFSM.States.Ground
{
    public class PlayerMoveState : BasePlayerState
    {
        private readonly PlayerContext m_context;
        private readonly PlayerMoveConfigSO m_moveConfig;

        public override PlayerStateId Id => PlayerStateId.GroundedMove;
        public override PlayerStateId ParentId => PlayerStateId.Grounded;
        
        public PlayerMoveState(PlayerContext context, PlayerMoveConfigSO moveConfig)
        {
            m_context = context;
            m_moveConfig = moveConfig;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            Vector2 moveInput = m_context.MoveInput;
            Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y);

            m_context.MoveDir = moveDir;
            m_context.TargetMoveSpeed = m_context.IsSprintPressed ? m_moveConfig.SprintSpeed : m_moveConfig.WalkSpeed;
        }
    }
}
