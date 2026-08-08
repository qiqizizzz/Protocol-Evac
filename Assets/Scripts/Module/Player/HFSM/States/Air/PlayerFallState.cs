/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家下落状态，承接自然下落与落地转换
 * │  类    名: PlayerFallState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
*/

using Module.Player.Context;
using Module.Player.HFSM.Config.Air;

namespace Module.Player.HFSM.States.Air
{
    public sealed class PlayerFallState : BasePlayerState
    {
        private readonly PlayerContext m_context;
        private readonly PlayerAirConfigSO m_airConfig;

        public override PlayerStateId Id => PlayerStateId.AirborneFall;
        public override PlayerStateId ParentId => PlayerStateId.Airborne;

        public PlayerFallState(PlayerContext context, PlayerAirConfigSO airConfig)
        {
            m_context = context;
            m_airConfig = airConfig;
        }

        // 进入下落状态并应用空中配置的武器表现
        public override void Enter()
        {
            m_context.Action.IsWeaponVisible = m_airConfig.FallLoopClipData.ShowWeapon;
        }
    }
}
