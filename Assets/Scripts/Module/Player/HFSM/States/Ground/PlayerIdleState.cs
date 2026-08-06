/*
 * ┌───────────────────────────────────────┐
 * │  描    述: 玩家地面待机状态，负责清空移动意图                      
 * │  类    名: PlayerIdleState.cs       
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.HFSM.Config.Move;
using UnityEngine;

namespace Module.Player.HFSM.States.Ground
{
    public sealed class PlayerIdleState : BasePlayerState
    {
        private readonly PlayerContext m_context;
        private readonly PlayerMoveConfigSO m_moveConfig;

        public override PlayerStateId Id => PlayerStateId.GroundedIdle;
        public override PlayerStateId ParentId => PlayerStateId.Grounded;
        
        public PlayerIdleState(PlayerContext context, PlayerMoveConfigSO moveConfig)
        {
            m_context = context;
            m_moveConfig = moveConfig;
        }

        // 进入待机状态并应用移动配置的武器表现
        public override void Enter()
        {
            m_context.MoveDir = Vector3.zero;
            m_context.TargetMoveSpeed = 0f;
            m_context.IsWeaponVisible = m_moveConfig.IdleClipData.ShowWeapon;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            m_context.MoveDir = Vector3.zero;
            m_context.TargetMoveSpeed = 0f;
        }
    }
}
