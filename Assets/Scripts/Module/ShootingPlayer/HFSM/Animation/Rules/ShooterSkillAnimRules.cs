/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色技能状态的动画规则
* │  类    名: ShooterSkillAnimRules.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.ChaisakiPlayer.Context;

using ShooterAnimRule = Framework.Character.Animation.CharacterAnimRule<Module.ShootingPlayer.HFSM.ShooterStateId, Module.ShootingPlayer.HFSM.Animation.ShooterAnimParams>;

namespace Module.ShootingPlayer.HFSM.Animation.Rules
{
    // 射击技能状态的动画规则
    public static class ShooterSkillAnimRules
    {
        // 创建技能动画规则集合
        public static IReadOnlyList<ShooterAnimRule> Create(PlayerContext context)
        {
            return new List<ShooterAnimRule>
            {
                new ShooterAnimRule(ShooterStateId.SkillShoot,
                    (ref ShooterAnimParams animParams) => ResolveShoot(context, ref animParams))
            };
        }

        // 解析射击动画参数，射击期间锁定位移，混合树回到待机
        private static void ResolveShoot(PlayerContext context, ref ShooterAnimParams animParams)
        {
            animParams.IsGrounded = context.Movement.IsGrounded;
            animParams.MoveSpeed = 0f;
        }
    }
}
