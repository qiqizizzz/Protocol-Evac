/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色受击与死亡状态的动画规则
* │  类    名: ShooterDisabledAnimRules.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.ChaisakiPlayer.Context;

using ShooterAnimRule = Framework.Character.Animation.CharacterAnimRule<Module.ShootingPlayer.HFSM.ShooterStateId, Module.ShootingPlayer.HFSM.Animation.ShooterAnimParams>;

namespace Module.ShootingPlayer.HFSM.Animation.Rules
{
    // 不可操作状态的动画规则，受击与死亡由 hurtAnimationId 选择混合树样本
    public static class ShooterDisabledAnimRules
    {
        // 创建不可操作状态动画规则集合
        public static IReadOnlyList<ShooterAnimRule> Create(PlayerContext context)
        {
            ShooterAnimRule.ResolveHandler handler =
                (ref ShooterAnimParams animParams) => ResolveDisabled(context, ref animParams);

            return new List<ShooterAnimRule>
            {
                new ShooterAnimRule(ShooterStateId.DisabledHurt, handler),
                new ShooterAnimRule(ShooterStateId.DisabledDead, handler)
            };
        }

        // 解析不可操作状态的动画参数
        private static void ResolveDisabled(PlayerContext context, ref ShooterAnimParams animParams)
        {
            animParams.MoveSpeed = 0f;
            animParams.VerticalSpeed = context.Movement.Velocity.y;
            animParams.IsGrounded = context.Movement.IsGrounded;
        }
    }
}
