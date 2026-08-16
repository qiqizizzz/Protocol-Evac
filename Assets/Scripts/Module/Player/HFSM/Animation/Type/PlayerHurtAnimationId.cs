/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家受击动画标识，用于选择具体受击表现
 * │  类    名: PlayerHurtAnimationId.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Player.HFSM.Animation.Type
{
    public enum PlayerHurtAnimationId
    {
        None,
        LightLeft,
        LightRight,
        HeavyLeft,
        HeavyRight,
        KnockUpStart,
        KnockUpLoop,
        KnockUpFall
    }
}
