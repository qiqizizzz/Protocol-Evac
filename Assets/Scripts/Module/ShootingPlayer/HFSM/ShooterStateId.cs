/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色状态机状态标识枚举
* │  类    名: ShooterStateId.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.ShootingPlayer.HFSM
{
    // 射击角色的状态标识，与千咲状态机完全独立
    public enum ShooterStateId
    {
        None = 0,

        // 复合状态
        Grounded,
        Crouched,
        Action,
        Skill,
        Disabled,

        // 站立
        GroundedIdle,
        GroundedMove,
        GroundedAim,
        GroundedAimMove,

        // 蹲伏
        CrouchedIdle,
        CrouchedAim,
        CrouchedAimMove,

        // 动作
        ActionEvade,

        // 技能
        SkillShoot,

        // 不可操作
        DisabledHurt,
        DisabledDead
    }
}
