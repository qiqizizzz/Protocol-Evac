/*
 * ┌──────────────────────────────────────┐
 * │  描    述: 玩家跳跃状态，负责写入起跳竖直速度
 * │  类    名: PlayerJumpState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────┘
 */

using Module.Player.Config.Air;
using Module.Player.Context;
using UnityEngine;

namespace Module.Player.HFSM.States.Air
{
    public sealed class PlayerJumpState : BasePlayerState
    {
        private readonly PlayerContext m_context;
        private readonly PlayerAirConfigSO m_airConfig;

        public override PlayerStateId Id => PlayerStateId.AirborneJump;
        public override PlayerStateId ParentId => PlayerStateId.Airborne;

        public PlayerJumpState(PlayerContext context, PlayerAirConfigSO airConfig)
        {
            m_context = context;
            m_airConfig = airConfig;
        }

        public override void Enter()
        {
            Vector3 velocity = m_context.Velocity;
            velocity.y = m_airConfig.JumpForce;
            m_context.Velocity = velocity;
        }
    }
}
