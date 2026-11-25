/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色状态工厂，负责装配状态树全部节点
* │  类    名: ShootingPlayerStateFactory.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Player.Context;
using Module.Player.Skill.Core;
using Module.ShootingPlayer.Config;
using Module.ShootingPlayer.HFSM.Runtime;
using Module.ShootingPlayer.HFSM.States.Action;
using Module.ShootingPlayer.HFSM.States.Aim;
using Module.ShootingPlayer.HFSM.States.Crouch;
using Module.ShootingPlayer.HFSM.States.Disabled;
using Module.ShootingPlayer.HFSM.States.Ground;
using Module.ShootingPlayer.HFSM.States.Skill;

namespace Module.ShootingPlayer.HFSM.Factory
{
    // 射击角色状态工厂，集中创建并返回状态实例集合
    public static class ShootingPlayerStateFactory
    {
        // 创建射击角色状态树的全部状态
        public static List<BaseShooterState> CreateStates(PlayerContext context,
            ShooterRuntimeState runtimeState, ShootingPlayerSettingsSO settings,
            PlayerSkillController skillController)
        {
            List<BaseShooterState> states = new List<BaseShooterState>();

            // 站立
            states.Add(new ShooterGroundedState());
            states.Add(new ShooterIdleState(context));
            states.Add(new ShooterMoveState(context, settings.MoveConfig));
            states.Add(new ShooterAimState(context, settings.AimConfig));
            states.Add(new ShooterAimMoveState(context, settings.AimConfig));

            // 蹲伏
            states.Add(new ShooterCrouchedState());
            states.Add(new ShooterCrouchIdleState(context));
            states.Add(new ShooterCrouchAimState(context, settings.CrouchConfig));
            states.Add(new ShooterCrouchAimMoveState(context, settings.CrouchConfig));

            // 动作
            states.Add(new ShooterActionState());
            states.Add(new ShooterEvadeState(context, runtimeState, settings.EvadeConfig));

            // 技能
            states.Add(new ShooterSkillState());
            states.Add(new ShooterShootState(context, runtimeState, settings.ShootConfig, skillController));

            // 不可操作
            states.Add(new ShooterDisabledState());
            states.Add(new ShooterHurtState(context, runtimeState, settings.DamageConfig));
            states.Add(new ShooterDeadState(context, runtimeState));

            return states;
        }
    }
}
