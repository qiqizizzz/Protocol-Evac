/*
 * ┌───────────────────────────────────────┐
 * │  描    述: 玩家地面待机状态，负责清空移动意图                      
 * │  类    名: PlayerIdleState.cs       
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────┘
 */

using Module.Player.Context;
using UnityEngine;

namespace Module.Player.HFSM.States.Ground
{
    public sealed class PlayerIdleState : BasePlayerState
    {
        private readonly PlayerContext m_context;

        public override PlayerStateId Id => PlayerStateId.GroundedIdle;
        public override PlayerStateId ParentId => PlayerStateId.Grounded;
        
        public PlayerIdleState(PlayerContext context)
        {
            m_context = context;
        }

        public override void Enter()
        {
            m_context.MoveDir = Vector3.zero;
            m_context.TargetMoveSpeed = 0f;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            m_context.MoveDir = Vector3.zero;
            m_context.TargetMoveSpeed = 0f;
        }
    }
}
