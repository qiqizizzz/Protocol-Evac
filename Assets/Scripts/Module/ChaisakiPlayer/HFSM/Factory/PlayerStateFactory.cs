/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家状态树组装工厂
 * │  类    名: PlayerStateFactory.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Framework.Character.Runtime.Config;
using Module.ChaisakiPlayer.Context;
using Module.ChaisakiPlayer.HFSM;
using Module.ChaisakiPlayer.HFSM.States.Action;
using Module.ChaisakiPlayer.HFSM.States.Air;
using Module.ChaisakiPlayer.HFSM.States.Disabled;
using Module.ChaisakiPlayer.HFSM.States.Ground;
using Module.ChaisakiPlayer.HFSM.States.Skill;
using Framework.Character.Runtime.Skill.Core;

// 状态机外壳来自 Character.Core
using PlayerStateMachine = Framework.Character.HFSM.CharacterStateMachine<Module.ChaisakiPlayer.HFSM.PlayerStateId>;

namespace Module.ChaisakiPlayer.HFSM.Factory
{
    public static class PlayerStateFactory
    {
        // 创建并初始化玩家状态树
        public static PlayerStateMachine Create(PlayerContext context, PlayerSettingsSO settings, CharacterSkillController<PlayerStateId> skillController)
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
            stateMachine.RegisterState(new PlayerDisabledState());
            stateMachine.RegisterState(new PlayerHurtState(context, settings.DamageConfig));
            stateMachine.RegisterState(new PlayerDeadState(context));

            stateMachine.Init(PlayerStateId.Grounded);
            return stateMachine;
        }
    }
}
