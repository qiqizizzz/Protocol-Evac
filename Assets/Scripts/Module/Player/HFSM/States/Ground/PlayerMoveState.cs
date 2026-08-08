/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家地面移动状态，负责写入移动方向与目标速度                      
 * │  类    名: PlayerMoveState.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.Core;
using Module.Player.HFSM.Config.Common;
using Module.Player.HFSM.Config.Move;
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

        // 进入移动状态并应用移动配置的武器表现
        public override void Enter()
        {
            RefreshWeaponVisibility();
        }

        // 按当前移动档位持续同步武器表现
        public override void Tick(float deltaTime)
        {
            RefreshWeaponVisibility();
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            Vector2 moveInput = m_context.Input.MoveInput;
            Vector3 moveDir = PlayerMoveDirectionResolver.Resolve(m_context, moveInput);

            m_context.Movement.MoveDir = moveDir;
            m_context.Movement.TargetMoveSpeed = m_context.Input.IsSprintActive ? m_moveConfig.SprintSpeed : m_moveConfig.WalkSpeed;
        }

        // 根据当前移动档位刷新武器表现
        private void RefreshWeaponVisibility()
        {
            PlayerStateClipData clipData = m_context.Input.IsSprintActive
                ? m_moveConfig.RunClipData
                : m_moveConfig.WalkClipData;

            m_context.Action.IsWeaponVisible = clipData.ShowWeapon;
        }
    }
}
