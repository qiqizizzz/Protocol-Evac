/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家状态树组装工厂
 * │  类    名: PlayerStateFactory.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.Config;
using Module.Player.Context;
using Module.Player.HFSM;
using Module.Player.HFSM.States.Action;
using Module.Player.HFSM.States.Air;
using Module.Player.HFSM.States.Ground;
using Module.Player.HFSM.States.Skill;
using Module.Player.Skill.Core;

namespace Module.Player.HFSM.Factory
{
    public static class PlayerStateFactory
    {
        // 创建并初始化玩家状态树
        public static PlayerStateMachine Create(PlayerContext context, PlayerSettingsSO settings, PlayerSkillController skillController)
        {
            PlayerStateMachine stateMachine = new PlayerStateMachine();

            stateMachine.RegisterState(new PlayerGroundedState());
            stateMachine.RegisterState(new PlayerIdleState(context, settings.MoveConfig));
            stateMachine.RegisterState(new PlayerMoveState(context, settings.MoveConfig));
            stateMachine.RegisterState(new PlayerStopState(context, settings.MoveConfig));
            stateMachine.RegisterState(new PlayerAirborneState(context, settings.AirConfig));
            stateMachine.RegisterState(new PlayerJumpState(context, settings.AirConfig));
            stateMachine.RegisterState(new PlayerFallState(context, settings.AirConfig));
            stateMachine.RegisterState(new PlayerActionState());
            stateMachine.RegisterState(new PlayerDodgeState(context, settings.DodgeConfig));
            stateMachine.RegisterState(new PlayerSkillState());
            stateMachine.RegisterState(new PlayerNormalAttackState(context, skillController, settings.NormalAttackConfig));

            stateMachine.Init(PlayerStateId.Grounded);
            return stateMachine;
        }
    }
}
