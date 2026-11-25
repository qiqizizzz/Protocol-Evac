/*
 * ┌──────────────────────────────────────┐
 * │  描    述: 玩家跳跃状态，负责写入起跳竖直速度
 * │  类    名: PlayerJumpState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────┘
 */

using Module.ChaisakiPlayer.Context;
using Module.ChaisakiPlayer.HFSM.Config.Air;
using Framework.Character.Runtime.Input;
using UnityEngine;

namespace Module.ChaisakiPlayer.HFSM.States.Air
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

        // 进入跳跃状态并应用空中配置的武器表现
        public override void Enter()
        {
            m_context.Input.Buffer.Consume(CharacterBufferedInputType.Jump);
            m_context.Action.RequestAnimReplay(PlayerStateId.AirborneJump);
            m_context.Action.IsWeaponVisible = m_airConfig.JumpBeginClipData.ShowWeapon;
            Vector3 velocity = m_context.Movement.Velocity;
            velocity.y = m_airConfig.JumpForce;
            m_context.Movement.Velocity = velocity;
        }
    }
}
